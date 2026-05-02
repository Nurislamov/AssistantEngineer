using AssistantEngineer.Modules.Buildings.Domain.Enums;

namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

public sealed record BuildingArchetypeSummary(
    string Code,
    string DisplayName,
    RoomType Type,
    int RoomsCount,
    double RoomAreaM2,
    double RoomHeightM);