using AssistantEngineer.Modules.Calculations.Application.Abstractions.Solar;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.WeatherSolar;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Weather;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.WeatherSolar;

public sealed class AnnualWeatherSolarProfileBuilder : IAnnualWeatherSolarProfileBuilder
{
    private readonly ISolarPositionCalculator _solarPositionCalculator;
    private readonly ISurfaceIrradianceCalculator _surfaceIrradianceCalculator;

    public AnnualWeatherSolarProfileBuilder(
        ISolarPositionCalculator solarPositionCalculator,
        ISurfaceIrradianceCalculator surfaceIrradianceCalculator)
    {
        _solarPositionCalculator = solarPositionCalculator;
        _surfaceIrradianceCalculator = surfaceIrradianceCalculator;
    }

    public Result<AnnualWeatherSolarProfile> Build(
        AnnualWeatherSolarProfileRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<AnnualWeatherSolarProfile>.Failure(validation);

        var surfaces = request.Surfaces is { Count: > 0 }
            ? request.Surfaces
            : WeatherSolarSurface.DefaultSurfaces;

        var hours = request.WeatherDataSet.Hours
            .Select(hour => BuildHour(
                request,
                surfaces,
                hour))
            .ToArray();

        var profile = new AnnualWeatherSolarProfile(
            Year: request.WeatherDataSet.Year,
            TimeZoneOffset: request.WeatherDataSet.TimeZoneOffset,
            LatitudeDegrees: request.LatitudeDegrees,
            LongitudeDegrees: request.LongitudeDegrees,
            Surfaces: surfaces,
            Hours: hours);

        return Result<AnnualWeatherSolarProfile>.Success(profile);
    }

    private HourlyWeatherSolarRecord BuildHour(
        AnnualWeatherSolarProfileRequest request,
        IReadOnlyList<WeatherSolarSurface> surfaces,
        HourlyWeatherRecord weather)
    {
        var solarPosition = _solarPositionCalculator.Calculate(
            new SolarPositionRequest(
                Timestamp: weather.Timestamp,
                LatitudeDegrees: request.LatitudeDegrees,
                LongitudeDegrees: request.LongitudeDegrees));

        var globalHorizontalIrradiance = GetGlobalHorizontalIrradiance(
            weather,
            solarPosition);

        var surfaceRecords = surfaces
            .Select(surface => BuildSurfaceRecord(
                request,
                weather,
                solarPosition,
                globalHorizontalIrradiance,
                surface))
            .ToArray();

        return new HourlyWeatherSolarRecord(
            HourOfYear: weather.HourOfYear,
            Weather: weather,
            SolarPosition: solarPosition,
            SurfaceIrradiance: surfaceRecords);
    }

    private HourlySurfaceIrradianceRecord BuildSurfaceRecord(
        AnnualWeatherSolarProfileRequest request,
        HourlyWeatherRecord weather,
        SolarPositionResult solarPosition,
        double globalHorizontalIrradianceWm2,
        WeatherSolarSurface surface)
    {
        var irradiance = _surfaceIrradianceCalculator.Calculate(
            new SurfaceIrradianceRequest(
                SolarPosition: solarPosition,
                Surface: surface.Orientation,
                DirectNormalIrradianceWm2: weather.DirectNormalIrradianceWm2,
                DiffuseHorizontalIrradianceWm2: weather.DiffuseHorizontalIrradianceWm2,
                GlobalHorizontalIrradianceWm2: globalHorizontalIrradianceWm2,
                GroundReflectance: request.GroundReflectance,
                DiagnosticsContext: $"Hour {weather.HourOfYear} {surface.Code} surface"));

        return new HourlySurfaceIrradianceRecord(
            SurfaceCode: surface.Code,
            Orientation: surface.Orientation,
            Irradiance: irradiance);
    }

    private static double GetGlobalHorizontalIrradiance(
        HourlyWeatherRecord weather,
        SolarPositionResult solarPosition)
    {
        if (weather.GlobalHorizontalIrradianceWm2.HasValue)
            return weather.GlobalHorizontalIrradianceWm2.Value;

        if (solarPosition.SolarAltitudeDegrees <= 0)
            return 0;

        var sunAltitudeRadians =
            solarPosition.SolarAltitudeDegrees * Math.PI / 180.0;

        var projectedDirect =
            weather.DirectNormalIrradianceWm2 *
            Math.Sin(sunAltitudeRadians);

        return Math.Max(
            0.0,
            projectedDirect + weather.DiffuseHorizontalIrradianceWm2);
    }

    private static Result Validate(
        AnnualWeatherSolarProfileRequest request)
    {
        if (request.WeatherDataSet is null)
            return Result.Validation("Weather dataset is required.");

        if (!request.WeatherDataSet.IsCompleteYear())
            return Result.Validation("Weather dataset must contain a complete year.");

        if (request.LatitudeDegrees is < -90.0 or > 90.0)
            return Result.Validation("Latitude must be between -90 and 90 degrees.");

        if (request.LongitudeDegrees is < -180.0 or > 180.0)
            return Result.Validation("Longitude must be between -180 and 180 degrees.");

        if (request.GroundReflectance is < 0.0 or > 1.0)
            return Result.Validation("Ground reflectance must be between 0 and 1.");

        var surfaces = request.Surfaces is { Count: > 0 }
            ? request.Surfaces
            : WeatherSolarSurface.DefaultSurfaces;

        var duplicateSurfaceCodes = surfaces
            .GroupBy(surface => surface.Code, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (duplicateSurfaceCodes.Length > 0)
        {
            return Result.Conflict(
                $"Surface codes must be unique: {string.Join(", ", duplicateSurfaceCodes)}.");
        }

        var invalidSurfaceCodes = surfaces
            .Where(surface => string.IsNullOrWhiteSpace(surface.Code))
            .ToArray();

        if (invalidSurfaceCodes.Length > 0)
            return Result.Validation("Surface code is required.");

        return Result.Success();
    }
}
