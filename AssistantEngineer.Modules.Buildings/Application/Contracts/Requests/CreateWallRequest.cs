using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;

namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;

public class CreateWallRequest
{
    public double AreaM2 { get; set; }
    public bool IsExternal { get; set; }
    public double UValue { get; set; } = 1.5;
    public CardinalDirectionDto Orientation { get; set; } = CardinalDirectionDto.North;
}
