using Npgsql;
using System.Text.Json;
using Arc.Domain.Models;
using Arc.Application.Persistence;
using Arc.Application.RegressionGates;
namespace Arc.Infrastructure.RegressionGates;


/// <summary>
/// PostgreSQL-backed regression gate repository.
/// Stores gates with JSON serialization of rules.
/// </summary>
public sealed class PostgresRegressionGateRepository : IRegressionGateRepository
{
    private readonly IDatabaseContext _dbContext;

    public PostgresRegressionGateRepository(IDatabaseContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<RegressionGate> CreateAsync(RegressionGate gate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gate);

        const string sql = @"
            INSERT INTO regression_gates 
                (id, owner_id, name, description, workflow_id, golden_execution_id, rules, is_active, created_at_utc)
            VALUES 
                (@id, @ownerId, @name, @description, @workflowId, @goldenExecutionId, @rules::jsonb, @isActive, @createdAtUtc)
            RETURNING id";

        await using var connection = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);

        var rulesJson = SerializeRules(gate.Rules);

        command.Parameters.AddWithValue("id", gate.Id.Value);
        command.Parameters.AddWithValue("ownerId", gate.OwnerId.Value);
        command.Parameters.AddWithValue("name", gate.Name);
        command.Parameters.AddWithValue("description", (object?)gate.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("workflowId", (object?)gate.WorkflowId ?? DBNull.Value);
        command.Parameters.AddWithValue("goldenExecutionId", gate.GoldenExecutionId.Value);
        command.Parameters.AddWithValue("rules", rulesJson);
        command.Parameters.AddWithValue("isActive", gate.IsActive);
        command.Parameters.AddWithValue("createdAtUtc", gate.CreatedAtUtc);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        
        if (result == null)
            throw new InvalidOperationException("Failed to create regression gate");

        return gate;
    }

    public async Task<RegressionGate?> GetByIdAsync(RegressionGateId id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, owner_id, name, description, workflow_id, golden_execution_id, rules, is_active, created_at_utc
            FROM regression_gates
            WHERE id = @id";

        await using var connection = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        try { return MapToRegressionGate(reader); }
        catch { return null; }
    }

    public async Task<IReadOnlyList<RegressionGate>> ListByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, owner_id, name, description, workflow_id, golden_execution_id, rules, is_active, created_at_utc
            FROM regression_gates
            WHERE owner_id = @ownerId
            ORDER BY created_at_utc DESC";

        await using var connection = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("ownerId", userId.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        var gates = new List<RegressionGate>();
        while (await reader.ReadAsync(cancellationToken))
        {
            try { gates.Add(MapToRegressionGate(reader)); }
            catch { /* skip rows with invalid/stale data */ }
        }

        return gates;
    }

    public async Task<IReadOnlyList<RegressionGate>> ListByWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, owner_id, name, description, workflow_id, golden_execution_id, rules, is_active, created_at_utc
            FROM regression_gates
            WHERE workflow_id = @workflowId AND is_active = true
            ORDER BY created_at_utc DESC";

        await using var connection = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("workflowId", workflowId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        var gates = new List<RegressionGate>();
        while (await reader.ReadAsync(cancellationToken))
        {
            try { gates.Add(MapToRegressionGate(reader)); }
            catch { /* skip rows with invalid/stale data */ }
        }

        return gates;
    }

    public async Task<bool> DeleteAsync(RegressionGateId id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM regression_gates WHERE id = @id";

        await using var connection = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id.Value);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<bool> UpdateIsActiveAsync(RegressionGateId id, bool isActive, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE regression_gates 
            SET is_active = @isActive 
            WHERE id = @id";

        await using var connection = await _dbContext.GetConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id.Value);
        command.Parameters.AddWithValue("isActive", isActive);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    private static RegressionGate MapToRegressionGate(NpgsqlDataReader reader)
    {
        var id = new RegressionGateId(reader.GetGuid(0));
        var ownerId = new UserId(reader.GetGuid(1));
        var name = reader.GetString(2);
        var description = reader.IsDBNull(3) ? null : reader.GetString(3);
        var workflowId = reader.IsDBNull(4) ? null : reader.GetString(4);
        var goldenExecutionId = new GoldenExecutionId(reader.GetString(5));
        var rulesJson = reader.GetString(6);
        var rules = DeserializeRules(rulesJson);
        var isActive = reader.GetBoolean(7);
        var createdAtUtc = reader.GetDateTime(8);

        return new RegressionGate(
            id,
            ownerId,
            name,
            goldenExecutionId,
            rules,
            description,
            workflowId,
            isActive,
            createdAtUtc
        );
    }

    private static string SerializeRules(IReadOnlyList<DivergenceRule> rules)
    {
        var ruleDtos = rules.Select(r => new
        {
            type = r.Type.ToStringValue(),
            threshold = r.Threshold
        }).ToList();

        return JsonSerializer.Serialize(ruleDtos);
    }

    private static List<DivergenceRule> DeserializeRules(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var ruleDtos = JsonSerializer.Deserialize<List<RuleDto>>(json, options)
            ?? throw new InvalidOperationException("Failed to deserialize rules");

        return ruleDtos
            .Where(dto => !string.IsNullOrWhiteSpace(dto.Type))
            .Select(dto =>
            {
                try { return (DivergenceRule?)new DivergenceRule(DivergenceRuleTypeExtensions.FromStringValue(dto.Type), dto.Threshold); }
                catch { return null; }
            })
            .Where(r => r != null)
            .Select(r => r!)
            .ToList();
    }

    private class RuleDto
    {
        public string Type { get; set; } = string.Empty;
        public double Threshold { get; set; }
    }
}