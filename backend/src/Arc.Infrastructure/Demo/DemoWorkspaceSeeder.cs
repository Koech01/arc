using System.Reflection;
using System.Text.Json;
using Arc.Application.Execution;
using Arc.Application.Identity;
using Arc.Application.LLM;
using Arc.Application.Notifications;
using Arc.Application.Persistence;
using Arc.Application.RegressionGates;
using Arc.Application.Results;
using Arc.Application.Telemetry;
using Arc.Application.Webhooks;
using Arc.Application.Workflows;
using Arc.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Arc.Infrastructure.Demo;

/// <summary>
/// Idempotent demo workspace seeder. Creates a fully populated portfolio workspace
/// with fixed, deterministic data (demo@arc.com).
/// </summary>
public sealed class DemoWorkspaceSeeder
{
    public const string DemoEmail = "demo@arc.com";

    private readonly IUserRepository _users;
    private readonly IPasswordHashingService _hasher;
    private readonly IUserPreferencesRepository _prefs;
    private readonly ILLMConfigurationRepository _llms;
    private readonly IWorkflowRepository _workflows;
    private readonly IExecutionTemplateStore _templates;
    private readonly IExecutionResultStore _executions;
    private readonly IAuditLogger _audit;
    private readonly IRegressionGateRepository _gates;
    private readonly IWebhookRepository _webhooks;
    private readonly INotificationRepository _notifications;
    private readonly IDatabaseContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DemoWorkspaceSeeder> _logger;

