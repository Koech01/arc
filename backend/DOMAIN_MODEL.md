# Domain Model

Arc's Domain layer (`Arc.Domain`) contains 25 pure C# models with zero external dependencies. All domain entities enforce business invariants through their constructors, ensuring the system never enters an invalid state.

## Core Principles

1. **Immutability** - Value objects and most entities are immutable once created; mutations return new instances via `With*` factory methods
2. **Invariant Enforcement** - Business rules validated in constructors; violations throw domain exceptions
3. **No Framework Dependencies** - Pure .NET types only
4. **Self-Validation** - Models throw domain exceptions on rule violations
5. **Strongly-Typed IDs** - `UserId`, `WebhookId`, `NotificationId`, `RegressionGateId`, `GoldenExecutionId`

## Domain Models by Category

### Execution Models

#### ExecutionGraph

Represents a directed acyclic graph (DAG) of tasks to execute.

**Properties:**
- `Nodes` - read-only collection of `TaskNode` instances

**Business Rules:**
- Must contain at least one node
- All node dependencies must reference existing nodes
- Cannot contain circular dependencies (cycle detection on construction)
- Graph structure is immutable once created

#### TaskNode

Individual task within an execution graph.

**Properties:**
- `Id` - unique task identifier (string)
- `Name` - human-readable task description
- `DependsOn` - read-only collection of task IDs this task depends on (deduplicated)
- `Prompt` (optional) - custom LLM prompt (max 5000 characters)
- `LLMConfigId` (optional) - LLM configuration override

**Business Rules:**
- `Id` and `Name` cannot be null or whitespace
- Task cannot depend on itself
- Dependencies are automatically deduplicated
- Prompt limited to 5000 characters

### Identity and User Models

#### User

User account with authentication and authorization.

**Properties:**
- `Id` - strongly-typed `UserId` (GUID-based)
- `Username` - unique username (3–50 characters)
- `Email` - unique email address (normalized to lowercase)
- `PasswordHash` - BCrypt hashed password
- `Role` - `UserRole` enum (`User` or `Admin`)
- `Firstname` (optional) - display name (max 100 characters)
- `IsActive` - account active status
- `FailedLoginAttempts` - login failure counter
- `LockedUntilUtc` (optional) - time-based lockout expiry
- `CreatedAt` - account creation timestamp
- `DeletedAt` (optional) - soft delete timestamp
- `IsLockedOut` - computed: `LockedUntilUtc.HasValue && DateTime.UtcNow < LockedUntilUtc`
- `IsDeleted` - computed: `DeletedAt.HasValue`

**Business Rules:**
- Username must be 3–50 characters
- Email must be valid format
- Password stored as BCrypt hash only
- Account locked for a configurable duration after reaching the failed login threshold
- Soft delete preserves audit trail; deleted accounts cannot authenticate
- All mutations return new `User` instances (immutable pattern)

**Mutation Methods (return new instances):**
- `Activate()` / `Deactivate()` - toggle `IsActive`
- `UpdateRole(UserRole)` - change role
- `WithFailedLoginAttempt(maxAttempts, lockoutDuration)` - increment failure counter; applies lockout when threshold is reached
- `WithResetFailedAttempts()` - clear counter and lockout
- `SoftDelete()` - set `DeletedAt` to now, set `IsActive` to false
- `WithNewPasswordHash(string)` - replace password hash
- `UpdateEmail(string)` / `UpdateUsername(string)` / `UpdateProfile(string, string, string?)` - update profile fields

**Factory:**
- `User.Create(username, email, passwordHash, role)` - creates with new GUID and current UTC timestamp

#### UserId

Strongly-typed user identifier.

**Properties:**
- `Value` - underlying GUID

**Business Rules:**
- Cannot be empty GUID
- Value equality semantics
- Immutable

#### UserRole

```csharp
public enum UserRole { User, Admin }
```

#### PasswordResetToken

Time-limited password reset token.

