using AssistantEngineer.Modules.Identity.Application.Contracts.Access;

namespace AssistantEngineer.Modules.Identity.Application.Services.Access;

public sealed class TenantQueryIsolationPolicy : ITenantQueryIsolationPolicy
{
    public TenantScopedQueryDecision CanReadResource(
        TenantQueryContext context,
        int? resourceOrganizationId,
        string requiredPermission) =>
        Evaluate(context, resourceOrganizationId, requiredPermission, operation: "Read");

    public TenantScopedQueryDecision CanWriteResource(
        TenantQueryContext context,
        int? resourceOrganizationId,
        string requiredPermission) =>
        Evaluate(context, resourceOrganizationId, requiredPermission, operation: "Write");

    private static TenantScopedQueryDecision Evaluate(
        TenantQueryContext context,
        int? resourceOrganizationId,
        string requiredPermission,
        string operation)
    {
        if (string.IsNullOrWhiteSpace(requiredPermission))
        {
            throw new ArgumentException("Required permission must not be empty.", nameof(requiredPermission));
        }

        if (!context.IsAuthenticated)
        {
            return TenantScopedQueryDecision.Deny(
                TenantQueryFailureReasons.Unauthenticated,
                metadata: CreateMetadata(operation, requiredPermission));
        }

        if (!HasPermission(context, requiredPermission))
        {
            return TenantScopedQueryDecision.Deny(
                TenantQueryFailureReasons.MissingPermission,
                metadata: CreateMetadata(operation, requiredPermission));
        }

        if (resourceOrganizationId.HasValue)
        {
            if (!context.OrganizationId.HasValue)
            {
                return TenantScopedQueryDecision.Deny(
                    TenantQueryFailureReasons.MissingOrganization,
                    shouldReturnNotFound: context.ReturnNotFoundForTenantMismatch,
                    metadata: CreateMetadata(operation, requiredPermission));
            }

            if (context.OrganizationId.Value == resourceOrganizationId.Value)
            {
                return TenantScopedQueryDecision.AllowTenantScoped(
                    CreateMetadata(operation, requiredPermission));
            }

            return TenantScopedQueryDecision.Deny(
                TenantQueryFailureReasons.TenantMismatch,
                shouldReturnNotFound: context.ReturnNotFoundForTenantMismatch,
                metadata: CreateMetadata(operation, requiredPermission));
        }

        if (context.AllowUnscopedResourcesDuringTransition)
        {
            return TenantScopedQueryDecision.AllowUnscopedTransition(
                CreateMetadata(operation, requiredPermission));
        }

        return TenantScopedQueryDecision.Deny(
            TenantQueryFailureReasons.UnscopedResourceDenied,
            shouldReturnNotFound: context.ReturnNotFoundForTenantMismatch,
            metadata: CreateMetadata(operation, requiredPermission));
    }

    private static bool HasPermission(TenantQueryContext context, string requiredPermission) =>
        context.Permissions.Any(permission =>
            string.Equals(permission, requiredPermission, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyDictionary<string, string> CreateMetadata(
        string operation,
        string requiredPermission) =>
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["operation"] = operation,
            ["requiredPermission"] = requiredPermission
        };
}
