using AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

public sealed class DomesticHotWaterDemandInputValidator : IDomesticHotWaterDemandInputValidator
{
    public DomesticHotWaterDemandValidationResult Validate(DomesticHotWaterUsefulDemandInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(input.Demand.Diagnostics);
        diagnostics.AddRange(input.TemperatureModel.Diagnostics);
        diagnostics.AddRange(input.DrawProfile.Diagnostics);

        if (string.IsNullOrWhiteSpace(input.CalculationId))
        {
            diagnostics.Add(CreateError(
                "AE-DHW-CALCULATION-ID-MISSING",
                "Domestic hot water calculation id is required."));
        }

        if (input.Demand.DemandBasis == DomesticHotWaterDemandBasis.Unknown)
        {
            diagnostics.Add(CreateError(
                "AE-DHW-DEMAND-BASIS-UNKNOWN",
                "Domestic hot water demand basis must not be Unknown."));
        }

        ValidateTemperatures(input, diagnostics);
        ValidateWaterProperties(input, diagnostics);
        ValidateDemandBasisInputs(input.Demand, diagnostics);
        ValidateDrawProfiles(input.DrawProfile, diagnostics);

        return new DomesticHotWaterDemandValidationResult(
            IsValid: diagnostics.All(diagnostic => diagnostic.Severity != CalculationDiagnosticSeverity.Error),
            Diagnostics: diagnostics);
    }

    private static void ValidateTemperatures(
        DomesticHotWaterUsefulDemandInput input,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        var cold = input.TemperatureModel.ColdWaterTemperatureCelsius;
        var hot = input.TemperatureModel.HotWaterSetpointTemperatureCelsius;

        if (!double.IsFinite(cold) || !double.IsFinite(hot))
        {
            diagnostics.Add(CreateError(
                "AE-DHW-TEMPERATURE-INVALID",
                "Cold/hot water temperatures must be finite."));
            return;
        }

        if (hot <= cold)
        {
            diagnostics.Add(CreateError(
                "AE-DHW-TEMPERATURE-RISE-NONPOSITIVE",
                "Hot water setpoint temperature must be greater than cold water temperature."));
        }

        if (input.TemperatureModel.UseTemperatureCelsius is not null)
        {
            var use = input.TemperatureModel.UseTemperatureCelsius.Value;
            if (!double.IsFinite(use) || use < cold || use > hot)
            {
                diagnostics.Add(CreateError(
                    "AE-DHW-TEMPERATURE-INVALID",
                    "Use water temperature must be finite and between cold and hot temperatures."));
            }
        }
    }

    private static void ValidateWaterProperties(
        DomesticHotWaterUsefulDemandInput input,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (input.WaterDensityKgPerLiter is not null &&
            (!double.IsFinite(input.WaterDensityKgPerLiter.Value) ||
             input.WaterDensityKgPerLiter.Value <= 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-DHW-WATER-DENSITY-INVALID",
                "Water density must be positive when provided."));
        }

