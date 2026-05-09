using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterLossDefinition(
    DomesticHotWaterSystemKind SystemKind,
    double? StorageVolumeLiters,
    double? StorageLossCoefficientWPerKelvin,
    double? StorageAmbientTemperatureCelsius,
    double? DistributionPipeLengthMeters,
    double? DistributionLossCoefficientWPerMeterKelvin,
    IReadOnlyList<double>? CirculationOperationSchedule,
    double? CirculationOperationFraction,
    double? CirculationLoopLengthMeters,
    double? CirculationLossCoefficientWPerMeterKelvin,
    double RecoveredLossFraction,
    IReadOnlyList<double>? AuxiliaryEnergyProfileKWh,
    double? AuxiliaryEnergyPerStepKWh,
    DomesticHotWaterLossOwnershipPolicy LossOwnershipPolicy,
    double TimeStepHours,
    string? Source,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
