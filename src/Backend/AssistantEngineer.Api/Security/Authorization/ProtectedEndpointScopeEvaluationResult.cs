namespace AssistantEngineer.Api.Security.Authorization;

public sealed record ProtectedEndpointScopeEvaluationResult(
    ProtectedEndpointScopeEvaluationKind Kind,
    ProtectedEndpointScopeKind ScopeKind,
    int? ProjectId = null,
    int? BuildingId = null,
    string? ScopeIdentifier = null)
{
    public bool ScopeResolved => Kind == ProtectedEndpointScopeEvaluationKind.Allowed;

    public bool ScopeMissing => Kind == ProtectedEndpointScopeEvaluationKind.ScopeMissing;

    public bool TenantMismatch => Kind == ProtectedEndpointScopeEvaluationKind.TenantMismatch;

    public static ProtectedEndpointScopeEvaluationResult NotEvaluated(
        ProtectedEndpointScopeKind scopeKind = ProtectedEndpointScopeKind.None) =>
        new(ProtectedEndpointScopeEvaluationKind.NotEvaluated, scopeKind);

    public static ProtectedEndpointScopeEvaluationResult Allowed(
        ProtectedEndpointScopeKind scopeKind,
        int? projectId = null,
        int? buildingId = null,
        string? scopeIdentifier = null) =>
        new(ProtectedEndpointScopeEvaluationKind.Allowed, scopeKind, projectId, buildingId, scopeIdentifier);

    public static ProtectedEndpointScopeEvaluationResult Missing(
        ProtectedEndpointScopeKind scopeKind,
        int? projectId = null,
        int? buildingId = null,
        string? scopeIdentifier = null) =>
        new(ProtectedEndpointScopeEvaluationKind.ScopeMissing, scopeKind, projectId, buildingId, scopeIdentifier);

    public static ProtectedEndpointScopeEvaluationResult Mismatch(
        ProtectedEndpointScopeKind scopeKind,
        int? projectId = null,
        int? buildingId = null,
        string? scopeIdentifier = null) =>
        new(ProtectedEndpointScopeEvaluationKind.TenantMismatch, scopeKind, projectId, buildingId, scopeIdentifier);
}
