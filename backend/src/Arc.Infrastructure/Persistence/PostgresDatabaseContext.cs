using Npgsql;
using Arc.Application.Persistence;
using Microsoft.Extensions.Logging;
namespace Arc.Infrastructure.Persistence;


/// <summary>
/// PostgreSQL implementation of database context.
/// Handles connection management and schema initialization.
/// </summary>
public sealed class PostgresDatabaseContext : IDatabaseContext
{
    private readonly string _connectionString;
    private readonly ILogger<PostgresDatabaseContext> _logger;

    public PostgresDatabaseContext(string connectionString, ILogger<PostgresDatabaseContext> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing PostgreSQL database schema");

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            -- Users table
            CREATE TABLE IF NOT EXISTS users (
                id UUID PRIMARY KEY,
                username VARCHAR(50) NOT NULL,
                email VARCHAR(255) UNIQUE NOT NULL,
                password_hash VARCHAR(255) NOT NULL,
                role VARCHAR(50) NOT NULL,
                firstname VARCHAR(100),
                created_at_utc TIMESTAMP NOT NULL,
                updated_at_utc TIMESTAMP NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT true
            );

            CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
            CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);

            -- Password reset tokens table
            CREATE TABLE IF NOT EXISTS password_reset_tokens (
                id UUID PRIMARY KEY,
                user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                token VARCHAR(255) UNIQUE NOT NULL,
                expires_at_utc TIMESTAMP NOT NULL,
                used BOOLEAN NOT NULL DEFAULT false,
                created_at_utc TIMESTAMP NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_password_reset_tokens_token ON password_reset_tokens(token);
            CREATE INDEX IF NOT EXISTS idx_password_reset_tokens_user_id ON password_reset_tokens(user_id);

            -- LLM configurations table (must precede execution_templates and workflows)
            CREATE TABLE IF NOT EXISTS llm_configurations (
                id VARCHAR(16) PRIMARY KEY,
                name VARCHAR(200) NOT NULL,
                base_url VARCHAR(500) NOT NULL,
                model VARCHAR(200) NOT NULL,
                api_key VARCHAR(500),
                endpoint VARCHAR(200) NOT NULL DEFAULT 'chat/completions',
                auth_type VARCHAR(50) NOT NULL DEFAULT 'bearer',
                headers JSONB NOT NULL DEFAULT '{}'::jsonb,
                created_by UUID NOT NULL,
                created_at TIMESTAMP NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT true,
                FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE CASCADE,
                UNIQUE(created_by, name)
            );

            CREATE INDEX IF NOT EXISTS idx_llm_configs_user ON llm_configurations(created_by);

            -- Execution results table
            CREATE TABLE IF NOT EXISTS execution_results (
                execution_id VARCHAR(255) PRIMARY KEY,
                user_id UUID NOT NULL,
                created_at_utc TIMESTAMP NOT NULL,
                task_count INTEGER NOT NULL,
                status VARCHAR(50) NOT NULL,
                execution_time_ms BIGINT NOT NULL,
                result_json TEXT NOT NULL,
                workflow_id VARCHAR(255) NULL,
                workflow_name VARCHAR(200) NULL,
                workflow_description VARCHAR(1000) NULL,
                is_archived BOOLEAN NOT NULL DEFAULT false,
                archived_at_utc TIMESTAMP NULL,
                archived_by UUID NULL,
                archive_reason VARCHAR(500) NULL,
                retention_expires_at_utc TIMESTAMP NULL,
                FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
                FOREIGN KEY (archived_by) REFERENCES users(id) ON DELETE SET NULL
            );

            -- Idempotent column additions for existing deployments
            ALTER TABLE execution_results ADD COLUMN IF NOT EXISTS workflow_id VARCHAR(255) NULL;
            ALTER TABLE execution_results ADD COLUMN IF NOT EXISTS workflow_name VARCHAR(200) NULL;
            ALTER TABLE execution_results ADD COLUMN IF NOT EXISTS workflow_description VARCHAR(1000) NULL;
            ALTER TABLE execution_results ADD COLUMN IF NOT EXISTS is_archived BOOLEAN NOT NULL DEFAULT false;

            CREATE INDEX IF NOT EXISTS idx_execution_results_user_id ON execution_results(user_id);
            CREATE INDEX IF NOT EXISTS idx_execution_results_created_at ON execution_results(created_at_utc);
            CREATE INDEX IF NOT EXISTS idx_execution_results_status ON execution_results(status);
            CREATE INDEX IF NOT EXISTS idx_execution_results_workflow_id ON execution_results(workflow_id);
            CREATE INDEX IF NOT EXISTS idx_execution_results_is_archived ON execution_results(is_archived);
            CREATE INDEX IF NOT EXISTS idx_execution_results_archived_at ON execution_results(archived_at_utc);
            CREATE INDEX IF NOT EXISTS idx_execution_results_retention_expires ON execution_results(retention_expires_at_utc);

            -- Audit logs table
            CREATE TABLE IF NOT EXISTS audit_logs (
                id BIGSERIAL PRIMARY KEY,
                execution_id VARCHAR(255) NOT NULL,
                sequence_number INTEGER NOT NULL,
                event_type VARCHAR(100) NOT NULL,
                task_id VARCHAR(255),
                message TEXT NOT NULL,
                timestamp_utc TIMESTAMP NOT NULL,
                UNIQUE(execution_id, sequence_number)
            );

            CREATE INDEX IF NOT EXISTS idx_audit_logs_execution_id ON audit_logs(execution_id);
            CREATE INDEX IF NOT EXISTS idx_audit_logs_event_type ON audit_logs(event_type);
            CREATE INDEX IF NOT EXISTS idx_audit_logs_task_id ON audit_logs(task_id);

            -- Task execution cache table
            CREATE TABLE IF NOT EXISTS task_execution_cache (
                task_hash VARCHAR(64) PRIMARY KEY,
                task_id VARCHAR(255) NOT NULL,
                task_name VARCHAR(500) NOT NULL,
                output TEXT NOT NULL,
                status VARCHAR(50) NOT NULL,
                cached_at_utc TIMESTAMP NOT NULL,
                expires_at_utc TIMESTAMP NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_task_cache_expires_at ON task_execution_cache(expires_at_utc);

            -- Execution templates table
            CREATE TABLE IF NOT EXISTS execution_templates (
                name VARCHAR(255) NOT NULL,
                user_id UUID NOT NULL,
                description TEXT,
                tasks_json TEXT NOT NULL,
                trigger_type VARCHAR(50) NOT NULL DEFAULT 'manual',
                llm_config_id VARCHAR(16),
                created_at_utc TIMESTAMP NOT NULL,
                use_count INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (name, user_id),
                FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
                FOREIGN KEY (llm_config_id) REFERENCES llm_configurations(id) ON DELETE SET NULL
            );

            -- Idempotent migration: add user_id column to pre-existing deployments
            ALTER TABLE execution_templates ADD COLUMN IF NOT EXISTS user_id UUID NULL;
            UPDATE execution_templates
            SET user_id = (SELECT id FROM users WHERE role = 'Admin' ORDER BY created_at_utc ASC LIMIT 1)
            WHERE user_id IS NULL;

            -- Workflows table
            CREATE TABLE IF NOT EXISTS workflows (
                id VARCHAR(255) PRIMARY KEY,
                name VARCHAR(200) NOT NULL,
                description VARCHAR(1000),
                tasks_json TEXT NOT NULL,
                trigger_type VARCHAR(50) NOT NULL,
                llm_config_id VARCHAR(16),
                created_by UUID NOT NULL,
                created_at TIMESTAMP NOT NULL,
                FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE CASCADE,
                FOREIGN KEY (llm_config_id) REFERENCES llm_configurations(id) ON DELETE SET NULL
            );

            CREATE INDEX IF NOT EXISTS idx_workflows_created_by ON workflows(created_by);
            CREATE INDEX IF NOT EXISTS idx_workflows_name ON workflows(name);

            -- Webhooks table
            CREATE TABLE IF NOT EXISTS webhooks (
                id UUID PRIMARY KEY,
                url VARCHAR(2048) NOT NULL,
                events_json TEXT NOT NULL,
                secret VARCHAR(255) NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT true,
                created_by UUID NOT NULL,
                created_at TIMESTAMP NOT NULL,
                FOREIGN KEY (created_by) REFERENCES users(id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_webhooks_created_by ON webhooks(created_by);
            CREATE INDEX IF NOT EXISTS idx_webhooks_is_active ON webhooks(is_active);

            -- Notifications table
            CREATE TABLE IF NOT EXISTS notifications (
                id UUID PRIMARY KEY,
                user_id UUID NOT NULL,
                title VARCHAR(255) NOT NULL,
                message VARCHAR(2000) NOT NULL,
                type VARCHAR(20) NOT NULL,
                is_read BOOLEAN NOT NULL DEFAULT false,
                created_at TIMESTAMP NOT NULL,
                FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_notifications_user_id ON notifications(user_id);
            CREATE INDEX IF NOT EXISTS idx_notifications_created_at ON notifications(created_at);
            CREATE INDEX IF NOT EXISTS idx_notifications_is_read ON notifications(is_read);

            -- Execution archive audit table
            CREATE TABLE IF NOT EXISTS execution_archive_audit (
                id BIGSERIAL PRIMARY KEY,
                execution_id VARCHAR(255) NOT NULL,
                action VARCHAR(20) NOT NULL,
                performed_by UUID NOT NULL,
                performed_at_utc TIMESTAMP NOT NULL,
                reason VARCHAR(500),
                ip_address VARCHAR(45),
                user_agent VARCHAR(500),
                FOREIGN KEY (execution_id) REFERENCES execution_results(execution_id) ON DELETE CASCADE,
                FOREIGN KEY (performed_by) REFERENCES users(id) ON DELETE SET NULL
            );

            CREATE INDEX IF NOT EXISTS idx_archive_audit_execution_id ON execution_archive_audit(execution_id);
            CREATE INDEX IF NOT EXISTS idx_archive_audit_performed_by ON execution_archive_audit(performed_by);
            CREATE INDEX IF NOT EXISTS idx_archive_audit_performed_at ON execution_archive_audit(performed_at_utc);
            CREATE INDEX IF NOT EXISTS idx_archive_audit_action ON execution_archive_audit(action);

            -- User preferences table
            CREATE TABLE IF NOT EXISTS user_preferences (
                user_id UUID PRIMARY KEY,
                theme VARCHAR(20) NOT NULL DEFAULT 'system',
                notification_email BOOLEAN NOT NULL DEFAULT true,
                notification_push BOOLEAN NOT NULL DEFAULT false,
                notification_execution_complete BOOLEAN NOT NULL DEFAULT true,
                notification_execution_failed BOOLEAN NOT NULL DEFAULT true,
                language VARCHAR(10) NOT NULL DEFAULT 'en',
                timezone VARCHAR(50) NOT NULL DEFAULT 'UTC',
                FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
            );

            -- ── Admin features: idempotent column additions ─────────────────────────
            ALTER TABLE users ADD COLUMN IF NOT EXISTS failed_login_attempts INTEGER NOT NULL DEFAULT 0;
            ALTER TABLE users ADD COLUMN IF NOT EXISTS locked_until_utc TIMESTAMP NULL;
            ALTER TABLE users ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP NULL;

            CREATE INDEX IF NOT EXISTS idx_users_deleted_at ON users(deleted_at);
            CREATE INDEX IF NOT EXISTS idx_users_locked_until ON users(locked_until_utc);

            -- Admin action audit log (append-only)
            CREATE TABLE IF NOT EXISTS admin_audit_log (
                id              BIGSERIAL PRIMARY KEY,
                admin_user_id   UUID NOT NULL,
                action          VARCHAR(100) NOT NULL,
                timestamp_utc   TIMESTAMP NOT NULL,
                target_user_id  VARCHAR(255),
                detail          VARCHAR(1000),
                ip_address      VARCHAR(45),
                user_agent      VARCHAR(500)
            );

            CREATE INDEX IF NOT EXISTS idx_admin_audit_admin_user ON admin_audit_log(admin_user_id);
            CREATE INDEX IF NOT EXISTS idx_admin_audit_timestamp   ON admin_audit_log(timestamp_utc);
            CREATE INDEX IF NOT EXISTS idx_admin_audit_action       ON admin_audit_log(action);

            -- Login history
            CREATE TABLE IF NOT EXISTS login_history (
                id              BIGSERIAL PRIMARY KEY,
                user_id         UUID NOT NULL,
                timestamp_utc   TIMESTAMP NOT NULL,
                success         BOOLEAN NOT NULL,
                failure_reason  VARCHAR(100),
                ip_address      VARCHAR(45),
                user_agent      VARCHAR(500),
                FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_login_history_user_id    ON login_history(user_id);
            CREATE INDEX IF NOT EXISTS idx_login_history_timestamp   ON login_history(timestamp_utc);
            CREATE INDEX IF NOT EXISTS idx_login_history_success     ON login_history(success);

            -- Golden executions table (for regression testing)
            CREATE TABLE IF NOT EXISTS golden_executions (
                execution_id VARCHAR(255) PRIMARY KEY,
                owner_id UUID NOT NULL,
                label VARCHAR(255),
                marked_at_utc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (owner_id) REFERENCES users(id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_golden_executions_owner ON golden_executions(owner_id);

            -- Regression gates table
            CREATE TABLE IF NOT EXISTS regression_gates (
                id UUID PRIMARY KEY,
                owner_id UUID NOT NULL,
                name VARCHAR(200) NOT NULL,
                description TEXT,
                workflow_id VARCHAR(255),
                golden_execution_id VARCHAR(255) NOT NULL,
                rules JSONB NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT true,
                created_at_utc TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (owner_id) REFERENCES users(id) ON DELETE CASCADE,
                UNIQUE(owner_id, name)
            );

            CREATE INDEX IF NOT EXISTS idx_regression_gates_owner ON regression_gates(owner_id);
            CREATE INDEX IF NOT EXISTS idx_regression_gates_workflow ON regression_gates(workflow_id);
        ";

        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("PostgreSQL database schema initialized successfully");
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return false;
        }
    }
}