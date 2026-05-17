namespace AssistantEngineer.Modules.Identity.Application.Contracts.Access;

public sealed record TenantScopedQueryDecision(
    bool Allowed,
    bool IsTenantScoped,
    bool IsUnscopedTransition,
    string? FailureReason,
    bool ShouldReturnNotFound,
    IReadOnlyDictionary<string, string>? Metadata = null)
{
    public static TenantScopedQueryDecision AllowTenantScoped(
        IReadOnlyDictionary<string, string>? metadata = null) =>
        new(
            Allowed: true,
            IsTenantScoped: true,
            IsUnscopedTransition: false,
            FailureReason: null,
            ShouldReturnNotFound: false,
            Metadata: metadata);

    public static TenantScopedQueryDecision AllowUnscopedTransition(
        IReadOnlyDictionary<string, string>? metadata = null) =>
        new(
            Allowed: true,
            IsTenantScoped: false,
            IsUnscopedTransition: true,
            FailureReason: null,
            ShouldReturnNotFound: false,
            Metadata: metadata);

    public static TenantScopedQueryDecision Deny(
        string failureReason,
        bool shouldReturnNotFound = false,
        IReadOnlyDictionary<string, string>? metadata = null) =>
        new(
            Allowed: false,
            IsTenantScoped: false,
            IsUnscopedTransition: false,
            FailureReason: failureReason,
            ShouldReturnNotFound: shouldReturnNotFound,
            Metadata: metadata);
}
