namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Construction;

public sealed record Iso52016ConstructionMaterialLayer(
    string LayerId,
    string Name,
    double ThicknessM,
    double ConductivityWPerMK,
    double DensityKgPerM3,
    double SpecificHeatJPerKgK,
    double? ThermalResistanceM2KPerW = null,
    bool IsMassless = false);
