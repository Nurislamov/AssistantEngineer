using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;

namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;

public class UpdateWallRequest
{
    public double AreaM2 { get; set; }
    public double UValue { get; set; } = 1.5;
    public CardinalDirectionDto Orientation { get; set; } = CardinalDirectionDto.North;
    public WallBoundaryTypeDto BoundaryType { get; set; } = WallBoundaryTypeDto.External;
    public int? AdjacentRoomId { get; set; }
}
