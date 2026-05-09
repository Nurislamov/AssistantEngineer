using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater.Iso12831;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater.Iso12831;

public sealed class Iso12831DomesticHotWaterDrawProfileProvider
{
    private static readonly double[] ResidentialWeekday =
    [
        0.01, 0.005, 0.005, 0.005, 0.01, 0.05,
        0.12, 0.11, 0.06, 0.03, 0.02, 0.025,
        0.03, 0.025, 0.02, 0.025, 0.04, 0.08,
        0.12, 0.11, 0.07, 0.035, 0.02, 0.01
    ];

    private static readonly double[] ResidentialWeekend =
    [
        0.01, 0.005, 0.005, 0.005, 0.01, 0.025,
        0.06, 0.09, 0.1, 0.075, 0.045, 0.035,
        0.035, 0.03, 0.03, 0.035, 0.045, 0.07,
        0.11, 0.115, 0.075, 0.045, 0.025, 0.015
    ];

    private static readonly double[] OfficeDaytime =
    [
        0.00, 0.00, 0.00, 0.00, 0.005, 0.02,
        0.06, 0.09, 0.10, 0.11, 0.11, 0.10,
        0.10, 0.10, 0.09, 0.07, 0.05, 0.03,
        0.015, 0.01, 0.005, 0.005, 0.005, 0.005
    ];

    private static readonly double[] HotelMorningEvening =
    [
        0.02, 0.015, 0.01, 0.01, 0.02, 0.06,
        0.10, 0.12, 0.08, 0.04, 0.03, 0.025,
        0.02, 0.02, 0.02, 0.03, 0.05, 0.08,
        0.10, 0.09, 0.07, 0.05, 0.03, 0.02
    ];

    private static readonly double[] SchoolDaytime =
    [
        0.00, 0.00, 0.00, 0.00, 0.005, 0.015,
        0.05, 0.09, 0.11, 0.12, 0.11, 0.10,
        0.10, 0.09, 0.08, 0.06, 0.03, 0.015,
        0.005, 0.005, 0.005, 0.005, 0.005, 0.005
    ];

    private static readonly double[] Flat =
    [
        1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
        1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
        1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
        1.0, 1.0, 1.0, 1.0, 1.0, 1.0
    ];

    public Result<Iso12831DomesticHotWaterProfiles> ResolveProfiles(
        DomesticHotWaterDrawProfileTable drawProfileTable,
        IReadOnlyList<double>? weekdayOverride,
        IReadOnlyList<double>? weekendOverride,
        IReadOnlyList<double>? customDrawProfile)
    {
        ArgumentNullException.ThrowIfNull(drawProfileTable);

        var weekday = weekdayOverride ?? drawProfileTable.WeekdayWeights24;
        var weekend = weekendOverride ?? drawProfileTable.WeekendWeights24;

        var weekdayValidation = Validate(weekday, "Weekday DHW draw profile");
        if (weekdayValidation.IsFailure)
            return Result<Iso12831DomesticHotWaterProfiles>.Failure(weekdayValidation);

        var weekendValidation = Validate(weekend, "Weekend DHW draw profile");
        if (weekendValidation.IsFailure)
            return Result<Iso12831DomesticHotWaterProfiles>.Failure(weekendValidation);

        if (drawProfileTable.DrawProfileKind == Iso12831DomesticHotWaterDrawProfileKind.Custom)
        {
            if (customDrawProfile is null)
                return Result<Iso12831DomesticHotWaterProfiles>.Validation(
                    "Custom draw profile kind requires CustomDrawProfile with 24 values.");

            var customValidation = Validate(customDrawProfile, "Custom DHW draw profile");
            if (customValidation.IsFailure)
                return Result<Iso12831DomesticHotWaterProfiles>.Failure(customValidation);

            var normalizedCustom = Normalize(customDrawProfile);
            return Result<Iso12831DomesticHotWaterProfiles>.Success(
                new Iso12831DomesticHotWaterProfiles(normalizedCustom, normalizedCustom));
        }

        return Result<Iso12831DomesticHotWaterProfiles>.Success(
            new Iso12831DomesticHotWaterProfiles(
                WeekdayProfile: Normalize(weekday),
                WeekendProfile: Normalize(weekend)));
    }

    public Result<Iso12831DomesticHotWaterProfiles> ResolveProfiles(
        Iso12831DomesticHotWaterDrawProfileKind kind,
        IReadOnlyList<double>? weekdayOverride,
        IReadOnlyList<double>? weekendOverride,
        IReadOnlyList<double>? customDrawProfile)
    {
        if (kind == Iso12831DomesticHotWaterDrawProfileKind.Custom)
        {
            if (customDrawProfile is null)
                return Result<Iso12831DomesticHotWaterProfiles>.Validation(
                    "Custom draw profile kind requires CustomDrawProfile with 24 values.");

            var customValidation = Validate(customDrawProfile, "Custom DHW draw profile");
            if (customValidation.IsFailure)
                return Result<Iso12831DomesticHotWaterProfiles>.Failure(customValidation);

            var normalizedCustom = Normalize(customDrawProfile);
            return Result<Iso12831DomesticHotWaterProfiles>.Success(
                new Iso12831DomesticHotWaterProfiles(normalizedCustom, normalizedCustom));
        }

        var (baseWeekday, baseWeekend) = kind switch
        {
            Iso12831DomesticHotWaterDrawProfileKind.ResidentialWeekdayWeekend => (ResidentialWeekday, ResidentialWeekend),
            Iso12831DomesticHotWaterDrawProfileKind.OfficeDaytime => (OfficeDaytime, OfficeDaytime),
            Iso12831DomesticHotWaterDrawProfileKind.HotelMorningEvening => (HotelMorningEvening, HotelMorningEvening),
            Iso12831DomesticHotWaterDrawProfileKind.SchoolDaytime => (SchoolDaytime, SchoolDaytime),
            Iso12831DomesticHotWaterDrawProfileKind.Flat => (Flat, Flat),
            _ => (Flat, Flat)
        };

        var weekday = weekdayOverride ?? baseWeekday;
        var weekend = weekendOverride ?? baseWeekend;

        var weekdayValidation = Validate(weekday, "Weekday DHW draw profile");
        if (weekdayValidation.IsFailure)
            return Result<Iso12831DomesticHotWaterProfiles>.Failure(weekdayValidation);

        var weekendValidation = Validate(weekend, "Weekend DHW draw profile");
        if (weekendValidation.IsFailure)
            return Result<Iso12831DomesticHotWaterProfiles>.Failure(weekendValidation);

        return Result<Iso12831DomesticHotWaterProfiles>.Success(
            new Iso12831DomesticHotWaterProfiles(
                WeekdayProfile: Normalize(weekday),
                WeekendProfile: Normalize(weekend)));
    }

    private static Result Validate(IReadOnlyList<double> profile, string name)
    {
        if (profile.Count != 24)
            return Result.Validation($"{name} must contain exactly 24 values.");

        if (profile.Any(value => !double.IsFinite(value) || value < 0.0))
            return Result.Validation($"{name} values must be finite and non-negative.");

        if (profile.Sum() <= 0.0)
            return Result.Validation($"{name} must contain at least one positive value.");

        return Result.Success();
    }

    private static IReadOnlyList<double> Normalize(IReadOnlyList<double> profile)
    {
        var sum = profile.Sum();
        return profile.Select(value => value / sum).ToArray();
    }
}

public sealed record Iso12831DomesticHotWaterProfiles(
    IReadOnlyList<double> WeekdayProfile,
    IReadOnlyList<double> WeekendProfile);
