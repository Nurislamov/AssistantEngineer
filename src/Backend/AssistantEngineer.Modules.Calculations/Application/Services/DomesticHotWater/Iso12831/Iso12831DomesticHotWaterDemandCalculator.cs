using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater.Iso12831;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater.Iso12831;

public sealed class Iso12831DomesticHotWaterDemandCalculator
{
    private static readonly int[] DaysPerMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    private readonly Iso12831DomesticHotWaterReferenceDataProvider _referenceDataProvider;
    private readonly Iso12831DomesticHotWaterDrawProfileProvider _drawProfileProvider;

    public Iso12831DomesticHotWaterDemandCalculator(
        Iso12831DomesticHotWaterReferenceDataProvider referenceDataProvider,
        Iso12831DomesticHotWaterDrawProfileProvider drawProfileProvider)
    {
        _referenceDataProvider = referenceDataProvider;
        _drawProfileProvider = drawProfileProvider;
    }

    public Result<Iso12831DomesticHotWaterResult> Calculate(Iso12831DomesticHotWaterInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.PeopleCount < 0)
            return Result<Iso12831DomesticHotWaterResult>.Validation("PeopleCount cannot be negative.");

        if (input.AreaM2 < 0)
            return Result<Iso12831DomesticHotWaterResult>.Validation("AreaM2 cannot be negative.");

        if (input.UnitsCount < 0)
            return Result<Iso12831DomesticHotWaterResult>.Validation("UnitsCount cannot be negative.");

        if (input.HotWaterTemperatureC <= input.ColdWaterTemperatureC)
            return Result<Iso12831DomesticHotWaterResult>.Validation(
                "HotWaterTemperatureC must be greater than ColdWaterTemperatureC.");

        if (input.DistributionLossFactor is < 0 or > 1)
            return Result<Iso12831DomesticHotWaterResult>.Validation(
                "DistributionLossFactor must be between 0 and 1.");

        if (input.StorageLossKWhPerDay < 0 || input.CirculationLossKWhPerDay < 0)
            return Result<Iso12831DomesticHotWaterResult>.Validation(
                "StorageLossKWhPerDay and CirculationLossKWhPerDay must be non-negative.");

        var resolvedReference = _referenceDataProvider.Resolve(
            input.UsageCategory,
            input.UseTableDrivenReferenceData,
            input.TableDrivenUsageCategory);
        var defaults = resolvedReference.ReferenceDefaults;
        var diagnostics = new List<Iso12831DomesticHotWaterDiagnostics>();
        var assumptions = new List<string>
        {
            "EN12831-3-style standard-based domestic hot water engineering calculator.",
            "Internal deterministic engineering anchors only.",
            "No full ISO 12831-3 compliance claim.",
            "Not a full validation package.",
            "No external validation claim.",
            "Water draw energy uses liters x 1 kg/l x 4.186 kJ/(kg K) x deltaT / 3600.",
            "Monthly and hourly allocation uses a non-leap 365-day calendar."
        };

        if (input.UseTableDrivenReferenceData)
        {
            diagnostics.Add(new Iso12831DomesticHotWaterDiagnostics(
                "Iso12831Dhw.TableDrivenReferenceApplied",
                $"Applied EN12831-3-style table/profile entry '{resolvedReference.ReferenceEntryId}'."));
            assumptions.Add(resolvedReference.TemperatureAssumptions.Notes);
        }

        var equivalentOccupantsUsed = ResolveEquivalentOccupants(input, defaults, diagnostics);
        var referenceDailyVolumeLiters = ResolveReferenceDailyVolume(
            input,
            defaults,
            equivalentOccupantsUsed,
            diagnostics);

        if (referenceDailyVolumeLiters < 0)
            return Result<Iso12831DomesticHotWaterResult>.Validation("Resolved daily volume cannot be negative.");

        if (referenceDailyVolumeLiters == 0)
        {
            diagnostics.Add(new Iso12831DomesticHotWaterDiagnostics(
                "Iso12831Dhw.ZeroDemand",
                "Resolved daily DHW volume is zero."));
        }

        const double waterDensityKgPerLiter = 1.0;
        const double waterSpecificHeatKJPerKgK = 4.186;
        const double kJPerKWh = 3600.0;
        var deltaT = input.HotWaterTemperatureC - input.ColdWaterTemperatureC;
        var dailyDrawEnergyKWh = referenceDailyVolumeLiters *
            waterDensityKgPerLiter *
            waterSpecificHeatKJPerKgK *
            deltaT / kJPerKWh;
        var dailyTotalEnergyKWh = dailyDrawEnergyKWh * (1.0 + input.DistributionLossFactor) +
            input.StorageLossKWhPerDay +
            input.CirculationLossKWhPerDay;

        var monthlyResults = BuildMonthlyResults(referenceDailyVolumeLiters, dailyDrawEnergyKWh, dailyTotalEnergyKWh);

