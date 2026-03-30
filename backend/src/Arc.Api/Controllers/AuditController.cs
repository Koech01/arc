using Arc.Api.DTOs.Audit;
using Microsoft.AspNetCore.Mvc;
using Arc.Application.Telemetry;


namespace Arc.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditLogger _auditLogger;

        public AuditController(IAuditLogger auditLogger)
        {
            _auditLogger = auditLogger;
        }

        /// <summary>
        /// Retrieves deterministic audit logs for a given execution ID.
        /// </summary>
        [HttpGet("{executionId}")]
        public async Task<IActionResult> GetLogs(
            string executionId,
            [FromQuery] AuditEventType? eventType,
            [FromQuery] string? taskId
        )
        {
            if (string.IsNullOrWhiteSpace(executionId))
                return BadRequest("ExecutionId cannot be empty.");

            var logs = await _auditLogger.GetExecutionLogsAsync(
                executionId,
                eventType,
                taskId
            );

            if (!logs.Any())
                return NotFound();

            var dto = logs.Select(AuditLogEntryDto.FromDomain);
            return Ok(dto);
        }
    }
}