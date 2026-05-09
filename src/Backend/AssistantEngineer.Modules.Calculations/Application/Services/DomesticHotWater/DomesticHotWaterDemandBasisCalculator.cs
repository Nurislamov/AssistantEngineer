using AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater.Iso12831;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater.Iso12831;

namespace AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

public sealed class DomesticHotWaterDemandBasisCalculator : IDomesticHotWaterDemandBasisCalculator
{
    private readonly Iso12831DomesticHotWaterReferenceDataProvider _referenceDataProvider;

    public DomesticHotWaterDemandBasisCalculator(
        Iso12831DomesticHotWaterReferenceDataProvider referenceDataProvider)
    {
        _referenceDataProvider = referenceDataProvider ?? throw new ArgumentNullException(nameof(referenceDataProvider));
    }

    public DomesticHotWaterDemandBasisResult CalculateDailyVolume(DomesticHotWaterDemandBasisInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(input.Diagnostics);

        var defaults = _referenceDataProvider.Resolve(MapUseCategory(input.UseCategory));
        var dailyVolume = 0.0;
        var customHourly = Array.Empty<double>();
        var usesCustomHourly = false;
        IReadOnlyList<double>? scheduledEnergy = null;
        var usesScheduledEnergy = false;

        switch (input.DemandBasis)
        {
            case DomesticHotWaterDemandBasis.People:
                {
                    var rate = input.DailyVolumeLitersPerPerson;
                    if (rate is null || rate <= 0.0)
                    {
                        rate = defaults.LitersPerPersonDay;
                        diagnostics.Add(CreateWarning(
                            "AE-DHW-REFERENCE-RATE-DEFAULTED",
                            $"People-based volume rate defaulted to {rate:F3} l/person/day."));
                    }

                    dailyVolume = Math.Max(0.0, input.OccupantCount.GetValueOrDefault()) * rate.Value;
                    break;
                }

            case DomesticHotWaterDemandBasis.DwellingUnit:
                {
                    var rate = input.DailyVolumeLitersPerDwellingUnit;
                    if (rate is null || rate <= 0.0)
                    {
                        rate = defaults.LitersPerUnitDay;
                        diagnostics.Add(CreateWarning(
                            "AE-DHW-REFERENCE-RATE-DEFAULTED",
                            $"Dwelling-unit-based volume rate defaulted to {rate:F3} l/unit/day."));
                    }

                    dailyVolume = Math.Max(0.0, input.DwellingUnitCount.GetValueOrDefault()) * rate.Value;
                    break;
                }

            case DomesticHotWaterDemandBasis.FloorArea:
                {
                    var rate = input.DailyVolumeLitersPerSquareMeter;
                    if (rate is null || rate <= 0.0)
                    {
                        rate = defaults.LitersPerM2Day;
                        diagnostics.Add(CreateWarning(
                            "AE-DHW-REFERENCE-RATE-DEFAULTED",
                            $"Floor-area-based volume rate defaulted to {rate:F3} l/m2/day."));
                    }

                    dailyVolume = Math.Max(0.0, input.FloorAreaSquareMeters.GetValueOrDefault()) * rate.Value;
                    break;
                }

            case DomesticHotWaterDemandBasis.FixtureUse:
                {
                    dailyVolume = 0.0;
                    foreach (var fixture in input.FixtureUses)
                    {
                        diagnostics.AddRange(fixture.Diagnostics);

                        var fixtureVolume = 0.0;
                        if (fixture.UsesPerDay is > 0.0 && fixture.LitersPerUse is > 0.0)
                        {
                            fixtureVolume = fixture.UsesPerDay.Value * fixture.LitersPerUse.Value;
                        }
                        else if (fixture.UsesPerDay is > 0.0 &&
                                 fixture.UseDurationMinutes is > 0.0 &&
                                 fixture.FlowRateLitersPerMinute is > 0.0)
                        {
                            fixtureVolume = fixture.UsesPerDay.Value *
                                            fixture.UseDurationMinutes.Value *
                                            fixture.FlowRateLitersPerMinute.Value;
                        }
                        else
                        {
                            diagnostics.Add(CreateWarning(
                                "AE-DHW-FIXTURE-USE-INCOMPLETE",
                                $"Fixture '{fixture.FixtureId}' has incomplete data and was skipped."));
                            continue;
                        }

                        dailyVolume += fixtureVolume;
                        diagnostics.Add(CreateInfo(
                            "AE-DHW-FIXTURE-VOLUME-CALCULATED",
                            $"Fixture '{fixture.FixtureId}' contributed {fixtureVolume:F3} liters/day."));
                    }

                    break;
                }

            case DomesticHotWaterDemandBasis.CustomDailyVolume:
                dailyVolume = Math.Max(0.0, input.CustomDailyVolumeLiters.GetValueOrDefault());
                break;

            case DomesticHotWaterDemandBasis.CustomHourlyVolume:
                if (input.CustomHourlyVolumeLiters is { Count: 8760 })
                {
                    customHourly = input.CustomHourlyVolumeLiters.ToArray();
                    usesCustomHourly = true;
                    dailyVolume = customHourly.Sum() / 365.0;
                }
                break;

            case DomesticHotWaterDemandBasis.ScheduledEnergy:
                if (input.CustomHourlyUsefulEnergyKWh is { Count: 8760 } energy8760)
                {
                    scheduledEnergy = energy8760.ToArray();
                    usesScheduledEnergy = true;
                    dailyVolume = 0.0;
                }
                else if (input.CustomHourlyUsefulEnergyKWh is { Count: 12 } energy12)
                {
                    var monthlyToHourly = ExpandMonthlyToHourly(energy12);
                    scheduledEnergy = monthlyToHourly;
                    usesScheduledEnergy = true;
                    dailyVolume = 0.0;
                }
                else
                {
                    diagnostics.Add(CreateWarning(
                        "AE-DHW-SCHEDULED-ENERGY-INVALID",
                        "Scheduled energy basis requires 8760 hourly or 12 monthly useful-energy values."));
                }
                break;

            case DomesticHotWaterDemandBasis.Other:
            case DomesticHotWaterDemandBasis.Unknown:
            default:
                diagnostics.Add(CreateWarning(
                    "AE-DHW-DEMAND-BASIS-UNSUPPORTED",
                    $"Demand basis '{input.DemandBasis}' is unsupported in this deterministic lane."));
                dailyVolume = 0.0;
                break;
        }

        diagnostics.Add(CreateInfo(
            "AE-DHW-DAILY-VOLUME-CALCULATED",
            $"Daily DHW volume calculated as {dailyVolume:F3} liters/day."));

        return new DomesticHotWaterDemandBasisResult(
            DailyVolumeLiters: dailyVolume,
            CustomHourlyVolumeLiters8760: customHourly,
            UsesCustomHourlyVolume: usesCustomHourly,
            Diagnostics: diagnostics,
            ScheduledUsefulEnergyKWh: scheduledEnergy,
            UsesScheduledUsefulEnergy: usesScheduledEnergy);
    }

