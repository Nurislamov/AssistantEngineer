using AssistantEngineer.Modules.Identity.Application.Contracts;

namespace AssistantEngineer.Modules.Identity.Application.Contracts.Audit;

public sealed record AuditEventWriteRequest(
    string EventType,
    string Category,
    AuditEventOutcome Outcome,
    PrincipalAccessContext? Principal,
    string? CorrelationId,
    string? RequestId,
    string? ResourceType,
    string? ResourceId,
    string? ProjectId,
    string? BuildingId,
    string? WorkflowId,
    string? JobId,
    string? ArtifactId,
    string? Permission,
    string? FailureReason,
    IReadOnlyDictionary<string, string>? Metadata);
