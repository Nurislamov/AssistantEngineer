namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground.Iso13370;

public sealed record Iso13370GroundCalculationOptions(
    bool EnableSeasonalComponent = true,
    bool EnablePerimeterThermalBridge = true,
    double SeasonalAttenuationFactor = 0.55,
    double MonthlyHeatTransferVariationFactor = 0.05,
    double MinimumEquivalentGroundThicknessM = 0.3,
    double MaximumEquivalentGroundThicknessM = 8.0);

