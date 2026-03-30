namespace Arc.Application.Execution;

/// <summary>
/// Immutable workflow context attached to an execution at storage time.
/// Carries the display metadata required by the UI (workflowName, workflowDescription).
/// Populated by both the native workflow executor and the import pipeline before calling IExecutionResultStore.
/// WorkflowId is null for ad-hoc and imported executions with no linked workflow.
/// </summary>
public sealed record ExecutionWorkflowContext(
    string? WorkflowId,
    string WorkflowName,
    string WorkflowDescription
);