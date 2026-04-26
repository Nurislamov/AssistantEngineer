using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;

namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

public class WindowResponse
{
    public int Id { get; set; }
    public double AreaM2 { get; set; }
    public double UValue { get; set; }
    public double Shgc { get; set; }
    public CardinalDirectionDto Orientation { get; set; }
    public int RoomId { get; set; }
    public WindowShadingParametersResponse Shading { get; set; } = new();
}

public class WindowShadingParametersResponse
{
    public double OverhangDepthM { get; set; }
    public double SideFinDepthM { get; set; }
    public double RevealDepthM { get; set; }
    public double WindowHeightM { get; set; }
    public double WindowWidthM { get; set; }
    public double MinimumDirectSolarReductionFactor { get; set; }
    public double DiffuseSolarShareUnaffected { get; set; }
}
