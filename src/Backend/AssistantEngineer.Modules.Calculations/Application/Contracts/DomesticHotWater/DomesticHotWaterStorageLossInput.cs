using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterStorageLossInput(
    bool IsStoragePresent,
    double? StorageVolumeLiters,
    double? StorageSetpointTemperatureCelsius,
    double? AmbientTemperatureCelsius,
    IReadOnlyList<double>? HourlyAmbientTemperaturesCelsius8760,
    double? StorageLossCoefficientWPerKelvin,
    double? StandingLossWatts,
    double? OperatingHoursPerDay,
    double? RecoverableFraction,
    DomesticHotWaterLossRecoveryMode RecoveryMode,
    string? Source,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
