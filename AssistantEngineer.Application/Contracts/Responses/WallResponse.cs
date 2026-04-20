using AssistantEngineer.Application.Contracts.Common;

namespace AssistantEngineer.Application.Contracts.Responses;

public class WallResponse
{
    public int Id { get; set; }
    public double AreaM2 { get; set; }
    public bool IsExternal { get; set; }
    public double UValue { get; set; }
    public CardinalDirectionDto Orientation { get; set; }
    public int RoomId { get; set; }
}
