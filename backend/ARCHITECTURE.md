# Architecture Guide

## Overview

Arc is built with Clean Architecture principles featuring strict layer separation, dependency injection, and deterministic execution. Dependencies flow inward: outer layers depend on inner layers, never the reverse.

## Layer Diagram

```
┌─────────────────────────────────────────────────────┐
│  Infrastructure (External Systems)                  │
│  - PostgreSQL repositories                          │
│  - LLM provider implementations                     │
│  - Webhook delivery, caching                        │
│  ┌───────────────────────────────────────────────┐  │
│  │  API (HTTP/Presentation)                      │  │
│  │  - 22 controllers, DTOs, validators           │  │
│  │  - Middleware, filters, extensions            │  │
│  │  ┌─────────────────────────────────────────┐  │  │
│  │  │  Application (Use Cases)                │  │  │
│  │  │  - Service interfaces & implementations │  │  │
│  │  │  - Orchestration, execution, identity   │  │  │
│  │  │  ┌───────────────────────────────────┐  │  │  │
│  │  │  │  Domain (Business Entities)       │  │  │  │
│  │  │  │  - Pure C# models & exceptions    │  │  │  │
│  │  │  │  - No external dependencies       │  │  │  │
│  │  │  └───────────────────────────────────┘  │  │  │
│  │  └─────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘

Dependency Direction: Infrastructure → API → Application → Domain
```

**Dependency Rule:**
- Domain knows nothing about outer layers
- Application references Domain only
- API references Application and Domain
- Infrastructure implements Application interfaces

## Project Structure

### 1. Domain Layer (`Arc.Domain`)

**Purpose:** Pure business logic with zero framework dependencies.

**Contents:**

