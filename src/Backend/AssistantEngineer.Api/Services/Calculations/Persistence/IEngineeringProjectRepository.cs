using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public interface IEngineeringProjectRepository
{
    Task<EngineeringProjectRecordDto> UpsertAsync(
        EngineeringProjectRecordDto project,
        CancellationToken cancellationToken);

    Task<EngineeringProjectRecordDto?> GetByIdAsync(
        int projectId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EngineeringProjectRecordDto>> ListAsync(
        CancellationToken cancellationToken);

    Task<EngineeringProjectRecordDto?> UpdateMetadataAsync(
        int projectId,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken);
}
