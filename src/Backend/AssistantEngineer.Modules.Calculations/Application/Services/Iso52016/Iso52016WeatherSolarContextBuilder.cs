using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Weather;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.WeatherSolar;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Weather;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016WeatherSolarContextBuilder : IIso52016WeatherSolarContextBuilder
{
    private static readonly Iso52016GroundBoundaryTemperatureOptions DefaultGroundBoundaryOptions =
        new(
            Mode: Iso52016GroundBoundaryTemperatureMode.Periodic);

    private readonly IAnnualWeatherDataNormalizer _weatherDataNormalizer;
    private readonly IAnnualWeatherSolarProfileBuilder _weatherSolarProfileBuilder;
    private readonly IIso52016GroundBoundaryTemperatureProvider _groundBoundaryTemperatureProvider;

    public Iso52016WeatherSolarContextBuilder(
        IAnnualWeatherDataNormalizer weatherDataNormalizer,
        IAnnualWeatherSolarProfileBuilder weatherSolarProfileBuilder,
        IIso52016GroundBoundaryTemperatureProvider groundBoundaryTemperatureProvider)
    {
        _weatherDataNormalizer = weatherDataNormalizer;
        _weatherSolarProfileBuilder = weatherSolarProfileBuilder;
        _groundBoundaryTemperatureProvider = groundBoundaryTemperatureProvider;
    }

    public Result<Iso52016WeatherSolarContext> Build(
        Iso52016WeatherSolarContextRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016WeatherSolarContext>.Failure(validation);

        var weatherResult = _weatherDataNormalizer.Normalize(
            new AnnualWeatherNormalizationRequest(
                request.AnnualClimateData,
                request.TimeZoneOffset));

        if (weatherResult.IsFailure)
            return Result<Iso52016WeatherSolarContext>.Failure(weatherResult);

        var weatherSolarResult = _weatherSolarProfileBuilder.Build(
            new AnnualWeatherSolarProfileRequest(
                WeatherDataSet: weatherResult.Value,
                LatitudeDegrees: request.LatitudeDegrees,
                LongitudeDegrees: request.LongitudeDegrees,
                Surfaces: request.Surfaces,
                GroundReflectance: request.GroundReflectance));

        if (weatherSolarResult.IsFailure)
            return Result<Iso52016WeatherSolarContext>.Failure(weatherSolarResult);

        var groundProfileResult = _groundBoundaryTemperatureProvider.BuildProfile(
            new Iso52016GroundBoundaryTemperatureRequest(
                WeatherSolarProfile: weatherSolarResult.Value,
                Options: request.GroundBoundaryTemperature ?? DefaultGroundBoundaryOptions));

        if (groundProfileResult.IsFailure)
            return Result<Iso52016WeatherSolarContext>.Failure(groundProfileResult);

        var context = MapToIso52016Context(
            weatherSolarResult.Value,
            groundProfileResult.Value);

        return Result<Iso52016WeatherSolarContext>.Success(context);
    }

    private static Iso52016WeatherSolarContext MapToIso52016Context(
        AnnualWeatherSolarProfile profile,
        Iso52016GroundBoundaryTemperatureProfile groundProfile)
    {
        var hours = profile.Hours
            .Select(hour => MapHour(
                hour,
                groundProfile.GetHour(hour.HourOfYear)))
            .ToArray();

        return new Iso52016WeatherSolarContext(
            Year: profile.Year,
            TimeZoneOffset: profile.TimeZoneOffset,
            LatitudeDegrees: profile.LatitudeDegrees,
            LongitudeDegrees: profile.LongitudeDegrees,
            Hours: hours);
    }

    private static Iso52016HourlyWeatherSolarRecord MapHour(
        HourlyWeatherSolarRecord source,
        Iso52016GroundBoundaryTemperatureRecord ground)
    {
        var globalHorizontalIrradiance =
            source.Weather.GlobalHorizontalIrradianceWm2 ??
            CalculateGlobalHorizontalIrradiance(source);

        var surfaceRecords = source.SurfaceIrradiance
            .Select(surface => new Iso52016SurfaceWeatherSolarRecord(
                SurfaceCode: surface.SurfaceCode,
                Orientation: surface.Orientation,
                IncidenceAngleDegrees: surface.Irradiance.IncidenceAngleDegrees,
                BeamIrradianceWm2: surface.Irradiance.BeamIrradianceWm2,
                DiffuseSkyIrradianceWm2: surface.Irradiance.DiffuseSkyIrradianceWm2,
                GroundReflectedIrradianceWm2: surface.Irradiance.GroundReflectedIrradianceWm2,
                TotalIrradianceWm2: surface.Irradiance.TotalIrradianceWm2))
            .ToArray();

        return new Iso52016HourlyWeatherSolarRecord(
            HourOfYear: source.HourOfYear,
            Month: source.Weather.Month,
            Day: source.Weather.Day,
            Hour: source.Weather.Hour,
            OutdoorTemperatureC: source.Weather.DryBulbTemperatureC,
            GroundBoundaryTemperatureC: ground.GroundTemperatureC,
            SolarAltitudeDegrees: source.SolarPosition.SolarAltitudeDegrees,
            SolarAzimuthDegrees: source.SolarPosition.SolarAzimuthDegrees,
            DirectNormalIrradianceWm2: source.Weather.DirectNormalIrradianceWm2,
            DiffuseHorizontalIrradianceWm2: source.Weather.DiffuseHorizontalIrradianceWm2,
            GlobalHorizontalIrradianceWm2: globalHorizontalIrradiance,
            SurfaceIrradiance: surfaceRecords);
    }

    private static double CalculateGlobalHorizontalIrradiance(
        HourlyWeatherSolarRecord source)
    {
        if (source.SolarPosition.SolarAltitudeDegrees <= 0)
            return source.Weather.DiffuseHorizontalIrradianceWm2;

        var sunAltitudeRadians =
            source.SolarPosition.SolarAltitudeDegrees *
            Math.PI /
            180.0;

        var projectedDirect =
            source.Weather.DirectNormalIrradianceWm2 *
            Math.Sin(sunAltitudeRadians);

        return Math.Max(
            0.0,
            projectedDirect + source.Weather.DiffuseHorizontalIrradianceWm2);
    }

    private static Result Validate(
        Iso52016WeatherSolarContextRequest request)
    {
        if (request.AnnualClimateData is null)
            return Result.Validation("Annual climate data is required.");

        if (request.LatitudeDegrees is < -90.0 or > 90.0)
            return Result.Validation("Latitude must be between -90 and 90 degrees.");

        if (request.LongitudeDegrees is < -180.0 or > 180.0)
            return Result.Validation("Longitude must be between -180 and 180 degrees.");

        if (request.GroundReflectance is < 0.0 or > 1.0)
            return Result.Validation("Ground reflectance must be between 0 and 1.");

        return Result.Success();
    }
}