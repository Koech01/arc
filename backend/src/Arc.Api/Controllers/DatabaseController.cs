using Microsoft.AspNetCore.Mvc;
using Arc.Application.Persistence;


namespace Arc.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
public sealed class DatabaseController : ControllerBase
{
    private readonly IDatabaseContext _dbContext;
    private readonly ILogger<DatabaseController> _logger;

    public DatabaseController(IDatabaseContext dbContext, ILogger<DatabaseController> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks database connectivity and health.
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> HealthCheck()
    {
        var isHealthy = await _dbContext.HealthCheckAsync();
        
        if (isHealthy)
        {
            _logger.LogInformation("Database health check passed");
            return Ok(new
            {
                Provider = "PostgreSQL",
                Status = "Healthy",
                Message = "Database connection successful"
            });
        }

        _logger.LogWarning("Database health check failed");
        return StatusCode(503, new
        {
            Provider = "PostgreSQL",
            Status = "Unhealthy",
            Message = "Database connection failed"
        });
    }

    /// <summary>
    /// Deletes all executions and workflows from the database.
    /// </summary>
    [HttpDelete("clear")]
    public async Task<IActionResult> ClearData()
    {
        try
        {
            await using var connection = await _dbContext.OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            
            command.CommandText = @"
                DELETE FROM execution_results;
                DELETE FROM workflows;";
            
            await command.ExecuteNonQueryAsync();
            
            _logger.LogInformation("Successfully deleted all executions and workflows");
            return Ok(new { Message = "All executions and workflows deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete executions and workflows");
            return StatusCode(500, new { Message = "Failed to delete data", Error = ex.Message });
        }
    }
}