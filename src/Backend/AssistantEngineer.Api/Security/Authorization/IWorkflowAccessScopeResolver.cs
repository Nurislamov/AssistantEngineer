using AssistantEngineer.Modules.Identity.Application.Contracts.Access;

namespace AssistantEngineer.Api.Security.Authorization;

public interface IWorkflowAccessScopeResolver
{
    Task<WorkflowAccessScope?> ResolveWorkflowScopeAsync(
        string workflowId,
        CancellationToken cancellationToken);

    Task<WorkflowAccessScope?> ResolveScenarioScopeAsync(
        string scenarioId,
        CancellationToken cancellationToken);

    Task<WorkflowAccessScope?> ResolveJobScopeAsync(
        string jobId,
        CancellationToken cancellationToken);
}