        IReadOnlyList<Iso12831DomesticHotWaterHourlyResult> hourlyResults = [];
        if (input.IncludeHourlyProfile)
        {
            var profilesResult = resolvedReference.UsageProfileSet is { } tableProfileSet
                ? _drawProfileProvider.ResolveProfiles(
                    tableProfileSet.DrawProfileTable,
                    input.WeekdayDrawProfile,
                    input.WeekendDrawProfile,
                    input.CustomDrawProfile)
                : _drawProfileProvider.ResolveProfiles(
                    ResolveDrawProfileKind(input, defaults, diagnostics),
                    input.WeekdayDrawProfile,
                    input.WeekendDrawProfile,
                    input.CustomDrawProfile);

            if (profilesResult.IsFailure)
                return Result<Iso12831DomesticHotWaterResult>.Failure(profilesResult);

            hourlyResults = BuildHourlyResults(
                input,
                profilesResult.Value,
                referenceDailyVolumeLiters,
                dailyDrawEnergyKWh,
                input.DistributionLossFactor,
                input.StorageLossKWhPerDay,
                input.CirculationLossKWhPerDay);
        }

        var annualVolumeLiters = monthlyResults.Sum(item => item.VolumeLiters);
        var annualDrawEnergyKWh = monthlyResults.Sum(item => item.DrawEnergyKWh);
        var annualTotalEnergyKWh = monthlyResults.Sum(item => item.TotalEnergyKWh);

