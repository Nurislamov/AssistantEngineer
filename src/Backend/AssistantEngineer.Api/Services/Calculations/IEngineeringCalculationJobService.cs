using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations;

public interface IEngineeringCalculationJobService
{
    Task<EngineeringCalculationJobResultDto> CreateOrRunJobAsync(
        EngineeringCalculationJobRequestDto request,
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
