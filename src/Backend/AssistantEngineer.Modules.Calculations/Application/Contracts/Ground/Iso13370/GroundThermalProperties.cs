namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground.Iso13370;

public sealed record GroundThermalProperties(
    double GroundConductivityWPerMK,
    double EquivalentGroundThicknessM = 0.0);

