using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations.Workflow;

public interface IEngineeringWorkflowSubmissionService
{
    Task<EngineeringWorkflowSubmissionResult<EngineeringCalculationScenarioResultDto>> RunCalculationAsync(
        EngineeringCalculationScenarioRequestDto request,
        string? idempotencyKey,
        CancellationToken cancellationToken);

    Task<EngineeringWorkflowSubmissionResult<EngineeringCalculationJobResultDto>> CreateOrRunJobAsync(
        EngineeringCalculationJobRequestDto request,
        string? idempotencyKey,
        CancellationToken cancellationToken);
}

public sealed record EngineeringWorkflowSubmissionResult<T>(
    bool IsConflict,
    T? Payload,
    string? ConflictCode = null,
    string? ConflictMessage = null);
