using AssistantEngineer.Modules.Identity.Application.Contracts.Access;

namespace AssistantEngineer.Api.Security.Authorization;

public interface IWorkflowAccessScopeResolver
{
    Task<WorkflowAccessScope?> ResolveWorkflowScopeAsync(
        string workflowId,
        CancellationToken cancellationToken);
}
