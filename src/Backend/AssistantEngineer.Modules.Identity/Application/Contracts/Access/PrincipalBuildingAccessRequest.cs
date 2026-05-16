using AssistantEngineer.Modules.Identity.Domain.Enums;

namespace AssistantEngineer.Modules.Identity.Application.Contracts.Access;

public sealed record PrincipalBuildingAccessRequest(
    PrincipalAccessContext Principal,
    int BuildingId,
    Permission RequiredPermission);
