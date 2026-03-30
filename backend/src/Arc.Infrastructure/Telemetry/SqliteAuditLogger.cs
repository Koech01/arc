using Microsoft.Data.Sqlite;
using Arc.Application.Telemetry;


namespace Arc.Infrastructure.Telemetry
{
    public class SqliteAuditLogger : IAuditLogger, IDisposable
    {
        private readonly string _dbPath;
        private readonly SqliteConnection _connection;
        private readonly object _lock = new();

        public SqliteAuditLogger(string dbPath = "./data/audit_logs.db")
        {
            _dbPath = dbPath;
            var dir = Path.GetDirectoryName(Path.GetFullPath(_dbPath));
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            _connection = new SqliteConnection($"Data Source={_dbPath}");
            _connection.Open();

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS AuditLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ExecutionId TEXT NOT NULL,
                    Sequence INTEGER NOT NULL,
                    TimestampUtc TEXT NOT NULL,
                    EventType TEXT NOT NULL,
                    TaskId TEXT,
                    Message TEXT
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_Execution_Sequence 
                    ON AuditLogs(ExecutionId, Sequence);
            ";
            cmd.ExecuteNonQuery();
        }

        public Task LogAsync(string executionId, AuditEventType eventType, string? taskId = null, string? message = null)
        {
            lock (_lock)
            {
                var sequence = GetNextSequence(executionId);
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO AuditLogs (ExecutionId, Sequence, TimestampUtc, EventType, TaskId, Message)
                    VALUES ($executionId, $sequence, $timestamp, $eventType, $taskId, $message);
                ";
                cmd.Parameters.AddWithValue("$executionId", executionId);
                cmd.Parameters.AddWithValue("$sequence", sequence);
                cmd.Parameters.AddWithValue("$timestamp", DateTime.UtcNow.ToString("o"));
                cmd.Parameters.AddWithValue("$eventType", eventType.ToString());
                cmd.Parameters.AddWithValue("$taskId", taskId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("$message", message ?? (object)DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            return Task.CompletedTask;
        }

        public Task LogImportedAsync(
            string executionId,
            long sequence,
            DateTime timestampUtc,
            AuditEventType eventType,
            string? taskId = null,
            string? message = null)
        {
            lock (_lock)
            {
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO AuditLogs (ExecutionId, Sequence, TimestampUtc, EventType, TaskId, Message)
                    VALUES ($executionId, $sequence, $timestamp, $eventType, $taskId, $message)
                    ON CONFLICT(ExecutionId, Sequence) DO UPDATE SET
                        TimestampUtc = $timestamp,
                        EventType = $eventType,
                        TaskId = $taskId,
                        Message = $message;
                ";
                cmd.Parameters.AddWithValue("$executionId", executionId);
                cmd.Parameters.AddWithValue("$sequence", sequence);
                cmd.Parameters.AddWithValue("$timestamp", timestampUtc.ToString("o"));
                cmd.Parameters.AddWithValue("$eventType", eventType.ToString());
                cmd.Parameters.AddWithValue("$taskId", taskId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("$message", message ?? (object)DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditLogEntry>> GetExecutionLogsAsync(string executionId)
        {
            return GetExecutionLogsAsync(executionId, null, null);
        }

        public Task<IReadOnlyList<AuditLogEntry>> GetExecutionLogsAsync(
            string executionId,
            AuditEventType? eventType,
            string? taskId
        )
        {
            lock (_lock)
            {
                var entries = new List<AuditLogEntry>();

                using var cmd = _connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT ExecutionId, Sequence, TimestampUtc, EventType, TaskId, Message
                    FROM AuditLogs
                    WHERE ExecutionId = $executionId
                    AND ($eventType IS NULL OR EventType = $eventType)
                    AND ($taskId IS NULL OR TaskId = $taskId)
                    ORDER BY Sequence ASC;
                ";

                cmd.Parameters.AddWithValue("$executionId", executionId);
                cmd.Parameters.AddWithValue(
                    "$eventType",
                    eventType?.ToString() ?? (object)DBNull.Value
                );
                cmd.Parameters.AddWithValue(
                    "$taskId",
                    taskId ?? (object)DBNull.Value
                );

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    entries.Add(new AuditLogEntry(
                        reader.GetString(0),
                        reader.GetInt64(1),
                        DateTime.Parse(reader.GetString(2)),
                        Enum.Parse<AuditEventType>(reader.GetString(3)),
                        reader.IsDBNull(4) ? null : reader.GetString(4),
                        reader.IsDBNull(5) ? null : reader.GetString(5)
                    ));
                }

                return Task.FromResult((IReadOnlyList<AuditLogEntry>)entries);
            }
        }


        private long GetNextSequence(string executionId)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT IFNULL(MAX(Sequence), 0) + 1 FROM AuditLogs WHERE ExecutionId = $executionId;";
            cmd.Parameters.AddWithValue("$executionId", executionId);
            var result = cmd.ExecuteScalar();
            return Convert.ToInt64(result);
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}