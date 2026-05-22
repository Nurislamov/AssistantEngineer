namespace AssistantEngineer.Api.Security.Authorization;

public enum ProtectedEndpointScopeEvaluationKind
{
    NotEvaluated = 0,
    Allowed = 1,
    ScopeMissing = 2,
    TenantMismatch = 3
}
