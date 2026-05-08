using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterCirculationLossInput(
    bool IsCirculationPresent,
    double? LoopLengthMeters,
    double? LoopLinearLossCoefficientWPerMeterKelvin,
    double? SupplyTemperatureCelsius,
    double? ReturnTemperatureCelsius,
    double? AmbientTemperatureCelsius,
    IReadOnlyList<double>? HourlyAmbientTemperaturesCelsius8760,
    IReadOnlyList<double>? HourlyOperationFractions8760,
    double? OperatingHoursPerDay,
    double? PumpPowerWatts,
    double? RecoverableFraction,
    DomesticHotWaterLossRecoveryMode RecoveryMode,
    string? Source,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