    public DemoWorkspaceSeeder(
        IUserRepository users,
        IPasswordHashingService hasher,
        IUserPreferencesRepository prefs,
        ILLMConfigurationRepository llms,
        IWorkflowRepository workflows,
        IExecutionTemplateStore templates,
        IExecutionResultStore executions,
        IAuditLogger audit,
        IRegressionGateRepository gates,
        IWebhookRepository webhooks,
        INotificationRepository notifications,
        IDatabaseContext db,
        IConfiguration configuration,
        ILogger<DemoWorkspaceSeeder> logger)
    {
        _users = users;
        _hasher = hasher;
        _prefs = prefs;
        _llms = llms;
        _workflows = workflows;
        _templates = templates;
        _executions = executions;
        _audit = audit;
        _gates = gates;
        _webhooks = webhooks;
        _notifications = notifications;
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Called on application startup. Seeds when enabled and demo user is missing,
    /// or when <c>DemoWorkspace:ForceReseed</c> is true.
    /// </summary>
    public async Task SeedOnStartupAsync(CancellationToken cancellationToken = default)
    {
        if (!IsEnabled())
        {
            _logger.LogInformation("Demo workspace seeding is disabled");
            return;
        }

        var forceReseed = bool.TryParse(_configuration["DemoWorkspace:ForceReseed"], out var force) && force;
        var existing = await _users.GetByEmailAsync(DemoEmail);

        if (!forceReseed && existing is not null)
        {
            _logger.LogInformation("Demo user already exists, skipping seed");
            return;
        }

        await RunFullSeedAsync(cancellationToken);
        _logger.LogInformation("Demo workspace seeded ({Email})", DemoEmail);
    }

    /// <summary>
    /// Force a full idempotent reseed (delete demo user + recreate). Used by make seed-demo.
    /// </summary>
    public Task ReseedAsync(CancellationToken cancellationToken = default) =>
        RunFullSeedAsync(cancellationToken);

    private bool IsEnabled()
    {
        var enabledStr = _configuration["DemoWorkspace:Enabled"];
        return !string.IsNullOrEmpty(enabledStr) && bool.Parse(enabledStr);
    }

    private async Task RunFullSeedAsync(CancellationToken cancellationToken = default)
    {
        await CleanupAsync(cancellationToken);
        await SeedUserAsync();
        await SeedPreferencesAsync();
        await SeedLlmConfigsAsync();
        await SeedWorkflowsAsync();
        await SeedTemplatesAsync();
        await SeedExecutionsAsync();
        await SeedGoldenExecutionsAsync();
        await SeedRegressionGatesAsync();
        await SeedWebhooksAsync();
        await SeedNotificationsAsync();
    }

    // ── Fixed demo identity ───────────────────────────────────────────────
    const string Email = DemoEmail;
    const string Username = "amorgan";
    const string Password = "DemoArc2026!";
    const string DisplayName = "Alex Morgan";

    static readonly Guid UserGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");
    static readonly UserId UserId = UserId.From(UserGuid);
    static readonly DateTime UserCreatedAt = new(2025, 12, 15, 9, 0, 0, DateTimeKind.Utc);

    const string LlmGpt4o = "a1b2c3d4e5f67890";
    const string LlmClaude = "b2c3d4e5f6789012";
    const string LlmGemini = "c3d4e5f678901234";
    const string LlmOllama = "d4e5f67890123456";

    const string WfWeekly = "wf-weekly-eng-report";
    const string WfTriage = "wf-support-triage";
    const string WfSpec = "wf-feature-spec";
    const string WfRelease = "wf-release-notes";
    const string WfMarketing = "wf-marketing-brief";

    private async Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _users.GetByEmailAsync(Email);
        if (existing is null)
        {
            _logger.LogInformation("No existing demo user — fresh seed");
            return;
        }

        _logger.LogInformation("Removing existing demo user and all owned data…");
        await using var conn = await _db.OpenConnectionAsync();
        var uid = existing.Id.Value;

        await using (var cmd = new NpgsqlCommand(
            "DELETE FROM audit_logs WHERE execution_id IN (SELECT execution_id FROM execution_results WHERE user_id = @uid)", conn))
        {
            cmd.Parameters.AddWithValue("uid", uid);
            await cmd.ExecuteNonQueryAsync();
        }

        await using (var cmd = new NpgsqlCommand(
            "DELETE FROM execution_archive_audit WHERE execution_id IN (SELECT execution_id FROM execution_results WHERE user_id = @uid)", conn))
        {
            cmd.Parameters.AddWithValue("uid", uid);
            await cmd.ExecuteNonQueryAsync();
        }

        await using (var cmd = new NpgsqlCommand("DELETE FROM users WHERE id = @uid", conn))
        {
            cmd.Parameters.AddWithValue("uid", uid);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    async Task SeedUserAsync()
    {
        var hash = _hasher.HashPassword(Password);
        var user = new User(UserId, Username, Email, hash, UserRole.User, UserCreatedAt, true, DisplayName);
        await _users.CreateAsync(user);
        _logger.LogInformation("Created demo user {Email}", Email);
    }

    async Task SeedPreferencesAsync()
    {
        var prefs = new UserPreferences(UserId, "dark", true, false, true, true, "en", "America/New_York");
        await _prefs.UpsertAsync(prefs);
    }

    async Task SeedLlmConfigsAsync()
    {
        var configs = new (string Id, string Name, string Url, string Model, DateTime At)[]
        {
            (LlmGpt4o, "GPT-4o Production", "https://api.openai.com/v1", "gpt-4o", new DateTime(2025, 12, 15, 10, 0, 0, DateTimeKind.Utc)),
            (LlmClaude, "Claude Sonnet - Content", "https://api.anthropic.com/v1", "claude-3-5-sonnet-20241022", new DateTime(2025, 12, 16, 10, 0, 0, DateTimeKind.Utc)),
            (LlmGemini, "Gemini Flash - Fast", "https://generativelanguage.googleapis.com/v1beta", "gemini-1.5-flash", new DateTime(2026, 1, 8, 10, 0, 0, DateTimeKind.Utc)),
            (LlmOllama, "Ollama Local - Dev", "http://localhost:11434/v1", "llama3:8b", new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc))
        };

        foreach (var (id, name, url, model, at) in configs)
            await _llms.CreateAsync(MakeLlm(id, name, url, model, "demo-key-not-real", UserId, at));
    }

    async Task SeedWorkflowsAsync()
    {
        foreach (var wf in WorkflowDefinitions())
            await _workflows.CreateAsync(wf);
    }

    async Task SeedTemplatesAsync()
    {
        var specs = new (string Name, string Desc, string WfId, string Llm, DateTime At, int Uses)[]
        {
            ("weekly report", "Standard weekly status report for leadership.", WfWeekly, LlmGpt4o, Dt(2025, 12, 20), 12),
            ("feature specification", "PRD template with competitor research.", WfSpec, LlmClaude, Dt(2026, 2, 10), 8),
            ("technical design review", "RFC review checklist and trade-off analysis.", WfSpec, LlmGpt4o, Dt(2026, 4, 22), 5),
            ("customer email", "Professional customer response draft.", WfTriage, LlmClaude, Dt(2026, 1, 15), 15),
            ("marketing campaign", "Full campaign brief from audience analysis.", WfMarketing, LlmClaude, Dt(2026, 4, 5), 6)
        };

        await using var conn = await _db.OpenConnectionAsync();
        foreach (var (name, desc, wfId, llm, at, uses) in specs)
        {
            var wf = WorkflowDefinitions().First(w => w.Id == wfId);
            var tasksJson = JsonSerializer.Serialize(wf.Tasks.Select(t => new
            {
                t.Id, t.Name, t.AgentType, t.Prompt, t.LLMConfigId,
                Config = t.Config,
                Dependencies = t.Dependencies
            }));

            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO execution_templates (name, user_id, description, tasks_json, trigger_type, llm_config_id, created_at_utc, use_count)
                VALUES (@name, @uid, @desc, @tasks, 'manual', @llm, @at, @uses)", conn);
            cmd.Parameters.AddWithValue("name", name);
            cmd.Parameters.AddWithValue("uid", UserGuid);
            cmd.Parameters.AddWithValue("desc", desc);
            cmd.Parameters.AddWithValue("tasks", tasksJson);
            cmd.Parameters.AddWithValue("llm", llm);
            cmd.Parameters.AddWithValue("at", at);
            cmd.Parameters.AddWithValue("uses", uses);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    async Task SeedExecutionsAsync()
    {
        var specs = ExecutionSpecs();
        var workflows = WorkflowDefinitions().ToDictionary(w => w.Id);

        foreach (var spec in specs)
        {
            var wf = workflows[spec.WorkflowId];
            var displayName = wf.Name + spec.NameSuffix;
            var orderedTasks = TopologicalSort(wf.Tasks);
            var results = BuildTaskResults(orderedTasks, spec.FailAtOrder, spec.Id);
            var result = new ExecutionResult(spec.Id, UserId, results);

            var ctx = new ExecutionWorkflowContext(
                spec.IsImport ? null : wf.Id,
                displayName,
                wf.Description);

            await _executions.StoreAsync(spec.Id, result, spec.CreatedAt, ctx);
            await SeedAuditTrailAsync(spec.Id, spec.CreatedAt, results);

            if (spec.Archive)
                await _executions.ArchiveAsync(spec.Id, UserGuid, "Quarterly archive policy", 365);
        }
    }

    async Task SeedGoldenExecutionsAsync()
    {
        var goldens = new (string ExecId, string Label, DateTime MarkedAt)[]
        {
            ("exec-005-weekly-jan20", "Weekly Report Baseline v1", Dt(2026, 1, 21)),
            ("exec-011-release-mar12", "Release Notes v2.4 Golden", Dt(2026, 3, 13)),
            ("exec-019-release-may20", "Release Notes v2.5 Golden", Dt(2026, 5, 21))
        };

        await using var conn = await _db.OpenConnectionAsync();
        foreach (var (execId, label, markedAt) in goldens)
        {
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO golden_executions (execution_id, owner_id, label, marked_at_utc)
                VALUES (@eid, @oid, @label, @at)
                ON CONFLICT (execution_id) DO UPDATE SET label = EXCLUDED.label, marked_at_utc = EXCLUDED.marked_at_utc", conn);
            cmd.Parameters.AddWithValue("eid", execId);
            cmd.Parameters.AddWithValue("oid", UserGuid);
            cmd.Parameters.AddWithValue("label", label);
            cmd.Parameters.AddWithValue("at", markedAt);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    async Task SeedRegressionGatesAsync()
    {
        var gates = new (Guid Id, string Name, string Desc, string Golden, string WfId, DivergenceRule[] Rules, DateTime At)[]
        {
            (Guid.Parse("22222222-2222-2222-2222-222222222201"), "Weekly Report Quality Gate",
                "Ensures weekly reports maintain output quality across model updates.",
                "exec-005-weekly-jan20", WfWeekly,
                [Rule(DivergenceRuleType.SimilarityPercentage, 0.95), Rule(DivergenceRuleType.NoStatusDegradation, 1.0)],
                Dt(2026, 1, 22)),
            (Guid.Parse("22222222-2222-2222-2222-222222222202"), "Release Notes Exact Match",
                "Release notes must match the v2.4 golden baseline exactly.",
                "exec-011-release-mar12", WfRelease,
                [Rule(DivergenceRuleType.SimilarityPercentage, 1.0)],
                Dt(2026, 3, 14)),
            (Guid.Parse("22222222-2222-2222-2222-222222222203"), "Feature Spec Semantic Gate",
                "Allows minor wording changes but blocks structural regressions.",
                "exec-006-spec-feb03", WfSpec,
                [Rule(DivergenceRuleType.SimilarityPercentage, 0.85), Rule(DivergenceRuleType.MaxTaskDivergence, 0.10)],
                Dt(2026, 2, 5)),
            (Guid.Parse("22222222-2222-2222-2222-222222222204"), "Support Triage Stability",
                "Critical path and task statuses must be preserved for triage workflows.",
                "exec-003-triage-jan08", WfTriage,
                [Rule(DivergenceRuleType.CriticalPathPreservation, 1.0), Rule(DivergenceRuleType.NoStatusDegradation, 1.0)],
                Dt(2026, 1, 10))
        };

        foreach (var (id, name, desc, golden, wfId, rules, at) in gates)
        {
            var gate = new RegressionGate(
                new RegressionGateId(id), UserId, name,
                new GoldenExecutionId(golden), rules, desc, wfId, true, at);
            await _gates.CreateAsync(gate);
        }
    }

    async Task SeedWebhooksAsync()
    {
        var hooks = new (Guid Id, string Url, WebhookEventType[] Events, string Secret, bool Active, DateTime At)[]
        {
            (Guid.Parse("33333333-3333-3333-3333-333333333301"),
                "https://hooks.slack.com/services/T000/B000/demo-eng",
                [WebhookEventType.ExecutionStarted, WebhookEventType.ExecutionCompleted, WebhookEventType.ExecutionFailed],
                "demo-slack-secret-32chars-min", true, Dt(2025, 12, 18)),
            (Guid.Parse("33333333-3333-3333-3333-333333333302"),
                "https://events.pagerduty.com/integration/demo/enqueue",
                [WebhookEventType.ExecutionFailed],
                "demo-pager-secret-32chars-min", true, Dt(2026, 1, 10)),
            (Guid.Parse("33333333-3333-3333-3333-333333333303"),
                "https://audit.meridian-software.internal/webhooks/arc",
                [WebhookEventType.ExecutionCompleted, WebhookEventType.ExecutionFailed],
                "demo-audit-secret-32chars-min", true, Dt(2026, 2, 1))
        };

        foreach (var (id, url, events, secret, active, at) in hooks)
        {
            var wh = new Webhook(WebhookId.From(id), url, events, secret, active, UserId, at);
            await _webhooks.CreateAsync(wh);
        }
    }

    async Task SeedNotificationsAsync()
    {
        var notifs = NotificationDefinitions();
        foreach (var n in notifs)
        {
            var notification = new Notification(
                NotificationId.From(n.Id), UserId, n.Title, n.Message, n.Type, n.IsRead, n.At);
            await _notifications.CreateAsync(notification);
        }
    }

    async Task SeedAuditTrailAsync(string executionId, DateTime start, IReadOnlyList<TaskExecutionResult> tasks)
    {
        var t = start;
        long seq = 1;
        await _audit.LogImportedAsync(executionId, seq++, t, AuditEventType.OrchestratorStarted, null, "Orchestrator started");

        foreach (var task in tasks.OrderBy(x => x.ExecutionOrder))
        {
            t = t.AddMilliseconds(400);
            await _audit.LogImportedAsync(executionId, seq++, t, AuditEventType.TaskStarted, task.TaskId,
                $"Task '{task.TaskName}' started");
            t = t.AddMilliseconds(600);
            await _audit.LogImportedAsync(executionId, seq++, t, AuditEventType.TaskFinished, task.TaskId,
                $"Task '{task.TaskName}' finished with {task.Status}");
        }

        t = t.AddMilliseconds(200);
        await _audit.LogImportedAsync(executionId, seq, t, AuditEventType.OrchestratorFinished, null, "Orchestrator finished");
    }

    // ── Workflow definitions ──────────────────────────────────────────────

    static List<Workflow> WorkflowDefinitions() =>
    [
        Wf(WfWeekly, "Weekly Engineering Report",
            "Aggregates sprint metrics, PR activity, and incidents into a leadership-ready weekly report.",
            LlmGpt4o, Dt(2025, 12, 16, 10),
            T("collect-metrics", "Collect Sprint Metrics", "http", url: "https://api.meridian.dev/metrics/sprint"),
            T("summarize-prs", "Summarize Pull Requests", "llm", ["collect-metrics"], "Summarize merged PRs by theme."),
            T("draft-report", "Draft Weekly Report", "llm", ["summarize-prs"], "Write an executive summary with velocity and blockers."),
            T("format-output", "Format for Slack", "email", ["draft-report"], channel: "#eng-leads")),

        Wf(WfTriage, "Customer Support Triage",
            "Classifies support tickets, suggests responses, and routes to the correct team.",
            LlmGemini, Dt(2026, 1, 8, 14, 30),
            T("parse-ticket", "Parse Incoming Ticket", "llm", [], "Extract customer intent and urgency."),
            T("classify-priority", "Classify Priority", "llm", ["parse-ticket"], "Assign P1-P4 priority."),
            T("suggest-response", "Suggest Response", "llm", ["classify-priority"], "Draft a professional response."),
            T("route-team", "Route to Team", "http", ["suggest-response"], url: "https://api.meridian.dev/support/route")),

        Wf(WfSpec, "Feature Specification Generator",
            "Produces PRDs with competitor research and review checklists.",
            LlmClaude, Dt(2026, 2, 3, 11),
            T("gather-requirements", "Gather Requirements", "llm", [], "Structure requirements into user stories."),
            T("research-competitors", "Research Competitors", "http", ["gather-requirements"], url: "https://api.meridian.dev/research/competitors"),
            T("write-spec", "Write Feature Spec", "llm", ["research-competitors"], "Produce a complete feature specification."),
            T("review-checklist", "Generate Review Checklist", "llm", ["write-spec"], "Create engineering review checklist.")),

        Wf(WfRelease, "Release Notes Pipeline",
            "Transforms changelog entries into customer-facing release notes.",
            LlmGpt4o, Dt(2026, 3, 12, 9),
            T("fetch-changelog", "Fetch Changelog", "http", url: "https://api.meridian.dev/releases/changelog"),
            T("categorize-changes", "Categorize Changes", "llm", ["fetch-changelog"], "Group into Features, Fixes, Breaking."),
            T("write-notes", "Write Release Notes", "llm", ["categorize-changes"], "Write polished release notes."),
            T("publish-docs", "Publish to Docs Site", "http", ["write-notes"], url: "https://docs.meridian.dev/api/publish")),

        Wf(WfMarketing, "Marketing Campaign Brief",
            "Analyzes target audience and produces a campaign brief.",
            LlmClaude, Dt(2026, 4, 1, 15),
            T("analyze-audience", "Analyze Target Audience", "llm", [], "Define ICP segments and pain points."),
            T("draft-messaging", "Draft Key Messages", "llm", ["analyze-audience"], "Create headline and value proposition."),
            T("create-brief", "Create Campaign Brief", "llm", ["draft-messaging"], "Compile channels, timeline, and metrics."))
    ];

    static List<ExecSpec> ExecutionSpecs() =>
    [
        E("exec-001-weekly-dec15", WfWeekly, "", Dt(2025, 12, 15, 16)),
        E("exec-002-weekly-dec22", WfWeekly, "", Dt(2025, 12, 22, 16)),
        E("exec-003-triage-jan08", WfTriage, "", Dt(2026, 1, 8, 15)),
        E("exec-004-triage-jan15", WfTriage, "", Dt(2026, 1, 15, 15), fail: 2),
        E("exec-005-weekly-jan20", WfWeekly, "", Dt(2026, 1, 20, 16)),
        E("exec-006-spec-feb03", WfSpec, "", Dt(2026, 2, 3, 12)),
        E("exec-007-spec-feb10", WfSpec, "", Dt(2026, 2, 10, 12)),
        E("exec-008-weekly-feb17", WfWeekly, "", Dt(2026, 2, 17, 16)),
        E("exec-009-triage-feb24", WfTriage, "", Dt(2026, 2, 24, 15)),
        E("exec-010-spec-mar03", WfSpec, "", Dt(2026, 3, 3, 12), fail: 2),
        E("exec-011-release-mar12", WfRelease, " v2.4", Dt(2026, 3, 12, 10)),
        E("exec-012-weekly-mar19", WfWeekly, "", Dt(2026, 3, 19, 16), archive: true),
        E("exec-013-release-mar26", WfRelease, " v2.4.1", Dt(2026, 3, 26, 10)),
        E("exec-014-marketing-apr01", WfMarketing, " Q2", Dt(2026, 4, 1, 16)),
        E("exec-015-design-apr20", WfSpec, " — Dashboard Export", Dt(2026, 4, 20, 11)),
        E("exec-016-weekly-apr28", WfWeekly, "", Dt(2026, 4, 28, 16)),
        E("exec-017-marketing-may05", WfMarketing, "", Dt(2026, 5, 5, 16), fail: 1),
        E("exec-018-competitive-may12", WfSpec, " — Competitive Analysis", Dt(2026, 5, 12, 11)),
        E("exec-019-release-may20", WfRelease, " v2.5", Dt(2026, 5, 20, 10)),
        E("exec-020-weekly-may27", WfWeekly, "", Dt(2026, 5, 27, 16)),
        E("exec-021-triage-jun03", WfTriage, "", Dt(2026, 6, 3, 15)),
        E("exec-022-spec-jun06", WfSpec, " — Webhook Integration", Dt(2026, 6, 6, 12)),
        E("exec-023-weekly-jun10", WfWeekly, "", Dt(2026, 6, 10, 16)),
        E("exec-024-release-jun12", WfRelease, " v2.6", Dt(2026, 6, 12, 10)),
        E("exec-replay-001", WfWeekly, " (Replay)", Dt(2026, 6, 13, 9), replay: true),
        E("exec-replay-002", WfSpec, " (Replay)", Dt(2026, 6, 13, 10), replay: true),
        E("exec-replay-003", WfRelease, " (Replay)", Dt(2026, 6, 14, 9), replay: true, fail: 3),
        E("exec-replay-004", WfTriage, " (Replay)", Dt(2026, 6, 14, 10), replay: true),
        E("exec-import-001", WfSpec, " (Imported)", Dt(2026, 5, 15, 12), import: true),
        E("exec-import-002", WfWeekly, " (Imported)", Dt(2026, 4, 10, 16), import: true),
        E("exec-025-weekly-jun15", WfWeekly, "", Dt(2026, 6, 15, 8)),
        E("exec-026-triage-jun15", WfTriage, "", Dt(2026, 6, 15, 9, 30), fail: 2)
    ];

    static List<NotifSpec> NotificationDefinitions()
    {
        var list = new List<NotifSpec>();
        var id = 1;

        void Add(DateTime at, NotificationType type, string title, string msg, bool read = false) =>
            list.Add(new NotifSpec(Guid.Parse($"44444444-4444-4444-4444-{id++:D012}"), title, msg, type, read, at));

        Add(Dt(2025, 12, 15, 16, 5), NotificationType.ExecutionCompleted, "Weekly Engineering Report completed",
            "Execution exec-001-weekly-dec15 finished successfully with 4 tasks.", true);
        Add(Dt(2025, 12, 18, 9), NotificationType.Info, "Webhook connected: Slack #eng-alerts",
            "Slack webhook registered for execution events.", true);
        Add(Dt(2025, 12, 22, 16, 5), NotificationType.ExecutionCompleted, "Weekly Engineering Report completed",
            "Execution exec-002-weekly-dec22 finished successfully.", true);
        Add(Dt(2026, 1, 8, 15, 5), NotificationType.ExecutionCompleted, "Customer Support Triage completed",
            "Execution exec-003-triage-jan08 classified ticket as P2 — Billing.", true);
        Add(Dt(2026, 1, 10, 10), NotificationType.Info, "Regression gate created",
            "Support Triage Stability gate is now active.", true);
        Add(Dt(2026, 1, 15, 15, 5), NotificationType.ExecutionFailed, "Customer Support Triage failed",
            "Execution exec-004-triage-jan15 failed at Classify Priority: LLM request timed out.", true);
        Add(Dt(2026, 1, 15, 15, 6), NotificationType.Error, "Webhook delivery failed: PagerDuty",
            "Failed to deliver execution.failed event to PagerDuty On-Call (HTTP 503).", true);
        Add(Dt(2026, 1, 20, 16, 5), NotificationType.ExecutionCompleted, "Weekly Engineering Report completed",
            "Execution exec-005-weekly-jan20 finished successfully. Marked as golden baseline.", true);
        Add(Dt(2026, 1, 21, 9), NotificationType.Success, "Golden execution marked",
            "exec-005-weekly-jan20 marked as golden: Weekly Report Baseline v1.", true);
        Add(Dt(2026, 1, 22, 11), NotificationType.Info, "Regression gate created",
            "Weekly Report Quality Gate is now active.", true);
        Add(Dt(2026, 2, 1, 9), NotificationType.SystemUpdate, "Arc v1.2 deployed",
            "New regression gate rules and improved execution replay are now available.", true);
        Add(Dt(2026, 2, 3, 12, 5), NotificationType.ExecutionCompleted, "Feature Specification completed",
            "Execution exec-006-spec-feb03 produced spec for Dashboard Export feature.", true);
        Add(Dt(2026, 2, 5, 10), NotificationType.Info, "Regression gate created",
            "Feature Spec Semantic Gate is now active.", true);
        Add(Dt(2026, 2, 10, 12, 5), NotificationType.ExecutionCompleted, "Feature Specification completed",
            "Execution exec-007-spec-feb10 finished successfully.", true);
        Add(Dt(2026, 2, 17, 16, 5), NotificationType.ExecutionCompleted, "Weekly Engineering Report completed",
            "Execution exec-008-weekly-feb17 finished successfully.", true);
        Add(Dt(2026, 2, 24, 15, 5), NotificationType.ExecutionCompleted, "Customer Support Triage completed",
            "Execution exec-009-triage-feb24 routed ticket to Platform team.", true);
        Add(Dt(2026, 3, 3, 12, 5), NotificationType.ExecutionFailed, "Feature Specification failed",
            "Execution exec-010-spec-mar03 failed at Research Competitors: HTTP 404.", true);
        Add(Dt(2026, 3, 12, 10, 5), NotificationType.ExecutionCompleted, "Release Notes v2.4 published",
            "Execution exec-011-release-mar12 finished. 12 features, 8 fixes documented.", true);
        Add(Dt(2026, 3, 13, 9), NotificationType.Success, "Golden execution marked",
            "exec-011-release-mar12 marked as golden: Release Notes v2.4 Golden.", true);
        Add(Dt(2026, 3, 14, 10), NotificationType.Info, "Regression gate created",
            "Release Notes Exact Match gate is now active.", true);
        Add(Dt(2026, 3, 19, 16, 5), NotificationType.ExecutionCompleted, "Weekly Engineering Report completed",
            "Execution exec-012-weekly-mar19 finished successfully.", true);
        Add(Dt(2026, 3, 19, 17), NotificationType.Warning, "Execution archived",
            "exec-012-weekly-mar19 archived per quarterly retention policy.", true);
        Add(Dt(2026, 3, 26, 10, 5), NotificationType.ExecutionCompleted, "Release Notes v2.4.1 published",
            "Execution exec-013-release-mar26 finished successfully.", true);
        Add(Dt(2026, 4, 1, 16, 5), NotificationType.ExecutionCompleted, "Marketing Campaign Brief Q2 completed",
            "Execution exec-014-marketing-apr01 produced campaign brief for Q2 launch.", true);
        Add(Dt(2026, 4, 10, 16, 5), NotificationType.ExecutionCompleted, "Weekly Engineering Report (Imported)",
            "Execution exec-import-002 imported from staging environment.", true);
        Add(Dt(2026, 4, 20, 11, 5), NotificationType.ExecutionCompleted, "Feature Specification completed",
            "Execution exec-015-design-apr20 produced Dashboard Export spec.", true);
        Add(Dt(2026, 4, 28, 16, 5), NotificationType.ExecutionCompleted, "Weekly Engineering Report completed",
            "Execution exec-016-weekly-apr28 finished successfully.", true);
        Add(Dt(2026, 5, 5, 16, 5), NotificationType.ExecutionFailed, "Marketing Campaign Brief failed",
            "Execution exec-017-marketing-may05 failed at Analyze Target Audience.", false);
        Add(Dt(2026, 5, 12, 11, 5), NotificationType.ExecutionCompleted, "Competitive Analysis completed",
            "Execution exec-018-competitive-may12 finished with 4 competitor profiles.", true);
        Add(Dt(2026, 5, 15, 12, 5), NotificationType.ExecutionCompleted, "Feature Specification (Imported)",
            "Execution exec-import-001 imported from staging environment.", true);
        Add(Dt(2026, 5, 20, 10, 5), NotificationType.ExecutionCompleted, "Release Notes v2.5 published",
            "Execution exec-019-release-may20 finished. 8 features, 5 fixes documented.", true);
        Add(Dt(2026, 5, 21, 9), NotificationType.Success, "Golden execution marked",
            "exec-019-release-may20 marked as golden: Release Notes v2.5 Golden.", true);
        Add(Dt(2026, 5, 27, 16, 5), NotificationType.ExecutionCompleted, "Weekly Engineering Report completed",
            "Execution exec-020-weekly-may27 finished successfully.", false);
        Add(Dt(2026, 6, 3, 15, 5), NotificationType.ExecutionCompleted, "Customer Support Triage completed",
            "Execution exec-021-triage-jun03 classified ticket as P3 — How-to.", false);
        Add(Dt(2026, 6, 6, 12, 5), NotificationType.ExecutionCompleted, "Feature Specification completed",
            "Execution exec-022-spec-jun06 produced Webhook Integration spec.", false);
        Add(Dt(2026, 6, 10, 16, 5), NotificationType.ExecutionCompleted, "Weekly Engineering Report completed",
            "Execution exec-023-weekly-jun10 finished successfully.", false);
        Add(Dt(2026, 6, 12, 10, 5), NotificationType.ExecutionCompleted, "Release Notes v2.6 published",
            "Execution exec-024-release-jun12 finished. Regression gates passed.", false);
        Add(Dt(2026, 6, 13, 9, 5), NotificationType.Success, "Replay completed: Weekly Engineering Report",
            "Execution exec-replay-001 replay finished successfully.", false);
        Add(Dt(2026, 6, 13, 10, 5), NotificationType.Success, "Replay completed: Feature Specification",
            "Execution exec-replay-002 replay finished successfully.", false);
        Add(Dt(2026, 6, 14, 9, 5), NotificationType.ExecutionFailed, "Replay failed: Release Notes",
            "Execution exec-replay-003 replay failed at Write Release Notes.", false);
        Add(Dt(2026, 6, 14, 9, 6), NotificationType.Error, "Regression test failed: Feature Spec Semantic Gate",
            "Candidate exec-replay-003 failed similarity check (82.3% vs 85% threshold).", false);
        Add(Dt(2026, 6, 14, 10, 5), NotificationType.Success, "Replay completed: Customer Support Triage",
            "Execution exec-replay-004 replay finished successfully.", false);
        Add(Dt(2026, 6, 14, 11), NotificationType.Error, "Webhook delivery failed: Slack",
            "Failed to deliver execution.completed to Slack #eng-alerts (connection timeout).", false);
        Add(Dt(2026, 6, 15, 8, 5), NotificationType.ExecutionCompleted, "Weekly Engineering Report completed",
            "Execution exec-025-weekly-jun15 finished successfully.", false);
        Add(Dt(2026, 6, 15, 9, 35), NotificationType.ExecutionFailed, "Customer Support Triage failed",
            "Execution exec-026-triage-jun15 failed at Classify Priority.", false);
        Add(Dt(2026, 6, 15, 9, 36), NotificationType.Error, "Webhook delivery failed: Audit Log",
            "Failed to deliver execution.failed to Internal Audit Log endpoint.", false);
        Add(Dt(2026, 6, 15, 10), NotificationType.SystemUpdate, "Demo workspace refreshed",
            "Demo data seeded for portfolio visitors. All features populated.", false);
        Add(Dt(2026, 6, 15, 10, 1), NotificationType.Info, "Template usage milestone",
            "Template 'customer email' has been used 15 times.", false);
        Add(Dt(2026, 6, 15, 10, 2), NotificationType.Warning, "Regression gate approaching threshold",
            "Weekly Report Quality Gate: last test scored 96.1% (threshold 95%).", false);
        Add(Dt(2026, 6, 15, 10, 3), NotificationType.Info, "LLM config active",
            "GPT-4o Production is the default for 3 workflows.", false);

        return list;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    static DateTime Dt(int y, int m, int d, int h = 0, int min = 0) =>
        new(y, m, d, h, min, 0, DateTimeKind.Utc);

    static DivergenceRule Rule(DivergenceRuleType type, double threshold) => new(type, threshold);

    static Workflow Wf(string id, string name, string desc, string llm, DateTime at, params WorkflowTask[] tasks) =>
        new(id, name, desc, tasks, "manual", UserId, at, llm);

    static WorkflowTask T(string id, string name, string agent, string[]? deps = null, string? prompt = null,
        string? url = null, string? channel = null)
    {
        var config = new Dictionary<string, string>();
        if (url is not null) config["url"] = url;
        if (channel is not null) config["channel"] = channel;
        return new WorkflowTask(id, name, agent, prompt, null, config, deps ?? []);
    }

    static ExecSpec E(string id, string wf, string suffix, DateTime at, int? fail = null,
        bool replay = false, bool import = false, bool archive = false) =>
        new(id, wf, suffix, at, fail, replay, import, archive);

    static LLMConfiguration MakeLlm(string id, string name, string url, string model, string? key,
        UserId user, DateTime at) =>
        (LLMConfiguration)Activator.CreateInstance(
            typeof(LLMConfiguration),
            BindingFlags.NonPublic | BindingFlags.Instance, null,
            [id, name, url, model, key, "chat/completions", "bearer", new Dictionary<string, string>(), user, at, true],
            null)!;

    static IReadOnlyList<WorkflowTask> TopologicalSort(IReadOnlyList<WorkflowTask> tasks)
    {
        var sorted = new List<WorkflowTask>();
        var visited = new HashSet<string>();
        var map = tasks.ToDictionary(t => t.Id);

        void Visit(string id)
        {
            if (visited.Contains(id)) return;
            visited.Add(id);
            foreach (var dep in map[id].Dependencies)
                Visit(dep);
            sorted.Add(map[id]);
        }

        foreach (var t in tasks)
            Visit(t.Id);
        return sorted;
    }

    static IReadOnlyList<TaskExecutionResult> BuildTaskResults(
        IReadOnlyList<WorkflowTask> ordered, int? failAtOrder, string execId)
    {
        var results = new List<TaskExecutionResult>();
        var failed = false;
        var order = 1;

        foreach (var task in ordered)
        {
            if (failed)
            {
                results.Add(new(task.Id, task.Name, order++, TaskExecutionStatus.Skipped, ""));
                continue;
            }

            if (failAtOrder == order)
            {
                results.Add(new(task.Id, task.Name, order++, TaskExecutionStatus.Failed,
                    "Error: LLM request timed out after 30s"));
                failed = true;
                continue;
            }

            results.Add(new(task.Id, task.Name, order++, TaskExecutionStatus.Succeeded,
                $"[{execId}] {task.Name}: completed successfully.\n" +
                $"Summary: Deterministic output for demo workspace — Meridian Software."));
        }

        return results;
    }

    sealed record ExecSpec(
        string Id, string WorkflowId, string NameSuffix, DateTime CreatedAt,
        int? FailAtOrder, bool IsReplay, bool IsImport, bool Archive);

    sealed record NotifSpec(Guid Id, string Title, string Message, NotificationType Type, bool IsRead, DateTime At);
}