using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterDistributionLossInput(
    bool IsDistributionPresent,
    double? PipeLengthMeters,
    double? PipeLinearLossCoefficientWPerMeterKelvin,
    double? SupplyTemperatureCelsius,
    double? AmbientTemperatureCelsius,
    IReadOnlyList<double>? HourlyAmbientTemperaturesCelsius8760,
    double? OperatingHoursPerDay,
    double? RecoverableFraction,
    DomesticHotWaterLossRecoveryMode RecoveryMode,
    string? Source,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
