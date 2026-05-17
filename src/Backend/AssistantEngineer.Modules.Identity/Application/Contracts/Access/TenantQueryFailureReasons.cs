namespace AssistantEngineer.Modules.Identity.Application.Contracts.Access;

public static class TenantQueryFailureReasons
{
    public const string Unauthenticated = "Unauthenticated";
    public const string MissingOrganization = "MissingOrganization";
    public const string TenantMismatch = "TenantMismatch";
    public const string UnscopedResourceDenied = "UnscopedResourceDenied";
    public const string ResourceNotFound = "ResourceNotFound";
    public const string MissingPermission = "MissingPermission";
}
