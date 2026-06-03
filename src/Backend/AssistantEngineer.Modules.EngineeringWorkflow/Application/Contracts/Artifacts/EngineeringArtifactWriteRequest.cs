namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.Artifacts;

public sealed record EngineeringArtifactWriteRequest(
    string ArtifactKind,
    string Scope,
    string? SubjectType,
    string? SubjectId,
    string ContentType,
    byte[] Content,
    IReadOnlyDictionary<string, string>? Metadata);