- **Models/** - 25 domain entities:
  - **Execution:** `ExecutionGraph`, `TaskNode`
  - **Identity:** `User`, `UserId`, `UserRole`, `PasswordResetToken`, `UserPreferences`
  - **Workflows:** `Workflow`, `WorkflowTask`
  - **Regression Testing:** `RegressionGate`, `RegressionGateId`, `DivergenceRule`, `DivergenceRuleType`, `DivergenceSummary`, `RegressionTestResult`, `RuleEvaluationResult`, `GoldenExecutionMetadata`, `GoldenExecutionId`
  - **Webhooks:** `Webhook`, `WebhookId`, `WebhookEventType`
  - **Notifications:** `Notification`, `NotificationId`, `NotificationType`
  - **LLM:** `LLMConfiguration`

- **Exceptions/** - Business rule violations:
  - `DomainException`, `AuthenticationException`, `WorkflowException`, `WebhookException`, `ExecutionGraphInvalidException`, `TaskNodeInvalidException`, `RegressionGateInvalidException`

**Characteristics:**
- Immutable value types where possible; mutable state returned via `With*` factory methods
- Business invariants enforced in constructors
- No dependencies on infrastructure or frameworks
- Pure C# objects

### 2. Application Layer (`Arc.Application`)

**Purpose:** Defines system use cases and service contracts.

**Key Interfaces:**

#### Execution (`Application/Execution/`)
- `IPlanner` - converts input to execution graph
- `IExecutionEngine` - orchestrates task execution
- `IAgentExecutor` - executes individual tasks with LLM
- `IExecutionResultStore` - persistence interface
- `IAuditLogger` - audit trail logging
- `ITaskExecutionCache` - caching for determinism
- `IExecutionReplayer` - replay past executions
- `IBatchExecutor` - parallel batch processing
- `IExecutionComparer` - compare two executions
- `IExecutionTransformer` - transform execution data
- `IExecutionProfiler` - performance profiling
- `IExecutionVisualizer` - visualization data generation
- `IExecutionExporter` / `IExecutionImporter` - export/import with timestamp preservation
- `IExecutionTemplateStore` - template persistence
- `ILLMProviderService` - dynamic LLM provider resolution

#### Identity (`Application/Identity/`)
- `IAuthenticationService` - registration, login, password reset
- `IUserRepository` - user persistence
- `IUserContext` - current user resolution
- `IJwtTokenService` - token generation and validation
- `IPasswordHashingService` - BCrypt hashing
- `IEmailService` - password reset emails
- `IUserPreferencesRepository` - user preferences
- `IPasswordResetRepository` - password reset tokens

#### Admin (`Application/Admin/`)
- `IAdminStatsService` - dashboard statistics (cached 30 seconds), system-wide execution and LLM config queries
- `IAdminUserService` - user lifecycle management (activate, deactivate, role change, password reset, soft delete, paginated query)
- `IAdminAuditLogger` - admin action logging
- `ILoginHistoryRepository` - login tracking
- `IMaintenanceModeService` - maintenance mode toggle (in-memory singleton)
- `ISystemConfigurationService` - sanitized configuration snapshot

#### Workflows (`Application/Workflows/`)
- `IWorkflowRepository` - workflow persistence
- `IWorkflowExecutor` - workflow execution

#### Regression Testing (`Application/RegressionGates/`)
- `IRegressionGateService` - regression testing orchestration
- `IRegressionGateRepository` - regression gate persistence
- `IGoldenExecutionStore` - golden baseline storage

#### Webhooks (`Application/Webhooks/`)
- `IWebhookRepository` - webhook persistence
- `IWebhookDispatcher` - HMAC-signed webhook delivery (10-second timeout)

#### Notifications (`Application/Notifications/`)
- `INotificationService` - notification creation
- `INotificationRepository` - notification persistence

#### LLM (`Application/LLM/`)
- `ILLMProvider` - LLM provider abstraction
- `ILLMConfigurationRepository` - user LLM config storage

**Service Registration:**

All services are registered in `Arc.Api/Extensions/ServiceCollectionExtensions.cs`:

```csharp
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    services.AddScoped<IAuthenticationService, DeterministicAuthenticationService>();
    services.AddScoped<IPlanner, DeterministicPlannerV1>();
    services.AddScoped<ILLMProviderService, DynamicLLMProviderService>();
    services.AddScoped<IAgentExecutor, DeterministicAgentExecutorV1>();
    services.AddScoped<IExecutionEngine, DeterministicExecutionEngineV1>(...);
    services.AddScoped<IOrchestrator, DeterministicOrchestratorV1>();
    services.AddScoped<IExecutionReplayer, DeterministicExecutionReplayer>();
    services.AddScoped<IBatchExecutor, DeterministicBatchExecutorV1>();
    services.AddScoped<IExecutionComparer, DeterministicExecutionComparer>();
    services.AddScoped<IExecutionTransformer, DeterministicExecutionTransformer>();
    services.AddScoped<IExecutionProfiler, DeterministicExecutionProfiler>();
    services.AddScoped<IExecutionVisualizer, DeterministicExecutionVisualizer>();
    services.AddScoped<IExecutionExporter, DeterministicExecutionExporter>();
    services.AddScoped<IExecutionImporter, DeterministicExecutionImporter>();
    services.AddScoped<IWorkflowExecutor, DeterministicWorkflowExecutor>();
    services.AddScoped<IAdminStatsService, PostgresAdminStatsService>();
    services.AddScoped<IAdminUserService, AdminUserService>();
    services.AddSingleton<IMaintenanceModeService, InMemoryMaintenanceModeService>();
    services.AddScoped<ISystemConfigurationService, SystemConfigurationService>();
    services.AddScoped<AdminActionLoggingFilter>();
    services.AddScoped<IRegressionGateService, DeterministicRegressionGateService>();
    return services;
}
```

### 3. Infrastructure Layer (`Arc.Infrastructure`)

**Purpose:** Implements Application interfaces with external systems.

**Database Strategy:**

- **PostgreSQL:** Required for development and production. Full feature support across all repositories.

Configuration:

```csharp
services.AddScoped<IUserRepository, PostgresUserRepository>();
services.AddScoped<IExecutionResultStore, PostgresExecutionResultStore>();
services.AddScoped<IWorkflowRepository, PostgresWorkflowRepository>();
services.AddScoped<IWebhookRepository, PostgresWebhookRepository>();
services.AddScoped<INotificationRepository, PostgresNotificationRepository>();
services.AddScoped<ILLMConfigurationRepository, PostgresLLMConfigurationRepository>();
services.AddScoped<IRegressionGateRepository, PostgresRegressionGateRepository>();
services.AddScoped<IGoldenExecutionStore, PostgresGoldenExecutionStore>();
services.AddScoped<IAdminAuditLogger, PostgresAdminAuditLogger>();
services.AddScoped<ILoginHistoryRepository, PostgresLoginHistoryRepository>();
// ... remaining Postgres repositories
```

**Key Implementations:**

#### Persistence (`Infrastructure/Persistence/`)
- `PostgresDatabaseContext` - connection management, schema initialization
- Parameterized SQL throughout (no ORM)

#### Identity (`Infrastructure/Identity/`)
- `BCryptPasswordHashingService` - work factor 11
- `JwtTokenService` - JWT generation with HTTP-only cookies
- `HttpUserContext` - resolves current user from `HttpContext.User.Claims`
- `DatabaseSeeder` - creates admin account on startup

#### LLM (`Infrastructure/LLM/`)
- `GenericLlmProvider` - async HTTP client supporting OpenAI-compatible APIs (OpenAI, Azure, Anthropic, Gemini, Ollama)
- `FakeLlmProvider` - deterministic testing provider
- `LLMProviderFactory` - dynamic provider creation
- `LLMFailureClassifier` - classifies transient vs non-transient failures
- `DynamicLLMProviderService` - resolves provider per task/workflow/system priority

#### Admin (`Infrastructure/Admin/`)
- `PostgresAdminStatsService` - 30-second `IMemoryCache` for dashboard stats
- `AdminUserService` - user lifecycle (activate, deactivate, role change, password reset, soft delete)
- `InMemoryMaintenanceModeService` - volatile singleton (resets on app restart)
- `SystemConfigurationService` - sanitized config snapshot (no secrets, no connection strings)
- `PostgresAdminAuditLogger` - admin action audit trail
- `PostgresLoginHistoryRepository` - login tracking

### 4. API Layer (`Arc.Api`)

**Purpose:** HTTP presentation layer with 22 controllers.

#### Controllers (`Api/Controllers/`)

1. `AuthController` - registration, login, refresh, profile, password reset
2. `ExecutionController` - single execution endpoint (`POST /api/execute`)
3. `ExecutionsController` - list, get, tasks, logs, outputs, metadata, archive, unarchive, purge, archive-audit, replay, export, import, compare, golden
4. `BatchController` - batch processing
5. `WorkflowsController` - CRUD and execute
6. `ExecutionTemplatesController` - template management
7. `ExecutionProfileController` - performance profiling
8. `ExecutionVisualizationController` - visualization data
9. `ExecutionTransformationController` - execution transformation
10. `ExecutionExportImportController` - export/import with timestamp preservation
11. `ReplayController` - execution replay (audit-trace reconstruction)
12. `LLMConfigsController` - user LLM configurations
13. `RegressionGatesController` - regression testing (9 endpoints including toggle)
14. `WebhooksController` - webhook management (7 endpoints including PATCH update)
15. `NotificationsController` - user notifications
16. `SettingsController` - user preferences
17. `AdminController` - admin operations (20 endpoints)
18. `DatabaseController` - database health check
19. `HealthController` - application health
20. `AuditController` - execution audit trail
21. `TaskCacheController` - cache management
22. `CompareController` - execution comparison

#### DTOs (`Api/DTOs/`)

Organized by feature: `Auth/`, `Execution/`, `Admin/`, `LLM/`, `Notifications/`, `RegressionGates/`.

#### Validators (`Api/Validators/`)

FluentValidation rules for all request DTOs. Registered via:

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();
```

Validation errors return:

```json
{ "message": "Validation failed", "errors": [{ "field": "...", "errors": ["..."] }] }
```

#### Middleware (`Api/Middleware/`)

Middleware pipeline order in `Program.cs`:

```
1. UseSerilogRequestLogging     - structured request logging
2. UseGlobalExceptionMiddleware - centralized exception handling
3. UseHttpsRedirection          - HTTPS enforcement
4. UseCors                      - CORS policy
5. UseRateLimiter               - per-IP fixed-window rate limiting
6. UseMaintenanceModeMiddleware - HTTP 503 for non-admin requests when enabled
7. UseAuthentication            - JWT cookie authentication
8. UseAuthorization             - role-based authorization
9. MapControllers               - route to controllers
```

**GlobalExceptionMiddleware** maps domain exceptions to HTTP status codes:
- `AuthenticationException` → 401
- `DomainException` → 400
- Unhandled exceptions → 500 (logged, generic message returned)

**MaintenanceModeMiddleware** returns HTTP 503 for all non-admin, non-health-check requests when maintenance mode is active.

#### Filters (`Api/Filters/`)

`AdminActionLoggingFilter` - logs all admin actions via `IAdminAuditLogger`. Applied with `[ServiceFilter(typeof(AdminActionLoggingFilter))]`.

#### Authorization (`Api/Authorization/`)

Custom `[RequireRole(UserRole.Admin)]` attribute for role enforcement at controller level.

## Authentication and Authorization

### JWT Authentication

Tokens are read from the `auth_token` HTTP-only cookie. Configuration:

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    ValidateIssuer = true,
    ValidIssuer = "Arc",
    ValidateAudience = true,
    ValidAudience = "Arc",
    ValidateLifetime = true,
    ClockSkew = TimeSpan.FromMinutes(2)
};
```

Token expiry defaults to 480 minutes (8 hours), configurable via `Jwt:ExpirationMinutes`.

### Role-Based Authorization

```csharp
public enum UserRole { User, Admin }
```

Admin endpoints use `[Authorize(Roles = "Admin")]`. Standard user endpoints use `[Authorize]`.

### Account Security

- BCrypt password hashing, work factor 11
- Time-based account lockout after configurable failed login attempts (default: 5 attempts, 15-minute lockout window)
- Soft delete preserves audit trail
- Login history tracked per user (PostgreSQL only)

## Security Features

### Rate Limiting

Per-IP fixed-window rate limiter. Default: 500 requests per 60 seconds (configurable via `RateLimiting:PermitLimit` and `RateLimiting:WindowSeconds`). Returns HTTP 429 when exceeded.

### CORS

Configured origins (development defaults):
- `http://localhost:5173`
- `http://localhost:5266`
- `http://192.168.100.10:5173`

Credentials are allowed (required for cookie-based auth).

### Webhook HMAC Signing

All webhook payloads are signed with HMAC-SHA256. Signature delivered in `X-Arc-Signature` header (base64-encoded). Webhook secrets must be at least 20 characters.

### Admin Audit Logging

All admin actions are logged to PostgreSQL with: admin user ID, action name, target entity, timestamp, and result.

## Logging and Observability

### Serilog

Structured logging via Serilog, configured in `Program.cs`:

```csharp
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});
```

### Audit Trail

Execution audit trail with monotonic sequence numbers, stored per execution ID. Accessible via `GET /api/audit/{executionId}` and `GET /api/executions/{id}/logs`.

## Database Architecture

### Schema Initialization

Automatic on startup via `InitializeDatabaseAsync()`. Table creation order respects foreign key dependencies:

`users` → `password_reset_tokens` → `llm_configurations` → `execution_results` → `audit_logs` → `task_execution_cache` → `execution_templates` → `workflows` → remaining tables.

All `CREATE TABLE` statements use `IF NOT EXISTS` (idempotent).

### Multi-Tenancy

All user data is scoped by `UserId` at the repository level:
- Workflows, executions, LLM configs, webhooks, notifications, regression gates

Admin endpoints bypass user scoping via `[Authorize(Roles = "Admin")]`.

### Database Seeding

Admin account created on first run if `AdminAccount:Enabled` is `true` and the email does not already exist.

## Deterministic Execution

### Core Principle

Same input + same user → same execution ID + same results.

- Execution ID = SHA256 hash of inputs
- Topological sort with deterministic tie-breaking
- Task results cached by deterministic hash
- Audit logs with monotonic sequence numbers

### Deterministic Service Implementations

All core services carry the `Deterministic` prefix:

- `DeterministicPlannerV1`
- `DeterministicAgentExecutorV1`
- `DeterministicExecutionEngineV1`
- `DeterministicOrchestratorV1`
- `DeterministicWebhookDispatcher`
- `DeterministicNotificationService`
- `DeterministicWorkflowExecutor`
- `DeterministicRegressionGateService`
- `DeterministicBatchExecutorV1`
- `DeterministicExecutionComparer`
- `DeterministicExecutionProfiler`
- `DeterministicExecutionVisualizer`
- `DeterministicExecutionExporter`
- `DeterministicExecutionImporter`
- `DeterministicExecutionReplayer`
- `DeterministicExecutionTransformer`

### Task Execution Caching

```csharp
public interface ITaskExecutionCache
{
    Task<TaskResult?> GetAsync(string executionId, string taskId);
    Task SetAsync(string executionId, string taskId, TaskResult result);
}
```

Cache miss → execute task → cache result → return cached result on subsequent identical requests.

### LLM Provider Resolution Priority

1. Task-level `LLMConfigId` (highest priority)
2. Workflow-level `LLMConfigId`
3. System default from `appsettings.json` (`LLM:*` section)
4. `FakeLlmProvider` (deterministic fallback when no provider is configured)

## Testing Architecture

### Unit Tests (`Arc.UnitTests`)

Covers domain model business rules, application service logic, infrastructure implementations, and determinism validation. Uses xUnit, FluentAssertions, and NSubstitute.

### Integration Tests (`Arc.IntegrationTests`)

End-to-end API tests using `WebApplicationFactory<Program>`. Tests authenticate via HTTP-only cookie flows. Test parallelization is disabled for deterministic execution. Rate limiting is overridden via environment variables in test runs.

## Performance

### Caching Strategy

- **Task Execution Cache:** Deterministic task results cached indefinitely in PostgreSQL
- **Admin Stats Cache:** 30-second `IMemoryCache` for dashboard statistics
- **Rate Limiter:** In-memory per-IP fixed-window

### Database Connection Pooling

- PostgreSQL: Npgsql default connection pooling

### Async Pattern

All I/O operations are asynchronous. Async methods use the `Async` suffix.

## Configuration Reference

| Key | Default | Description |
|-----|---------|-------------|
| `ConnectionStrings:PostgreSQL` | - | PostgreSQL connection string |
| `Jwt:SecretKey` | dev key | JWT signing key (32+ characters required) |
| `Jwt:ExpirationMinutes` | `480` | JWT token lifetime in minutes |
| `Jwt:Issuer` | `Arc` | JWT issuer claim |
| `Jwt:Audience` | `Arc` | JWT audience claim |
| `LLM:DefaultBaseUrl` | - | Default LLM API base URL |
| `LLM:DefaultModel` | - | Default LLM model name |
| `LLM:DefaultApiKey` | - | Default LLM API key |
| `LLM:DefaultEndpoint` | `chat/completions` | Default LLM endpoint path |
| `LLM:DefaultAuthType` | `bearer` | Default auth type |
| `AdminAccount:Enabled` | `true` | Enable admin account seeding |
| `AdminAccount:Email` | `admin@arc.com` | Admin account email |
| `AdminAccount:Password` | - | Admin account password |
| `RateLimiting:PermitLimit` | `500` | Requests per window per IP |
| `RateLimiting:WindowSeconds` | `60` | Rate limit window in seconds |

## Deployment

### Development

```
Browser (localhost:5173) → Arc.Api (localhost:5266) → PostgreSQL
                                                     → LLM APIs
```

### Production

```
Users → HTTPS Reverse Proxy (nginx/Caddy, port 443)
      → Arc.Api (port 5266)
      → PostgreSQL (with SSL)
      → LLM APIs
      → Webhook targets
```

### Docker

```bash
docker-compose up -d
```

Includes Arc API container, PostgreSQL container, shared network, and volume persistence.

### Production Checklist

- Change admin password
- Set strong JWT secret key (32+ characters)
- Configure PostgreSQL with SSL
- Set up HTTPS reverse proxy
- Configure CORS for production domain
- Set up log aggregation
- Enable database backups
- Monitor failed authentication attempts via admin audit log