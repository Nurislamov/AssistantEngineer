using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class PeriodicIso52016GroundBoundaryTemperatureProvider : ISo52016GroundBoundaryTemperatureProvider
{
    private const double DaysPerYear = 365.0;

    public Result<Iso52016GroundBoundaryTemperatureProfile> BuildProfile(
        Iso52016GroundBoundaryTemperatureRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016GroundBoundaryTemperatureProfile>.Failure(validation);

        var options = request.Options;

        var hours = options.Mode switch
        {
            Iso52016GroundBoundaryTemperatureMode.OutdoorAir =>
                BuildOutdoorAirProfile(request.WeatherSolarProfile),

            Iso52016GroundBoundaryTemperatureMode.Fixed =>
                BuildFixedProfile(
                    request.WeatherSolarProfile,
                    options.FixedGroundTemperatureC!.Value),

            Iso52016GroundBoundaryTemperatureMode.Periodic =>
                BuildPeriodicProfile(
                    request.WeatherSolarProfile,
                    options),

            _ => throw new ArgumentOutOfRangeException(
                nameof(request),
                "Unsupported ground boundary temperature mode.")
        };

        return Result<Iso52016GroundBoundaryTemperatureProfile>.Success(
            new Iso52016GroundBoundaryTemperatureProfile(
                Year: request.WeatherSolarProfile.Year,
                Hours: hours));
    }

    private static IReadOnlyList<Iso52016GroundBoundaryTemperatureRecord> BuildOutdoorAirProfile(
        AnnualWeatherSolarProfile profile) =>
        profile.Hours
            .Select(hour => new Iso52016GroundBoundaryTemperatureRecord(
                HourOfYear: hour.HourOfYear,
                Timestamp: hour.Weather.Timestamp,
                GroundTemperatureC: hour.Weather.DryBulbTemperatureC))
            .ToArray();

    private static IReadOnlyList<Iso52016GroundBoundaryTemperatureRecord> BuildFixedProfile(
        AnnualWeatherSolarProfile profile,
        double fixedGroundTemperatureC) =>
        profile.Hours
            .Select(hour => new Iso52016GroundBoundaryTemperatureRecord(
                HourOfYear: hour.HourOfYear,
                Timestamp: hour.Weather.Timestamp,
                GroundTemperatureC: fixedGroundTemperatureC))
            .ToArray();

    private static IReadOnlyList<Iso52016GroundBoundaryTemperatureRecord> BuildPeriodicProfile(
        AnnualWeatherSolarProfile profile,
        Iso52016GroundBoundaryTemperatureOptions options)
    {
        var meanAnnualTemperature =
            options.MeanAnnualGroundTemperatureC ??
            profile.Hours.Average(hour => hour.Weather.DryBulbTemperatureC);

        var annualAmplitude =
            options.AnnualGroundTemperatureAmplitudeC ??
            CalculateOutdoorAirAmplitude(profile);

        var depth = options.DepthM;
        var thermalDiffusivity = options.ThermalDiffusivityM2PerDay;

        var dampingDepth =
            Math.Sqrt(
                thermalDiffusivity * DaysPerYear / Math.PI);

        var depthRatio =
            depth / dampingDepth;

        var dampedAmplitude =
            annualAmplitude * Math.Exp(-depthRatio);

        return profile.Hours
            .Select(hour =>
            {
                var dayOfYear =
                    hour.Weather.Timestamp.DayOfYear;

                var phase =
                    2.0 *
                    Math.PI *
                    (dayOfYear - options.ColdestGroundDayOfYear) /
                    DaysPerYear -
                    depthRatio;

                var groundTemperature =
                    meanAnnualTemperature -
                    dampedAmplitude * Math.Cos(phase);

                return new Iso52016GroundBoundaryTemperatureRecord(
                    HourOfYear: hour.HourOfYear,
                    Timestamp: hour.Weather.Timestamp,
                    GroundTemperatureC: groundTemperature);
            })
            .ToArray();
    }

    private static double CalculateOutdoorAirAmplitude(
        AnnualWeatherSolarProfile profile)
    {
        var monthlyAverageTemperatures = profile.Hours
            .GroupBy(hour => hour.Weather.Month)
            .Select(group => group.Average(hour => hour.Weather.DryBulbTemperatureC))
            .ToArray();

        if (monthlyAverageTemperatures.Length == 0)
            return 0.0;

        return
            (monthlyAverageTemperatures.Max() -
             monthlyAverageTemperatures.Min()) /
            2.0;
    }

    private static Result Validate(
        Iso52016GroundBoundaryTemperatureRequest request)
    {
        if (request.WeatherSolarProfile is null)
            return Result.Validation("Weather-solar profile is required.");

        if (request.WeatherSolarProfile.HourCount == 0)
            return Result.Validation("Weather-solar profile must contain hourly records.");

        if (request.Options is null)
            return Result.Validation("Ground boundary temperature options are required.");

        if (request.Options.DepthM < 0)
            return Result.Validation("Ground boundary depth must not be negative.");

        if (request.Options.ThermalDiffusivityM2PerDay <= 0)
            return Result.Validation("Ground thermal diffusivity must be greater than zero.");

        if (request.Options.ColdestGroundDayOfYear is < 1 or > 366)
            return Result.Validation("Coldest ground day of year must be between 1 and 366.");

        if (request.Options.Mode == Iso52016GroundBoundaryTemperatureMode.Fixed &&
            !request.Options.FixedGroundTemperatureC.HasValue)
        {
            return Result.Validation(
                "Fixed ground temperature is required when ground boundary mode is Fixed.");
        }

        return Result.Success();
    }
}