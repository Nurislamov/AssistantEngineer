using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

namespace AssistantEngineer.Api.Services.Calculations;

public interface IEngineeringCalculationJobService
{
    Task<EngineeringCalculationJobResultDto> CreateOrRunJobAsync(
        EngineeringCalculationJobRequestDto request,
        CancellationToken cancellationToken);
    Task<EngineeringCalculationJobResultDto?> ExecuteQueuedJobAsync(
        string jobId,
        CancellationToken cancellationToken);
    Task<EngineeringCalculationJobResultDto?> ExecuteClaimedJobAsync(
        string jobId,
        string workerId,
        CancellationToken cancellationToken);

    Task<EngineeringCalculationJobResultDto?> GetJobAsync(
        string jobId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EngineeringCalculationJobResultDto>> ListProjectJobsAsync(
        int projectId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EngineeringCalculationJobEventDto>> ListJobEventsAsync(
        string jobId,
        CancellationToken cancellationToken);

    Task<EngineeringCalculationJobResultDto?> CancelJobAsync(
        string jobId,
        CancellationToken cancellationToken);
}
