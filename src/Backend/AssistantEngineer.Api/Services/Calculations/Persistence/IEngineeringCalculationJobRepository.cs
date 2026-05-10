using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public interface IEngineeringCalculationJobRepository
{
    Task<EngineeringCalculationJobRecordDto> CreateAsync(
        EngineeringCalculationJobRecordDto job,
        CancellationToken cancellationToken);

    Task<EngineeringCalculationJobRecordDto> UpdateAsync(
        EngineeringCalculationJobRecordDto job,
        CancellationToken cancellationToken);

    Task<EngineeringCalculationJobRecordDto?> GetByIdAsync(
        string jobId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EngineeringCalculationJobRecordDto>> ListByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken);
}