        if (input.WaterSpecificHeatJPerKgKelvin is not null &&
            (!double.IsFinite(input.WaterSpecificHeatJPerKgKelvin.Value) ||
             input.WaterSpecificHeatJPerKgKelvin.Value <= 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-DHW-WATER-CP-INVALID",
                "Water specific heat must be positive when provided."));
        }
    }

    private static void ValidateDemandBasisInputs(
        DomesticHotWaterDemandBasisInput demand,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        switch (demand.DemandBasis)
        {
            case DomesticHotWaterDemandBasis.People:
                if (demand.OccupantCount is null || demand.OccupantCount <= 0.0)
                {
                    diagnostics.Add(CreateError(
                        "AE-DHW-OCCUPANT-COUNT-MISSING",
                        "People-based demand requires OccupantCount > 0."));
                }

                if (demand.DailyVolumeLitersPerPerson is null || demand.DailyVolumeLitersPerPerson <= 0.0)
                {
                    diagnostics.Add(CreateWarning(
                        "AE-DHW-DAILY-VOLUME-RATE-MISSING",
                        "People-based demand is missing DailyVolumeLitersPerPerson; a deterministic default may be applied."));
                }
                break;

            case DomesticHotWaterDemandBasis.DwellingUnit:
                if (demand.DwellingUnitCount is null || demand.DwellingUnitCount <= 0.0)
                {
                    diagnostics.Add(CreateError(
                        "AE-DHW-DWELLING-COUNT-MISSING",
                        "Dwelling-unit-based demand requires DwellingUnitCount > 0."));
                }

                if (demand.DailyVolumeLitersPerDwellingUnit is null || demand.DailyVolumeLitersPerDwellingUnit <= 0.0)
                {
                    diagnostics.Add(CreateWarning(
                        "AE-DHW-DAILY-VOLUME-RATE-MISSING",
                        "Dwelling-unit-based demand is missing DailyVolumeLitersPerDwellingUnit; a deterministic default may be applied."));
                }
                break;

            case DomesticHotWaterDemandBasis.FloorArea:
                if (demand.FloorAreaSquareMeters is null || demand.FloorAreaSquareMeters <= 0.0)
                {
                    diagnostics.Add(CreateError(
                        "AE-DHW-FLOOR-AREA-MISSING",
                        "Floor-area-based demand requires FloorAreaSquareMeters > 0."));
                }

                if (demand.DailyVolumeLitersPerSquareMeter is null || demand.DailyVolumeLitersPerSquareMeter <= 0.0)
                {
                    diagnostics.Add(CreateWarning(
                        "AE-DHW-DAILY-VOLUME-RATE-MISSING",
                        "Floor-area-based demand is missing DailyVolumeLitersPerSquareMeter; a deterministic default may be applied."));
                }
                break;

            case DomesticHotWaterDemandBasis.FixtureUse:
                if (demand.FixtureUses.Count == 0)
                {
                    diagnostics.Add(CreateError(
                        "AE-DHW-FIXTURE-USES-MISSING",
                        "Fixture-use-based demand requires at least one fixture."));
                }

                foreach (var fixture in demand.FixtureUses)
                {
                    foreach (var diagnostic in fixture.Diagnostics)
                    {
                        diagnostics.Add(diagnostic);
                    }

                    var usesAndLiters = fixture.UsesPerDay is > 0.0 && fixture.LitersPerUse is > 0.0;
                    var usesDurationFlow = fixture.UsesPerDay is > 0.0 &&
                                           fixture.UseDurationMinutes is > 0.0 &&
                                           fixture.FlowRateLitersPerMinute is > 0.0;
                    if (!usesAndLiters && !usesDurationFlow)
                    {
                        diagnostics.Add(CreateError(
                            "AE-DHW-FIXTURE-USE-INCOMPLETE",
                            $"Fixture '{fixture.FixtureId}' is missing complete use data."));
                    }
                }
                break;

            case DomesticHotWaterDemandBasis.CustomDailyVolume:
                if (demand.CustomDailyVolumeLiters is null ||
                    !double.IsFinite(demand.CustomDailyVolumeLiters.Value) ||
                    demand.CustomDailyVolumeLiters.Value <= 0.0)
                {
                    diagnostics.Add(CreateError(
                        "AE-DHW-CUSTOM-DAILY-VOLUME-INVALID",
                        "Custom daily volume must be finite and > 0."));
                }
                break;

            case DomesticHotWaterDemandBasis.CustomHourlyVolume:
                ValidateCustomHourlyVolume(demand.CustomHourlyVolumeLiters, diagnostics);
                break;
        }

        if (demand.CustomDailyProfileFractions is not null)
        {
            ValidateVector(
                demand.CustomDailyProfileFractions,
                expectedLength: 24,
                invalidCode: "AE-DHW-DAILY-PROFILE-INVALID",
                sumCode: "AE-DHW-PROFILE-SUM-NONPOSITIVE",
                label: "Custom daily profile fractions",
                diagnostics: diagnostics);
        }
    }

    private static void ValidateCustomHourlyVolume(
        IReadOnlyList<double>? hourly,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (hourly is null || hourly.Count != 8760)
        {
            diagnostics.Add(CreateError(
                "AE-DHW-CUSTOM-HOURLY-VOLUME-INVALID",
                "Custom hourly volume must contain exactly 8760 values."));
            return;
        }

        if (hourly.Any(value => !double.IsFinite(value) || value < 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-DHW-CUSTOM-HOURLY-VOLUME-INVALID",
                "Custom hourly volume values must be finite and non-negative."));
        }
    }

    private static void ValidateDrawProfiles(
        DomesticHotWaterDrawProfileInput drawProfile,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (drawProfile.HourlyFractions24 is not null)
        {
            ValidateVector(
                drawProfile.HourlyFractions24,
                expectedLength: 24,
                invalidCode: "AE-DHW-DAILY-PROFILE-INVALID",
                sumCode: "AE-DHW-PROFILE-SUM-NONPOSITIVE",
                label: "HourlyFractions24",
                diagnostics: diagnostics);
        }

        if (drawProfile.MonthlyFractions12 is not null)
        {
            ValidateVector(
                drawProfile.MonthlyFractions12,
                expectedLength: 12,
                invalidCode: "AE-DHW-MONTHLY-PROFILE-INVALID",
                sumCode: "AE-DHW-PROFILE-SUM-NONPOSITIVE",
                label: "MonthlyFractions12",
                diagnostics: diagnostics);
        }

        if (drawProfile.AnnualHourlyFractions8760 is not null)
        {
            ValidateVector(
                drawProfile.AnnualHourlyFractions8760,
                expectedLength: 8760,
                invalidCode: "AE-DHW-HOURLY-PROFILE-INVALID",
                sumCode: "AE-DHW-PROFILE-SUM-NONPOSITIVE",
                label: "AnnualHourlyFractions8760",
                diagnostics: diagnostics);
        }
    }

    private static void ValidateVector(
        IReadOnlyList<double> values,
        int expectedLength,
        string invalidCode,
        string sumCode,
        string label,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (values.Count != expectedLength)
        {
            diagnostics.Add(CreateError(
                invalidCode,
                $"{label} must contain exactly {expectedLength} values."));
            return;
        }

        if (values.Any(value => !double.IsFinite(value) || value < 0.0))
        {
            diagnostics.Add(CreateError(
                invalidCode,
                $"{label} values must be finite and non-negative."));
            return;
        }

        if (values.Sum() <= 0.0)
        {
            diagnostics.Add(CreateError(
                sumCode,
                $"{label} sum must be positive before normalization."));
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
            "DomesticHotWaterDemandInputValidator");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        DomesticHotWaterDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.InputPreparation,
            "DomesticHotWaterDemandInputValidator");
}
