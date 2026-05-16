using AssistantEngineer.Modules.Identity.Application.Contracts.Access;

namespace AssistantEngineer.Api.Security.Authorization;

public sealed class DefaultWorkflowAccessScopeResolver : IWorkflowAccessScopeResolver
{
    public Task<WorkflowAccessScope?> ResolveWorkflowScopeAsync(
        string workflowId,
        CancellationToken cancellationToken)
    {
        _ = workflowId;
        _ = cancellationToken;

        // P5-12: workflow-id to project/building ownership mapping is not fully wired yet.
        // Endpoints that carry projectId/buildingId use those scopes as primary authorization anchors.
        return Task.FromResult<WorkflowAccessScope?>(null);
    }
}
