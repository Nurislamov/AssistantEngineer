using AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

public sealed class DomesticHotWaterSystemLossInputValidator : IDomesticHotWaterSystemLossInputValidator
{
    public DomesticHotWaterSystemLossValidationResult Validate(DomesticHotWaterSystemLossInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(input.UsefulDemand.Diagnostics);
        diagnostics.AddRange(input.Storage.Diagnostics);
        diagnostics.AddRange(input.Distribution.Diagnostics);
        diagnostics.AddRange(input.Circulation.Diagnostics);

        if (string.IsNullOrWhiteSpace(input.CalculationId))
        {
            diagnostics.Add(CreateError(
                "AE-DHW-SYSTEM-CALCULATION-ID-MISSING",
                "Domestic hot water system-loss calculation id is required."));
        }

        if (input.UsefulDemand is null)
        {
            diagnostics.Add(CreateError(
                "AE-DHW-SYSTEM-USEFUL-DEMAND-MISSING",
                "Domestic hot water useful demand result is required."));
        }
        else
        {
            if (input.UsefulDemand.HourlyUsefulEnergyKWh8760.Count != 8760 ||
                input.UsefulDemand.HourlyUsefulEnergyKWh8760.Any(value => !double.IsFinite(value) || value < 0.0))
            {
                diagnostics.Add(CreateError(
                    "AE-DHW-SYSTEM-USEFUL-HOURLY-PROFILE-INVALID",
                    "Useful demand hourly useful-energy profile must contain 8760 finite non-negative values."));
            }

            if (input.UsefulDemand.HourlyVolumeLiters8760.Count != 8760 ||
                input.UsefulDemand.HourlyVolumeLiters8760.Any(value => !double.IsFinite(value) || value < 0.0))
            {
                diagnostics.Add(CreateError(
                    "AE-DHW-SYSTEM-USEFUL-HOURLY-PROFILE-INVALID",
                    "Useful demand hourly volume profile must contain 8760 finite non-negative values."));
            }
        }

        if (input.DefaultAmbientTemperatureCelsius is not null &&
            !double.IsFinite(input.DefaultAmbientTemperatureCelsius.Value))
        {
            diagnostics.Add(CreateError(
                "AE-DHW-SYSTEM-DEFAULT-AMBIENT-INVALID",
                "Default ambient temperature must be finite when provided."));
        }

        if (input.DefaultRecoverableFraction is not null &&
            (!double.IsFinite(input.DefaultRecoverableFraction.Value) ||
             input.DefaultRecoverableFraction.Value < 0.0 ||
             input.DefaultRecoverableFraction.Value > 1.0))
        {
            diagnostics.Add(CreateError(
                "AE-DHW-SYSTEM-DEFAULT-RECOVERABLE-FRACTION-INVALID",
                "Default recoverable fraction must be within [0, 1] when provided."));
        }

        ValidateStorage(input.Storage, diagnostics);
        ValidateDistribution(input.Distribution, diagnostics);
        ValidateCirculation(input.Circulation, diagnostics);

        return new DomesticHotWaterSystemLossValidationResult(
            IsValid: diagnostics.All(diagnostic => diagnostic.Severity != CalculationDiagnosticSeverity.Error),
            Diagnostics: diagnostics);
    }

    private static void ValidateStorage(
        DomesticHotWaterStorageLossInput storage,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (!storage.IsStoragePresent)
            return;

        var hasStandingLoss = storage.StandingLossWatts is > 0.0;
        var hasCoefficientMethod = storage.StorageLossCoefficientWPerKelvin is > 0.0 &&
                                   storage.StorageSetpointTemperatureCelsius is not null &&
                                   (storage.AmbientTemperatureCelsius is not null ||
                                    storage.HourlyAmbientTemperaturesCelsius8760 is not null);
        if (!hasStandingLoss && !hasCoefficientMethod)
        {
            diagnostics.Add(CreateError(
                "AE-DHW-STORAGE-INPUT-INCOMPLETE",
                "Storage loss input requires StandingLossWatts or valid coefficient-method data."));
        }

        if (storage.StorageVolumeLiters is not null &&
            (!double.IsFinite(storage.StorageVolumeLiters.Value) || storage.StorageVolumeLiters.Value <= 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-DHW-STORAGE-VOLUME-INVALID",
                "Storage volume must be > 0 when provided."));
        }

