namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.Artifacts;

public sealed record EngineeringArtifactReadResult(
    EngineeringArtifactDescriptor Descriptor,
    byte[] Content);