    private static Iso12831DomesticHotWaterUsageCategory MapUseCategory(DomesticHotWaterUseCategory category) =>
        category switch
        {
            DomesticHotWaterUseCategory.Residential => Iso12831DomesticHotWaterUsageCategory.ResidentialApartment,
            DomesticHotWaterUseCategory.Office => Iso12831DomesticHotWaterUsageCategory.Office,
            DomesticHotWaterUseCategory.Hotel => Iso12831DomesticHotWaterUsageCategory.Hotel,
            DomesticHotWaterUseCategory.School => Iso12831DomesticHotWaterUsageCategory.School,
            DomesticHotWaterUseCategory.Healthcare => Iso12831DomesticHotWaterUsageCategory.Healthcare,
            DomesticHotWaterUseCategory.Restaurant => Iso12831DomesticHotWaterUsageCategory.Restaurant,
            _ => Iso12831DomesticHotWaterUsageCategory.Custom
        };

    private static IReadOnlyList<double> ExpandMonthlyToHourly(IReadOnlyList<double> monthlyValues)
    {
        int[] daysPerMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
        var hourly = new double[8760];
        var offset = 0;
        for (var month = 0; month < 12; month++)
        {
            var hours = daysPerMonth[month] * 24;
            var hourlyValue = Math.Max(0.0, monthlyValues[month]) / hours;
            for (var index = 0; index < hours; index++)
                hourly[offset + index] = hourlyValue;

            offset += hours;
        }

        return hourly;
    }

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        DomesticHotWaterDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            StandardCalculationStage.Aggregation,
            "DomesticHotWaterDemandBasisCalculator");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        DomesticHotWaterDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.Aggregation,
            "DomesticHotWaterDemandBasisCalculator");
}