        if (storage.StorageLossCoefficientWPerKelvin is not null &&
            (!double.IsFinite(storage.StorageLossCoefficientWPerKelvin.Value) ||
             storage.StorageLossCoefficientWPerKelvin.Value <= 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-DHW-STORAGE-LOSS-COEFFICIENT-INVALID",
                "Storage loss coefficient must be > 0 when provided."));
        }

        if (storage.StandingLossWatts is not null &&
            (!double.IsFinite(storage.StandingLossWatts.Value) ||
             storage.StandingLossWatts.Value < 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-DHW-STORAGE-STANDING-LOSS-INVALID",
                "Storage standing loss must be >= 0 when provided."));
        }

        if (storage.StorageSetpointTemperatureCelsius is not null &&
            !double.IsFinite(storage.StorageSetpointTemperatureCelsius.Value))
        {
            diagnostics.Add(CreateError(
                "AE-DHW-STORAGE-TEMPERATURE-INVALID",
                "Storage setpoint temperature must be finite when provided."));
        }

        ValidateOperatingHoursAndRecoverableFraction(
            storage.OperatingHoursPerDay,
            storage.RecoverableFraction,
            diagnostics);
        ValidateHourlyAmbient(storage.HourlyAmbientTemperaturesCelsius8760, diagnostics);
    }

    private static void ValidateDistribution(
        DomesticHotWaterDistributionLossInput distribution,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (!distribution.IsDistributionPresent)
            return;

        if (distribution.PipeLengthMeters is null ||
            !double.IsFinite(distribution.PipeLengthMeters.Value) ||
            distribution.PipeLengthMeters.Value <= 0.0)
        {
            diagnostics.Add(CreateError(
                "AE-DHW-DISTRIBUTION-PIPE-LENGTH-INVALID",
                "Distribution pipe length must be > 0 when distribution is present."));
        }

        if (distribution.PipeLinearLossCoefficientWPerMeterKelvin is null ||
            !double.IsFinite(distribution.PipeLinearLossCoefficientWPerMeterKelvin.Value) ||
            distribution.PipeLinearLossCoefficientWPerMeterKelvin.Value <= 0.0)
        {
            diagnostics.Add(CreateError(
                "AE-DHW-DISTRIBUTION-LINEAR-LOSS-INVALID",
                "Distribution linear loss coefficient must be > 0 when distribution is present."));
        }

        if (distribution.SupplyTemperatureCelsius is null ||
            !double.IsFinite(distribution.SupplyTemperatureCelsius.Value))
        {
            diagnostics.Add(CreateError(
                "AE-DHW-DISTRIBUTION-TEMPERATURE-INVALID",
                "Distribution supply temperature must be finite when distribution is present."));
        }

        ValidateOperatingHoursAndRecoverableFraction(
            distribution.OperatingHoursPerDay,
            distribution.RecoverableFraction,
            diagnostics);
        ValidateHourlyAmbient(distribution.HourlyAmbientTemperaturesCelsius8760, diagnostics);
    }

    private static void ValidateCirculation(
        DomesticHotWaterCirculationLossInput circulation,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (!circulation.IsCirculationPresent)
            return;

        if (circulation.LoopLengthMeters is null ||
            !double.IsFinite(circulation.LoopLengthMeters.Value) ||
            circulation.LoopLengthMeters.Value <= 0.0)
        {
            diagnostics.Add(CreateError(
                "AE-DHW-CIRCULATION-LOOP-LENGTH-INVALID",
                "Circulation loop length must be > 0 when circulation is present."));
        }

        if (circulation.LoopLinearLossCoefficientWPerMeterKelvin is null ||
            !double.IsFinite(circulation.LoopLinearLossCoefficientWPerMeterKelvin.Value) ||
            circulation.LoopLinearLossCoefficientWPerMeterKelvin.Value <= 0.0)
        {
            diagnostics.Add(CreateError(
                "AE-DHW-CIRCULATION-LINEAR-LOSS-INVALID",
                "Circulation linear loss coefficient must be > 0 when circulation is present."));
        }

        if (circulation.SupplyTemperatureCelsius is null ||
            !double.IsFinite(circulation.SupplyTemperatureCelsius.Value))
        {
            diagnostics.Add(CreateError(
                "AE-DHW-CIRCULATION-INPUT-INCOMPLETE",
                "Circulation supply temperature must be finite when circulation is present."));
        }

        if (circulation.ReturnTemperatureCelsius is not null &&
            !double.IsFinite(circulation.ReturnTemperatureCelsius.Value))
        {
            diagnostics.Add(CreateError(
                "AE-DHW-CIRCULATION-INPUT-INCOMPLETE",
                "Circulation return temperature must be finite when provided."));
        }

        if (circulation.PumpPowerWatts is not null &&
            (!double.IsFinite(circulation.PumpPowerWatts.Value) ||
             circulation.PumpPowerWatts.Value < 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-DHW-CIRCULATION-PUMP-POWER-INVALID",
                "Circulation pump power must be >= 0 when provided."));
        }

        if (circulation.HourlyOperationFractions8760 is not null)
        {
            var profile = circulation.HourlyOperationFractions8760;
            if (profile.Count != 8760 ||
                profile.Any(value => !double.IsFinite(value) || value < 0.0))
            {
                diagnostics.Add(CreateError(
                    "AE-DHW-CIRCULATION-OPERATION-PROFILE-INVALID",
                    "Circulation hourly operation profile must contain 8760 finite non-negative values."));
            }
            else if (profile.Any(value => value > 1.0))
            {
                diagnostics.Add(CreateWarning(
                    "AE-DHW-CIRCULATION-OPERATION-PROFILE-INVALID",
                    "Circulation hourly operation profile contains values above 1.0 and may be clamped in calculation."));
            }
        }

        ValidateOperatingHoursAndRecoverableFraction(
            circulation.OperatingHoursPerDay,
            circulation.RecoverableFraction,
            diagnostics);
        ValidateHourlyAmbient(circulation.HourlyAmbientTemperaturesCelsius8760, diagnostics);
    }

    private static void ValidateOperatingHoursAndRecoverableFraction(
        double? operatingHoursPerDay,
        double? recoverableFraction,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (operatingHoursPerDay is not null &&
            (!double.IsFinite(operatingHoursPerDay.Value) ||
             operatingHoursPerDay.Value < 0.0 ||
             operatingHoursPerDay.Value > 24.0))
        {
            diagnostics.Add(CreateError(
                "AE-DHW-LOSS-OPERATING-HOURS-INVALID",
                "Operating hours per day must be within [0, 24] when provided."));
        }

        if (recoverableFraction is not null &&
            (!double.IsFinite(recoverableFraction.Value) ||
             recoverableFraction.Value < 0.0 ||
             recoverableFraction.Value > 1.0))
        {
            diagnostics.Add(CreateError(
                "AE-DHW-LOSS-RECOVERABLE-FRACTION-INVALID",
                "Recoverable fraction must be within [0, 1] when provided."));
        }
    }

    private static void ValidateHourlyAmbient(
        IReadOnlyList<double>? hourlyAmbient,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (hourlyAmbient is null)
            return;

        if (hourlyAmbient.Count != 8760 ||
            hourlyAmbient.Any(value => !double.IsFinite(value)))
        {
            diagnostics.Add(CreateError(
                "AE-DHW-STORAGE-TEMPERATURE-INVALID",
                "Hourly ambient temperature profile must contain 8760 finite values."));
        }
    }

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        DomesticHotWaterDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Error,
            code,
            message,
            StandardCalculationStage.InputPreparation,
            "DomesticHotWaterSystemLossInputValidator");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        DomesticHotWaterDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.InputPreparation,
            "DomesticHotWaterSystemLossInputValidator");
}
