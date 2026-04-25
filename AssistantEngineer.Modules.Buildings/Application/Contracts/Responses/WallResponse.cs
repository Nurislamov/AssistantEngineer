using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;

namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

public class WallResponse
{
    public int Id { get; set; }
    public double AreaM2 { get; set; }
    public bool IsExternal { get; set; }
    public double UValue { get; set; }
    public CardinalDirectionDto Orientation { get; set; }
    public WallBoundaryTypeDto BoundaryType { get; set; }
    public int RoomId { get; set; }
    public int? AdjacentRoomId { get; set; }
}