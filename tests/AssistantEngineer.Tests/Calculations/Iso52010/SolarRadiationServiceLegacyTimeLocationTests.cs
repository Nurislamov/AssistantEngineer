using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Solar;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests.Calculations.Iso52010;

public class SolarRadiationServiceLegacyTimeLocationTests
{
    [Fact]
    public void CalculateVerticalSurfaceRadiation_UsesProvidedLongitudeTimeZoneAndYear()
    {
        var solarPositionCalculator = new CapturingSolarPositionCalculator();
        var surfaceIrradianceCalculator = new ConstantSurfaceIrradianceCalculator();
        var service = new SolarRadiationService(
            solarPositionCalculator,
            surfaceIrradianceCalculator);

        var weather = CreateWeather(hourOfYear: 100);

        var result = service.CalculateVerticalSurfaceRadiation(
            weather,
            CardinalDirection.South,
            latitude: 41.3,
            longitudeDegrees: 69.2,
            timeZoneOffset: TimeSpan.FromHours(5),
            year: 2027,
            dayOfYear: 101,
            hour: 13);

        Assert.Equal(123.0, result, precision: 6);
        Assert.NotNull(solarPositionCalculator.LastRequest);

        Assert.Equal(41.3, solarPositionCalculator.LastRequest!.LatitudeDegrees, precision: 6);
        Assert.Equal(69.2, solarPositionCalculator.LastRequest.LongitudeDegrees, precision: 6);
        Assert.Equal(TimeSpan.FromHours(5), solarPositionCalculator.LastRequest.Timestamp.Offset);
        Assert.Equal(2027, solarPositionCalculator.LastRequest.Timestamp.Year);
        Assert.Equal(101, solarPositionCalculator.LastRequest.Timestamp.DayOfYear);
        Assert.Equal(13, solarPositionCalculator.LastRequest.Timestamp.Hour);
    }

    [Fact]
    public void CompatibilityOverloadKeepsOldSignatureButDelegatesThroughExplicitFallbackConstants()
    {
        var solarPositionCalculator = new CapturingSolarPositionCalculator();
        var surfaceIrradianceCalculator = new ConstantSurfaceIrradianceCalculator();
        var service = new SolarRadiationService(
            solarPositionCalculator,
            surfaceIrradianceCalculator);

        var weather = CreateWeather(hourOfYear: 10);

        _ = service.CalculateVerticalSurfaceRadiation(
            weather,
            CardinalDirection.South,
            latitude: 41.3,
            dayOfYear: 10,
            hour: 11);

        Assert.NotNull(solarPositionCalculator.LastRequest);

        Assert.Equal(2026, solarPositionCalculator.LastRequest!.Timestamp.Year);
        Assert.Equal(TimeSpan.Zero, solarPositionCalculator.LastRequest.Timestamp.Offset);
        Assert.Equal(0.0, solarPositionCalculator.LastRequest.LongitudeDegrees, precision: 6);
    }

    [Fact]
    public void Iso52016LegacyFallbackCallPassesConfiguredLocationAndTimezone()
    {
        var heatBalanceSource = ReadRepoFile(
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Services",
            "Iso52016",
            "Iso52016HourlyHeatBalanceCalculator.cs");

        Assert.Contains("_options.LongitudeDegrees", heatBalanceSource, StringComparison.Ordinal);
        Assert.Contains("TimeSpan.FromHours(_options.TimeZoneOffsetHours)", heatBalanceSource, StringComparison.Ordinal);
        Assert.Contains("_options.DefaultWeatherYear", heatBalanceSource, StringComparison.Ordinal);

        var serviceSource = ReadRepoFile(
            "src",
            "Backend",
            "AssistantEngineer.Modules.Calculations",
            "Application",
            "Services",
            "Iso52016",
            "SolarRadiationService.cs");

        Assert.Contains("LongitudeDegrees: longitudeDegrees", serviceSource, StringComparison.Ordinal);
        Assert.Contains("offset: timeZoneOffset", serviceSource, StringComparison.Ordinal);
        Assert.Contains("year: normalizedYear", serviceSource, StringComparison.Ordinal);
    }

    private static AnnualHourlyData CreateWeather(
        int hourOfYear)
    {
        var climateZone = ClimateZone.Create(
            "Legacy solar service test climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-10).Value).Value;

        var annualData = AnnualClimateData.Create(
            climateZone,
            2027).Value;

        return AnnualHourlyData.Create(
            annualData,
            hourOfYear,
            dryBulbTemperature: 10,
            directSolarRadiation: 600,
            diffuseSolarRadiation: 100,
            windSpeedMPerS: 2).Value;
    }

    private static string ReadRepoFile(params string[] parts)
    {
        var path = Path.Combine(
            parts.Prepend(TestPaths.RepoRoot).ToArray());

        Assert.True(
            File.Exists(path),
            $"Expected file does not exist: {path}");

        return File.ReadAllText(path);
    }

    private sealed class CapturingSolarPositionCalculator : ISolarPositionCalculator
    {
        public SolarPositionRequest? LastRequest { get; private set; }

        public SolarPositionResult Calculate(
            SolarPositionRequest request)
        {
            LastRequest = request;

            return new SolarPositionResult(
                DayOfYear: request.Timestamp.DayOfYear,
                SolarDeclinationDegrees: 0,
                EquationOfTimeMinutes: 0,
                HourAngleDegrees: 0,
                SolarAltitudeDegrees: 45,
                SolarAzimuthDegrees: 180,
                ZenithAngleDegrees: 45,
                RelativeAirMass: 1);
        }
    }

    private sealed class ConstantSurfaceIrradianceCalculator : ISurfaceIrradianceCalculator
    {
        public SurfaceIrradianceResult Calculate(
            SurfaceIrradianceRequest request) =>
            new(
                IncidenceAngleDegrees: 30,
                BeamIrradianceWm2: 100,
                DiffuseSkyIrradianceWm2: 20,
                GroundReflectedIrradianceWm2: 3,
                TotalIrradianceWm2: 123);
    }
}
