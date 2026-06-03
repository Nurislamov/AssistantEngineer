namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.Artifacts;

public sealed record EngineeringArtifactDescriptor(
    string ArtifactId,
    string ArtifactKind,
    string Scope,
    string? SubjectType,
    string? SubjectId,
    string ContentType,
    long SizeBytes,
    string Sha256,
    string StorageProvider,
    string StorageKey,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyDictionary<string, string>? Metadata);
