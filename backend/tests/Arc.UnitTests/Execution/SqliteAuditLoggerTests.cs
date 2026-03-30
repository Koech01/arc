using FluentAssertions;
using Arc.Application.Telemetry;
using Arc.Infrastructure.Telemetry;


namespace Arc.UnitTests.Execution
{
    public class SqliteAuditLoggerTests
    {
        [Fact]
        public async Task LogsAreDeterministicForSameExecutionId()
        {
            var dbPath = $"./test_audit_{Guid.NewGuid():N}.db";
            try
            {
            var logger = new SqliteAuditLogger(dbPath);
            var executionId = "deterministic-test";

            await logger.LogAsync(executionId, AuditEventType.OrchestratorStarted);
            await logger.LogAsync(executionId, AuditEventType.TaskStarted, "task1");
            await logger.LogAsync(executionId, AuditEventType.TaskFinished, "task1");
            await logger.LogAsync(executionId, AuditEventType.OrchestratorFinished);

            var logs1 = await logger.GetExecutionLogsAsync(executionId);
            var logs2 = await logger.GetExecutionLogsAsync(executionId);

            logs1.Count.Should().Be(4);
            logs1.Select(l => l.Sequence).Should().BeInAscendingOrder();
            logs1.Should().BeEquivalentTo(logs2);
            logger.Dispose();
            }
            finally { if (File.Exists(dbPath)) File.Delete(dbPath); }
        }

        [Fact]
        public async Task CanFilterByEventTypeAndTaskIdDeterministically()
        {
            var dbPath = $"./test_filtering_{Guid.NewGuid():N}.db";
            try
            {
            var logger = new SqliteAuditLogger(dbPath);
            var executionId = "filter-test";

            await logger.LogAsync(executionId, AuditEventType.OrchestratorStarted);
            await logger.LogAsync(executionId, AuditEventType.TaskStarted, "task1");
            await logger.LogAsync(executionId, AuditEventType.TaskFinished, "task1");
            await logger.LogAsync(executionId, AuditEventType.TaskStarted, "task2");
            await logger.LogAsync(executionId, AuditEventType.OrchestratorFinished);

            var filtered = await logger.GetExecutionLogsAsync(
                executionId,
                AuditEventType.TaskStarted,
                "task1"
            );

            filtered.Count.Should().Be(1);
            filtered[0].EventType.Should().Be(AuditEventType.TaskStarted);
            filtered[0].TaskId.Should().Be("task1");
            logger.Dispose();
            }
            finally { if (File.Exists(dbPath)) File.Delete(dbPath); }
        }
    }
}