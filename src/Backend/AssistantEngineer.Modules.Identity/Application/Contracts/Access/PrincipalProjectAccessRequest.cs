using AssistantEngineer.Modules.Identity.Domain.Enums;

namespace AssistantEngineer.Modules.Identity.Application.Contracts.Access;

public sealed record PrincipalProjectAccessRequest(
    PrincipalAccessContext Principal,
    int ProjectId,
    Permission RequiredPermission);