**Properties:**
- `Token` - unique token string
- `UserId` - owning user
- `ExpiresAt` - expiration timestamp
- `IsExpired` - computed: `ExpiresAt < DateTime.UtcNow`

**Business Rules:**
- 1-hour lifetime
- One-time use only

#### UserPreferences

User-specific application preferences.

**Properties:**
- `UserId` - strongly-typed user identifier
- `Theme` - UI theme (`"light"`, `"dark"`, or `"auto"`)
- `NotificationsEnabled` - push notification preference
- `UpdatedAt` - last update timestamp

**Business Rules:**
- Theme must be `"light"`, `"dark"`, or `"auto"`
- One preference set per user

### Workflow Models

#### Workflow

Reusable execution template.

**Properties:**
- `Id` - string identifier
- `Name` - workflow name (max 200 characters)
- `Description` - workflow description (max 1000 characters)
- `Tasks` - read-only list of `WorkflowTask` instances
- `TriggerType` - trigger type (`"manual"`, `"scheduled"`, or `"webhook"`)
- `LLMConfigId` (optional) - workflow-level LLM configuration override
- `CreatedBy` - `UserId` of the workflow owner
- `CreatedAt` - creation timestamp

**Business Rules:**
- `Id` and `Name` cannot be null or whitespace
- Name cannot exceed 200 characters
- Description cannot exceed 1000 characters
- Must contain at least one task
- `TriggerType` must be `"manual"`, `"scheduled"`, or `"webhook"`
- Task IDs must be unique within the workflow
- All task dependencies must reference existing tasks
- Cannot contain circular task dependencies (cycle detection on construction)

#### WorkflowTask

Task definition within a workflow template.

**Properties:**
- `Id` - unique task identifier within the workflow
- `Name` - human-readable task name
- `Dependencies` - list of task IDs this task depends on
- `Prompt` (optional) - custom LLM prompt
- `LLMConfigId` (optional) - task-level LLM configuration override

**Business Rules:**
- Same rules as `TaskNode`
- Dependencies must reference existing workflow tasks

### Webhook Models

#### Webhook

User-defined webhook subscription.

**Properties:**
- `Id` - strongly-typed `WebhookId`
- `Url` - target URL (HTTP or HTTPS)
- `Events` - read-only list of `WebhookEventType` subscriptions
- `Secret` - HMAC signing secret (minimum 20 characters)
- `IsActive` - webhook active status
- `CreatedBy` - `UserId` of the webhook owner
- `CreatedAt` - creation timestamp

**Business Rules:**
- URL must be a valid absolute HTTP or HTTPS URI
- Events list cannot be empty
- Secret must be at least 20 characters
- Webhooks are user-scoped

#### WebhookId

Strongly-typed webhook identifier.

**Properties:**
- `Value` - underlying GUID

#### WebhookEventType

```csharp
public enum WebhookEventType
{
    ExecutionStarted,
    ExecutionCompleted,
    ExecutionFailed,
    TaskCompleted,
    RegressionTestCompleted
}
```

All webhook payloads are signed with HMAC-SHA256. The signature is delivered in the `X-Arc-Signature` header (base64-encoded). Recipients verify the signature using the webhook secret.

### Regression Testing Models

#### RegressionGate

Regression testing configuration with divergence rules.

**Properties:**
- `Id` - strongly-typed `RegressionGateId`
- `OwnerId` - `UserId` of the gate owner
- `Name` - gate name (max 200 characters)
- `Description` (optional) - gate description (max 1000 characters)
- `WorkflowId` (optional) - associated workflow ID
- `GoldenExecutionId` - single golden baseline execution ID
- `Rules` - read-only list of `DivergenceRule` instances
- `IsActive` - gate active status
- `CreatedAtUtc` - creation timestamp

**Business Rules:**
- Name cannot be null or whitespace and cannot exceed 200 characters
- Description cannot exceed 1000 characters
- Must have at least one divergence rule
- References a single golden execution
- User-scoped (multi-tenant)

