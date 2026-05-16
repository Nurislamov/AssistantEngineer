namespace AssistantEngineer.Api.Security.Authentication;

public sealed record AuthenticatedPrincipal(
    int? UserId,
    int? OrganizationId,
    string? ExternalSubjectId,
    string AuthenticationScheme,
    IReadOnlySet<string> Roles,
    IReadOnlySet<string> Permissions,
    bool IsAuthenticated)
{
    public static readonly AuthenticatedPrincipal Anonymous = new(
        UserId: null,
        OrganizationId: null,
        ExternalSubjectId: null,
        AuthenticationScheme: "Anonymous",
        Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        Permissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        IsAuthenticated: false);
}
