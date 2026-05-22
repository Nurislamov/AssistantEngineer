using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public interface IEngineeringCalculationJobRepository
{
    Task<EngineeringCalculationJobRecordDto> CreateAsync(
        EngineeringCalculationJobRecordDto job,
        CancellationToken cancellationToken);

    Task<EngineeringCalculationJobRecordDto> UpdateAsync(
        EngineeringCalculationJobRecordDto job,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<EngineeringCalculationJobRecordDto>> ListQueuedAsync(
        int maxCount,
        CancellationToken cancellationToken);

    Task<EngineeringCalculationJobRecordDto?> TryClaimQueuedJobAsync(
        string jobId,
        string workerId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    Task<EngineeringCalculationJobRecordDto?> GetByIdAsync(
        string jobId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EngineeringCalculationJobRecordDto>> ListByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken);
}
