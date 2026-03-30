using Npgsql;
using Arc.Domain.Models;
using Arc.Application.LLM;
namespace Arc.Infrastructure.LLM;
using Arc.Application.Persistence;


public sealed class PostgresLLMConfigurationRepository : ILLMConfigurationRepository
{
    private readonly IDatabaseContext _dbContext;

    public PostgresLLMConfigurationRepository(IDatabaseContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<LLMConfiguration?> GetByIdAsync(string id, UserId userId)
    {
        await using var conn = await _dbContext.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT id, name, base_url, model, api_key, endpoint, auth_type, headers, created_by, created_at, is_active " +
            "FROM llm_configurations WHERE id = @id AND created_by = @userId", conn);
        
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("userId", userId.Value);

        await using var reader = await cmd.ExecuteReaderAsync();
        return reader.Read() ? MapToConfig(reader) : null;
    }

    public async Task<List<LLMConfiguration>> ListByUserAsync(UserId userId)
    {
        await using var conn = await _dbContext.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT id, name, base_url, model, api_key, endpoint, auth_type, headers, created_by, created_at, is_active " +
            "FROM llm_configurations WHERE created_by = @userId ORDER BY created_at DESC", conn);
        
        cmd.Parameters.AddWithValue("userId", userId.Value);

        var configs = new List<LLMConfiguration>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (reader.Read()) configs.Add(MapToConfig(reader));
        return configs;
    }

    public async Task CreateAsync(LLMConfiguration config)
    {
        await using var conn = await _dbContext.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO llm_configurations (id, name, base_url, model, api_key, endpoint, auth_type, headers, created_by, created_at, is_active) " +
            "VALUES (@id, @name, @baseUrl, @model, @apiKey, @endpoint, @authType, @headers, @createdBy, @createdAt, @isActive)", conn);

        cmd.Parameters.AddWithValue("id", config.Id);
        cmd.Parameters.AddWithValue("name", config.Name);
        cmd.Parameters.AddWithValue("baseUrl", config.BaseUrl);
        cmd.Parameters.AddWithValue("model", config.Model);
        cmd.Parameters.AddWithValue("apiKey", config.ApiKey ?? string.Empty);
        cmd.Parameters.AddWithValue("endpoint", config.Endpoint);
        cmd.Parameters.AddWithValue("authType", config.AuthType);
        cmd.Parameters.AddWithValue("headers", NpgsqlTypes.NpgsqlDbType.Jsonb, System.Text.Json.JsonSerializer.Serialize(config.Headers));
        cmd.Parameters.AddWithValue("createdBy", config.CreatedBy.Value);
        cmd.Parameters.AddWithValue("createdAt", config.CreatedAt);
        cmd.Parameters.AddWithValue("isActive", config.IsActive);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateAsync(LLMConfiguration config)
    {
        await using var conn = await _dbContext.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "UPDATE llm_configurations SET name = @name, model = @model, api_key = @apiKey, " +
            "base_url = @baseUrl, endpoint = @endpoint, auth_type = @authType, headers = @headers, is_active = @isActive WHERE id = @id AND created_by = @createdBy", conn);

        cmd.Parameters.AddWithValue("id", config.Id);
        cmd.Parameters.AddWithValue("name", config.Name);
        cmd.Parameters.AddWithValue("model", config.Model);
        cmd.Parameters.AddWithValue("apiKey", config.ApiKey ?? string.Empty);
        cmd.Parameters.AddWithValue("baseUrl", config.BaseUrl);
        cmd.Parameters.AddWithValue("endpoint", config.Endpoint);
        cmd.Parameters.AddWithValue("authType", config.AuthType);
        cmd.Parameters.AddWithValue("headers", NpgsqlTypes.NpgsqlDbType.Jsonb, System.Text.Json.JsonSerializer.Serialize(config.Headers));
        cmd.Parameters.AddWithValue("isActive", config.IsActive);
        cmd.Parameters.AddWithValue("createdBy", config.CreatedBy.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(string id, UserId userId)
    {
        await using var conn = await _dbContext.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            "DELETE FROM llm_configurations WHERE id = @id AND created_by = @userId", conn);
        
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("userId", userId.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    private static LLMConfiguration MapToConfig(NpgsqlDataReader reader)
    {
        var headersJson = reader.GetString(7);
        var headers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson) 
                      ?? new Dictionary<string, string>();
        
        return (LLMConfiguration)Activator.CreateInstance(
            typeof(LLMConfiguration),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new object?[]
            {
                reader.GetString(0),  // id
                reader.GetString(1),  // name
                reader.GetString(2),  // base_url
                reader.GetString(3),  // model
                reader.IsDBNull(4) ? null : reader.GetString(4),  // api_key
                reader.GetString(5),  // endpoint
                reader.GetString(6),  // auth_type
                headers,              // headers
                new UserId(reader.GetGuid(8)),  // created_by
                reader.GetDateTime(9),  // created_at
                reader.GetBoolean(10)  // is_active
            },
            null)!;
    }
}