**Mutation Methods (return new instances):**
- `WithIsActive(bool)` - toggle active status

#### RegressionGateId

Strongly-typed regression gate identifier.

**Properties:**
- `Value` - underlying GUID

#### DivergenceRule

Rule defining acceptable output divergence.

**Properties:**
- `Type` - `DivergenceRuleType` enum
- `Parameters` (optional) - rule-specific parameters string

**Rule Types:**

| Type | Parameters | Behavior |
|------|-----------|----------|
| `ExactMatch` | none | Outputs must be byte-for-byte identical |
| `ContainsKeywords` | comma-separated keywords | Outputs must contain all specified keywords |
| `SemanticSimilarity` | threshold (0.0–1.0) | Outputs must meet similarity threshold |
| `CustomRegex` | regex pattern | Outputs must match the regex pattern |

#### DivergenceRuleType

```csharp
public enum DivergenceRuleType
{
    ExactMatch,
    ContainsKeywords,
    SemanticSimilarity,
    CustomRegex
}
```

#### RegressionTestResult

Result of a regression test execution.

**Properties:**
- `Passed` - overall test result
- `ExecutionId` - tested execution ID
- `GoldenExecutionId` - baseline execution ID
- `RuleResults` - list of `RuleEvaluationResult` per rule
- `TestedAt` - test execution timestamp

#### RuleEvaluationResult

Individual rule evaluation result.

**Properties:**
- `RuleName` - rule type name
- `Passed` - rule passed
- `Reason` (optional) - failure reason

#### DivergenceSummary

Aggregated divergence statistics across rule evaluations.

#### GoldenExecutionMetadata

Metadata for golden baseline executions.

**Properties:**
- `ExecutionId` - execution marked as golden
- `MarkedBy` - user who marked it golden
- `MarkedAt` - timestamp when marked
- `Description` (optional) - notes about this baseline

#### GoldenExecutionId

Strongly-typed golden execution identifier.

**Properties:**
- `Value` - underlying execution ID string

### Notification Models

#### Notification

User notification.

**Properties:**
- `Id` - strongly-typed `NotificationId`
- `UserId` - notification recipient
- `Title` - notification title (max 255 characters)
- `Message` - notification body (max 2000 characters)
- `Type` - `NotificationType` enum
- `IsRead` - read status
- `CreatedAt` - creation timestamp

**Business Rules:**
- Title cannot be null or whitespace; max 255 characters
- Message cannot be null or whitespace; max 2000 characters
- Notifications are user-scoped
- Can be deleted by the owning user
- `RelatedEntityId` is tracked at the infrastructure layer only; it is not a domain property

**Mutation Methods (return new instances):**
- `MarkAsRead()` - sets `IsRead` to true; no-op if already read

**Factory:**
- `Notification.Create(userId, title, message, type)` - creates with new ID, `IsRead = false`, current UTC timestamp

#### NotificationId

Strongly-typed notification identifier.

**Properties:**
- `Value` - underlying GUID

#### NotificationType

```csharp
public enum NotificationType
{
    ExecutionCompleted,
    ExecutionFailed,
    WebhookDeliveryFailed,
    SystemUpdate
}
```

### LLM Models

#### LLMConfiguration

User-specific LLM endpoint configuration. Compatible with any OpenAI-compatible API.

**Properties:**
- `Id` - SHA256-derived string identifier (scoped to user and name)
- `Name` - configuration name
- `BaseUrl` - API base URL
- `Model` - model name
- `ApiKey` (optional) - API authentication key
- `Endpoint` - API endpoint path (default: `"chat/completions"`)
- `AuthType` - authentication type (default: `"bearer"`)
- `Headers` - additional HTTP headers (`Dictionary<string, string>`)
- `CreatedBy` - `UserId` of the configuration owner
- `CreatedAt` - creation timestamp
- `IsActive` - configuration active status

