using AssistantEngineer.Api.Options.Security;

namespace AssistantEngineer.Api.Security.Authorization;

public sealed class ProtectedEndpointTenantMismatchPolicy : IProtectedEndpointTenantMismatchPolicy
{
    public bool ShouldReturnNotFound(ApiAuthorizationOptions options, ProtectedEndpointScopeKind scopeKind)
    {
        if (scopeKind is ProtectedEndpointScopeKind.Workflow or
            ProtectedEndpointScopeKind.WorkflowScenario or
            ProtectedEndpointScopeKind.WorkflowJob)
        {
            return options.ReturnNotFoundForWorkflowTenantMismatch || options.ReturnNotFoundForTenantMismatch;
        }

        return options.ReturnNotFoundForTenantMismatch;
    }
}
