using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterTemperatureModel(
    double ColdWaterTemperatureCelsius,
    double HotWaterSetpointTemperatureCelsius,
    double? UseTemperatureCelsius,
    string? Source,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
