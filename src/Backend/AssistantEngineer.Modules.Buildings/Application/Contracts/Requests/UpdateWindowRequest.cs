using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;

namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;

public class UpdateWindowRequest
{
    public double AreaM2 { get; set; }
    public double UValue { get; set; } = 2.5;
    public double Shgc { get; set; } = 0.6;
    public CardinalDirectionDto Orientation { get; set; } = CardinalDirectionDto.North;
    public WindowShadingParametersRequest Shading { get; set; } = new();
}
