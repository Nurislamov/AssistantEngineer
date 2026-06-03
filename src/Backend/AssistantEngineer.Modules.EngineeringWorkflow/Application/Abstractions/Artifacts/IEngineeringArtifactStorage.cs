using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.Artifacts;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Abstractions.Artifacts;

public interface IEngineeringArtifactStorage
{
    Task<Result<EngineeringArtifactDescriptor>> WriteAsync(
        EngineeringArtifactWriteRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<EngineeringArtifactReadResult>> ReadAsync(
        string artifactId,
        CancellationToken cancellationToken = default);

    Task<Result<EngineeringArtifactDescriptor>> GetDescriptorAsync(
        string artifactId,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        string artifactId,
        CancellationToken cancellationToken = default);
}
