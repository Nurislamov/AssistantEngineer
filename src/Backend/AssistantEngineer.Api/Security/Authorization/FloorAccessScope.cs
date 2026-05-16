namespace AssistantEngineer.Api.Security.Authorization;

public sealed record FloorAccessScope(
    int FloorId,
    int BuildingId,
    int ProjectId,
    int? OrganizationId,
    int? OwnerUserId,
    bool IsTenantScoped);