        return Result<Iso12831DomesticHotWaterResult>.Success(new Iso12831DomesticHotWaterResult(
            DailyVolumeLiters: Round3(referenceDailyVolumeLiters),
            DailyDrawEnergyKWh: Round3(dailyDrawEnergyKWh),
            DailyTotalEnergyKWh: Round3(dailyTotalEnergyKWh),
            AnnualVolumeLiters: Round3(annualVolumeLiters),
            AnnualDrawEnergyKWh: Round3(annualDrawEnergyKWh),
            AnnualTotalEnergyKWh: Round3(annualTotalEnergyKWh),
            MonthlyResults: monthlyResults,
            HourlyResults: hourlyResults,
            EquivalentOccupantsUsed: Round3(equivalentOccupantsUsed),
            ReferenceDailyVolumeLiters: Round3(referenceDailyVolumeLiters),
            Diagnostics: diagnostics,
            AssumptionsUsed: assumptions));
    }

    private static double ResolveEquivalentOccupants(
        Iso12831DomesticHotWaterInput input,
        Iso12831DomesticHotWaterReferenceDefaults defaults,
        ICollection<Iso12831DomesticHotWaterDiagnostics> diagnostics)
    {
        if (input.EquivalentOccupants > 0)
            return input.EquivalentOccupants;

        var resolved = Math.Max(input.PeopleCount, 0) * defaults.EquivalentOccupantFactor;
        diagnostics.Add(new Iso12831DomesticHotWaterDiagnostics(
            "Iso12831Dhw.EquivalentOccupantsDefaulted",
            $"Equivalent occupants were derived from PeopleCount and category factor ({defaults.EquivalentOccupantFactor:F3})."));
        return resolved;
    }

    private static double ResolveReferenceDailyVolume(
        Iso12831DomesticHotWaterInput input,
        Iso12831DomesticHotWaterReferenceDefaults defaults,
        double equivalentOccupantsUsed,
        ICollection<Iso12831DomesticHotWaterDiagnostics> diagnostics)
    {
        return input.ReferenceMode switch
        {
            Iso12831DomesticHotWaterReferenceMode.PeopleBased => ResolvePeopleBasedVolume(
                input,
                defaults,
                equivalentOccupantsUsed,
                diagnostics),
            Iso12831DomesticHotWaterReferenceMode.AreaBased => ResolveAreaBasedVolume(input, defaults, diagnostics),
            Iso12831DomesticHotWaterReferenceMode.UnitBased => ResolveUnitBasedVolume(input, defaults, diagnostics),
            Iso12831DomesticHotWaterReferenceMode.CustomVolume => ResolveCustomVolume(input),
            _ => ResolvePeopleBasedVolume(input, defaults, equivalentOccupantsUsed, diagnostics)
        };
    }

    private static double ResolvePeopleBasedVolume(
        Iso12831DomesticHotWaterInput input,
        Iso12831DomesticHotWaterReferenceDefaults defaults,
        double equivalentOccupantsUsed,
        ICollection<Iso12831DomesticHotWaterDiagnostics> diagnostics)
    {
        var litersPerPersonDay = input.LitersPerPersonDay > 0
            ? input.LitersPerPersonDay
            : defaults.LitersPerPersonDay;

        if (input.LitersPerPersonDay <= 0)
        {
            diagnostics.Add(new Iso12831DomesticHotWaterDiagnostics(
                "Iso12831Dhw.LitersPerPersonDefaulted",
                $"People-based liters/day was defaulted to {litersPerPersonDay:F3}."));
        }

        return Math.Max(0, equivalentOccupantsUsed) * litersPerPersonDay;
    }

    private static double ResolveAreaBasedVolume(
        Iso12831DomesticHotWaterInput input,
        Iso12831DomesticHotWaterReferenceDefaults defaults,
        ICollection<Iso12831DomesticHotWaterDiagnostics> diagnostics)
    {
        var litersPerM2Day = input.LitersPerM2Day > 0
            ? input.LitersPerM2Day
            : defaults.LitersPerM2Day;

        if (input.LitersPerM2Day <= 0)
        {
            diagnostics.Add(new Iso12831DomesticHotWaterDiagnostics(
                "Iso12831Dhw.LitersPerM2Defaulted",
                $"Area-based liters/day was defaulted to {litersPerM2Day:F3}."));
        }

        return Math.Max(input.AreaM2, 0) * litersPerM2Day;
    }

    private static double ResolveUnitBasedVolume(
        Iso12831DomesticHotWaterInput input,
        Iso12831DomesticHotWaterReferenceDefaults defaults,
        ICollection<Iso12831DomesticHotWaterDiagnostics> diagnostics)
    {
        var litersPerUnitDay = input.LitersPerUnitDay > 0
            ? input.LitersPerUnitDay
            : defaults.LitersPerUnitDay;

        if (input.LitersPerUnitDay <= 0)
        {
            diagnostics.Add(new Iso12831DomesticHotWaterDiagnostics(
                "Iso12831Dhw.LitersPerUnitDefaulted",
                $"Unit-based liters/day was defaulted to {litersPerUnitDay:F3}."));
        }

        return Math.Max(input.UnitsCount, 0) * litersPerUnitDay;
    }

    private static double ResolveCustomVolume(Iso12831DomesticHotWaterInput input)
    {
        if (input.CustomDailyVolumeLiters < 0)
            throw new InvalidOperationException("CustomDailyVolumeLiters cannot be negative.");

        return input.CustomDailyVolumeLiters;
    }

    private static Iso12831DomesticHotWaterDrawProfileKind ResolveDrawProfileKind(
        Iso12831DomesticHotWaterInput input,
        Iso12831DomesticHotWaterReferenceDefaults defaults,
        ICollection<Iso12831DomesticHotWaterDiagnostics> diagnostics)
    {
        if (Enum.IsDefined(typeof(Iso12831DomesticHotWaterDrawProfileKind), input.DrawProfileKind) &&
            input.DrawProfileKind != 0)
        {
            return input.DrawProfileKind;
        }

        diagnostics.Add(new Iso12831DomesticHotWaterDiagnostics(
            "Iso12831Dhw.DrawProfileDefaulted",
            $"Draw profile kind was defaulted to {defaults.DrawProfileKind}."));
        return defaults.DrawProfileKind;
    }

    private static IReadOnlyList<Iso12831DomesticHotWaterMonthlyResult> BuildMonthlyResults(
        double dailyVolumeLiters,
        double dailyDrawEnergyKWh,
        double dailyTotalEnergyKWh)
    {
        return DaysPerMonth
            .Select((days, index) => new Iso12831DomesticHotWaterMonthlyResult(
                Month: index + 1,
                VolumeLiters: Round3(dailyVolumeLiters * days),
                DrawEnergyKWh: Round3(dailyDrawEnergyKWh * days),
                TotalEnergyKWh: Round3(dailyTotalEnergyKWh * days)))
            .ToArray();
    }

    private static IReadOnlyList<Iso12831DomesticHotWaterHourlyResult> BuildHourlyResults(
        Iso12831DomesticHotWaterInput input,
        Iso12831DomesticHotWaterProfiles profiles,
        double dailyVolumeLiters,
        double dailyDrawEnergyKWh,
        double distributionLossFactor,
        double storageLossKWhPerDay,
        double circulationLossKWhPerDay)
    {
        var result = new List<Iso12831DomesticHotWaterHourlyResult>(capacity: 8760);
        var holidayDates = input.HolidayDates ?? new HashSet<DateOnly>();

        var dayIndex = 0;
        for (var month = 1; month <= 12; month++)
        {
            var daysInMonth = DaysPerMonth[month - 1];
            for (var day = 1; day <= daysInMonth; day++)
            {
                var date = new DateOnly(input.Year, month, day);
                var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ||
                    holidayDates.Contains(date);
                var profile = isWeekend ? profiles.WeekendProfile : profiles.WeekdayProfile;

                for (var hour = 0; hour < 24; hour++)
                {
                    var hourOfYear = dayIndex * 24 + hour;
                    var hourlyVolume = dailyVolumeLiters * profile[hour];
                    var hourlyDrawEnergy = dailyDrawEnergyKWh * profile[hour];
                    var hourlyLosses = (storageLossKWhPerDay + circulationLossKWhPerDay) / 24.0;
                    var hourlyTotal = hourlyDrawEnergy * (1.0 + distributionLossFactor) + hourlyLosses;

                    result.Add(new Iso12831DomesticHotWaterHourlyResult(
                        HourOfYear: hourOfYear,
                        Month: month,
                        VolumeLiters: Round4(hourlyVolume),
                        DrawEnergyKWh: Round4(hourlyDrawEnergy),
                        TotalEnergyKWh: Round4(hourlyTotal)));
                }

                dayIndex++;
            }
        }

        return result;
    }

    private static double Round3(double value) =>
        Math.Round(value, 3, MidpointRounding.AwayFromZero);

    private static double Round4(double value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
