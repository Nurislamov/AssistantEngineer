using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

public sealed class DomesticHotWaterDemandService
{
    private static readonly int[] DaysPerMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
    private static readonly double[] DefaultWeekdayDrawProfile =
    [
        0.01, 0.005, 0.005, 0.005, 0.01, 0.05,
        0.12, 0.11, 0.06, 0.03, 0.02, 0.025,
        0.03, 0.025, 0.02, 0.025, 0.04, 0.08,
        0.12, 0.11, 0.07, 0.035, 0.02, 0.01
    ];
    private static readonly double[] DefaultWeekendDrawProfile =
    [
        0.01, 0.005, 0.005, 0.005, 0.01, 0.025,
        0.06, 0.09, 0.1, 0.075, 0.045, 0.035,
        0.035, 0.03, 0.03, 0.035, 0.045, 0.07,
        0.11, 0.115, 0.075, 0.045, 0.025, 0.015
    ];

    public Result<DomesticHotWaterDemandResult> Calculate(DomesticHotWaterDemandRequest request)
    {
        if (request.PeopleCount <= 0)
            return Result<DomesticHotWaterDemandResult>.Validation("People count must be positive.");

        if (request.LitersPerPersonDay <= 0)
            return Result<DomesticHotWaterDemandResult>.Validation("DHW liters per person per day must be positive.");

        if (request.HotWaterTemperatureC <= request.ColdWaterTemperatureC)
            return Result<DomesticHotWaterDemandResult>.Validation("Hot water temperature must be greater than cold water temperature.");

        if (request.DistributionLossFactor is < 0 or > 1)
            return Result<DomesticHotWaterDemandResult>.Validation("Distribution loss factor must be between 0 and 1.");

        if (request.StorageLossKWhPerDay < 0 || request.CirculationLossKWhPerDay < 0)
            return Result<DomesticHotWaterDemandResult>.Validation("DHW storage and circulation losses cannot be negative.");

        var weekdayProfile = request.WeekdayDrawProfile ?? DefaultWeekdayDrawProfile;
        var weekendProfile = request.WeekendDrawProfile ?? DefaultWeekendDrawProfile;
        var weekdayValidation = ValidateDrawProfile(weekdayProfile, "Weekday DHW draw profile");
        if (weekdayValidation.IsFailure)
            return Result<DomesticHotWaterDemandResult>.Failure(weekdayValidation);
        var weekendValidation = ValidateDrawProfile(weekendProfile, "Weekend DHW draw profile");
        if (weekendValidation.IsFailure)
            return Result<DomesticHotWaterDemandResult>.Failure(weekendValidation);

        const double waterSpecificHeatWhPerLiterK = 1.163;
        var dailyVolume = request.PeopleCount * request.LitersPerPersonDay;
        var dailyDrawEnergyKWh = dailyVolume *
            waterSpecificHeatWhPerLiterK *
            (request.HotWaterTemperatureC - request.ColdWaterTemperatureC) /
            1000.0;
        var dailyEnergyKWh = dailyDrawEnergyKWh * (1 + request.DistributionLossFactor) +
            request.StorageLossKWhPerDay +
            request.CirculationLossKWhPerDay;
        var monthly = DaysPerMonth
            .Select((days, index) => new DomesticHotWaterMonthlyDemand(
                Month: index + 1,
                VolumeLiters: Round(dailyVolume * days),
                EnergyKWh: Round(dailyEnergyKWh * days)))
            .ToArray();
        var hourly = request.IncludeHourlyProfile
            ? CreateHourlyDemand(
                request,
                weekdayProfile,
                weekendProfile,
                dailyVolume,
                waterSpecificHeatWhPerLiterK)
            : [];

        return Result<DomesticHotWaterDemandResult>.Success(new DomesticHotWaterDemandResult(
            DailyVolumeLiters: Round(dailyVolume),
            DailyEnergyKWh: Round(dailyEnergyKWh),
            MonthlyDemand: monthly,
            HourlyDemand: hourly,
            AnnualVolumeLiters: Round(monthly.Sum(month => month.VolumeLiters)),
            AnnualEnergyKWh: Round(monthly.Sum(month => month.EnergyKWh))));
    }

    private static IReadOnlyList<DomesticHotWaterHourlyDemand> CreateHourlyDemand(
        DomesticHotWaterDemandRequest request,
        IReadOnlyList<double> weekdayProfile,
        IReadOnlyList<double> weekendProfile,
        double dailyVolumeLiters,
        double waterSpecificHeatWhPerLiterK)
    {
        var hourly = new List<DomesticHotWaterHourlyDemand>(8760);
        var start = new DateTime(request.Year, 1, 1);
        var weekdaySum = weekdayProfile.Sum();
        var weekendSum = weekendProfile.Sum();

        for (var hourOfYear = 0; hourOfYear < 8760; hourOfYear++)
        {
            var timestamp = start.AddHours(hourOfYear);
            var date = DateOnly.FromDateTime(timestamp);
            var isWeekend = timestamp.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ||
                request.HolidayDates.Contains(date);
            var profile = isWeekend ? weekendProfile : weekdayProfile;
            var profileSum = isWeekend ? weekendSum : weekdaySum;
            var volume = dailyVolumeLiters * profile[timestamp.Hour] / profileSum;
            var drawEnergy = volume *
                waterSpecificHeatWhPerLiterK *
                (request.HotWaterTemperatureC - request.ColdWaterTemperatureC) /
                1000.0;
            var hourlyLosses = (request.StorageLossKWhPerDay + request.CirculationLossKWhPerDay) / 24.0;
            var energy = drawEnergy * (1 + request.DistributionLossFactor) + hourlyLosses;
            hourly.Add(new DomesticHotWaterHourlyDemand(
                hourOfYear,
                GetMonth(hourOfYear),
                RoundHourly(volume),
                RoundHourly(energy)));
        }

        return hourly;
    }

    private static Result ValidateDrawProfile(IReadOnlyList<double> profile, string name)
    {
        if (profile.Count != 24)
            return Result.Validation($"{name} must contain exactly 24 values.");

        if (profile.Any(value => value < 0 || !double.IsFinite(value)))
            return Result.Validation($"{name} values must be finite and non-negative.");

        if (profile.Sum() <= 0)
            return Result.Validation($"{name} must contain at least one positive value.");

        return Result.Success();
    }

    private static int GetMonth(int hourOfYear)
    {
        var dayOfYear = hourOfYear / 24;
        var accumulatedDays = 0;
        for (var month = 1; month <= DaysPerMonth.Length; month++)
        {
            accumulatedDays += DaysPerMonth[month - 1];
            if (dayOfYear < accumulatedDays)
                return month;
        }

        return 12;
    }

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static double RoundHourly(double value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);
}