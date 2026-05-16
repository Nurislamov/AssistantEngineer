namespace AssistantEngineer.Modules.Identity.Application.Contracts;

public sealed record PrincipalAccessContext(
    int? UserId,
    int? OrganizationId,
    string? ExternalSubjectId,
    IReadOnlySet<string> Roles,
    IReadOnlySet<string> Permissions,
    bool IsAuthenticated);
