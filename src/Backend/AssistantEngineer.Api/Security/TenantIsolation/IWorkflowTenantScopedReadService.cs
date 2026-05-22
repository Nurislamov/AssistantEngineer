using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Api.Security.TenantIsolation;

public interface IWorkflowTenantScopedReadService
{
    Task<Result<WorkflowTenantScopedStateReadResult>> GetWorkflowStateForTenantAsync(
        int projectId,
        int? buildingId,
        TenantQueryContext context,
        CancellationToken cancellationToken = default);

    Task<Result<EngineeringCalculationScenarioRecordDto>> GetScenarioForTenantAsync(
        string scenarioId,
        TenantQueryContext context,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<EngineeringCalculationScenarioRecordDto>>> ListScenariosForProjectForTenantAsync(
        int projectId,
        TenantQueryContext context,
        CancellationToken cancellationToken = default);

    Task<Result<EngineeringCalculationJobResultDto>> GetJobForTenantAsync(
        string jobId,
        TenantQueryContext context,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<EngineeringCalculationJobEventDto>>> GetJobEventsForTenantAsync(
        string jobId,
        TenantQueryContext context,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<EngineeringCalculationJobResultDto>>> ListJobsForProjectForTenantAsync(
        int projectId,
        TenantQueryContext context,
        CancellationToken cancellationToken = default);
}

public sealed record WorkflowTenantScopedStateReadResult(
    EngineeringWorkflowStateDto? PersistedState);
