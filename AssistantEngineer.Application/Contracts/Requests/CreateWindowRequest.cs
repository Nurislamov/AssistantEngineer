using AssistantEngineer.Application.Contracts.Common;

namespace AssistantEngineer.Application.Contracts.Requests;

public class CreateWindowRequest
{
    public double AreaM2 { get; set; }
    public double UValue { get; set; } = 2.5;
    public double Shgc { get; set; } = 0.6;
    public CardinalDirectionDto Orientation { get; set; } = CardinalDirectionDto.North;
    public WindowShadingParametersRequest Shading { get; set; } = new();
}

public class WindowShadingParametersRequest
{
    public double OverhangDepthM { get; set; }
    public double SideFinDepthM { get; set; }
    public double RevealDepthM { get; set; }
    public double WindowHeightM { get; set; }
    public double WindowWidthM { get; set; }
    public double MinimumDirectSolarReductionFactor { get; set; } = 0.15;
    public double DiffuseSolarShareUnaffected { get; set; } = 0.3;
}
