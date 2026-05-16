namespace AssistantEngineer.Api.Security.Authorization;

public sealed record RoomAccessScope(
    int RoomId,
    int FloorId,
    int BuildingId,
    int ProjectId,
    int? OrganizationId,
    int? OwnerUserId,
    bool IsTenantScoped);