**Business Rules:**
- `Name`, `BaseUrl`, and `Model` cannot be null or whitespace
- `Id` is deterministically derived from `UserId`, `Name`, and creation timestamp
- API keys are never logged or exposed in API responses
- User can have multiple configurations

**Mutation Methods (return new instances):**
- `Deactivate()` - sets `IsActive` to false
- `WithUpdates(name?, baseUrl?, model?, apiKey?, endpoint?, authType?, headers?)` - partial update; null fields preserve existing values; empty `apiKey` preserves existing key

**Factory:**
- `LLMConfiguration.Create(name, baseUrl, model, apiKey, endpoint, authType, headers, createdBy)` - creates with generated ID and current UTC timestamp

## Domain Exceptions

| Exception | HTTP Mapping | Usage |
|-----------|-------------|-------|
| `DomainException` | 400 | Base exception for all business rule violations |
| `AuthenticationException` | 401 | Authentication and authorization errors |
| `WorkflowException` | 400 | Workflow validation errors |
| `WebhookException` | 400 | Webhook-related errors |
| `ExecutionGraphInvalidException` | 400 | Execution graph structure violations |
| `TaskNodeInvalidException` | 400 | Task node invariant violations |
| `RegressionGateInvalidException` | 400 | Regression gate invariant violations |

HTTP mapping is applied by `GlobalExceptionMiddleware` in the API layer.

## Design Patterns

### Value Objects

Strongly-typed IDs use the value object pattern:
- `UserId`, `WebhookId`, `NotificationId`, `RegressionGateId`, `GoldenExecutionId`
- Value equality semantics
- Immutable
- Cannot be empty or invalid

### Immutability

Entities return new instances for all mutations:
- `User` - `Activate()`, `Deactivate()`, `UpdateRole()`, `WithFailedLoginAttempt()`, `WithResetFailedAttempts()`, `SoftDelete()`, `WithNewPasswordHash()`, `UpdateEmail()`, `UpdateUsername()`, `UpdateProfile()`
- `RegressionGate` - `WithIsActive()`
- `LLMConfiguration` - `Deactivate()`, `WithUpdates()`
- `Notification` - `MarkAsRead()`

Structural models (`TaskNode`, `WorkflowTask`, `DivergenceRule`) are immutable by construction.

### Soft Delete

`User` implements soft delete:
- `DeletedAt` timestamp instead of physical deletion
- `IsDeleted` computed property
- Preserves audit trail and referential integrity

### Computed Properties

Read-only derived properties:
- `User.IsLockedOut` - derived from `LockedUntilUtc` and current UTC time
- `User.IsDeleted` - derived from `DeletedAt`
- `PasswordResetToken.IsExpired` - derived from `ExpiresAt`

### Cycle Detection

`ExecutionGraph` and `Workflow` validate for cycles using DFS with a recursion stack on construction. A `WorkflowException` or `ExecutionGraphInvalidException` is thrown if a cycle is detected.

## Summary

| Category | Models |
|----------|--------|
| Execution | `ExecutionGraph`, `TaskNode` |
| Identity | `User`, `UserId`, `UserRole`, `PasswordResetToken`, `UserPreferences` |
| Workflows | `Workflow`, `WorkflowTask` |
| Webhooks | `Webhook`, `WebhookId`, `WebhookEventType` |
| Regression | `RegressionGate`, `RegressionGateId`, `DivergenceRule`, `DivergenceRuleType`, `DivergenceSummary`, `RegressionTestResult`, `RuleEvaluationResult`, `GoldenExecutionMetadata`, `GoldenExecutionId` |
| Notifications | `Notification`, `NotificationId`, `NotificationType` |
| LLM | `LLMConfiguration` |

**Domain Exceptions:** `DomainException`, `AuthenticationException`, `WorkflowException`, `WebhookException`, `ExecutionGraphInvalidException`, `TaskNodeInvalidException`, `RegressionGateInvalidException`
