namespace AssistantEngineer.Modules.Identity.Application.Contracts.Audit;

public sealed record AuditEventRecord(
    string AuditEventId,
    DateTimeOffset OccurredAtUtc,
    string EventType,
    string Category,
    AuditEventOutcome Outcome,
    int? UserId,
    int? OrganizationId,
    string? ExternalSubjectId,
    string? CorrelationId,
    string? RequestId,
    string? ProjectId,
    string? BuildingId,
    string? WorkflowId,
    string? JobId,
    string? ArtifactId,
    string? ResourceType,
    string? ResourceId,
    string? Permission,
    string? FailureReason,
    IReadOnlyDictionary<string, string>? Metadata);
