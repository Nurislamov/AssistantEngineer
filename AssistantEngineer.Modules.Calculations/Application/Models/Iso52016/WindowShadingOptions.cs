namespace AssistantEngineer.Modules.Calculations.Application.Models.Iso52016;

public sealed record WindowShadingOptions(
    double OverhangDepthM,
    double SideFinDepthM,
    double RevealDepthM,
    double WindowHeightM,
    double WindowWidthM,
    double MinimumDirectSolarReductionFactor,
    double DiffuseSolarShareUnaffected);
