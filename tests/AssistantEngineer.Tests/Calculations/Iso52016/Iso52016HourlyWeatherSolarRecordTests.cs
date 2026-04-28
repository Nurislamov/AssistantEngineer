using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016HourlyWeatherSolarRecordTests
{
    [Fact]
    public void GetSurface_ByStringCode_ReturnsSurface()
    {
        var hour = CreateHour();

        var south = hour.GetSurface(
            WeatherSolarSurfaceCodes.South);

        Assert.Equal(
            WeatherSolarSurfaceCodes.South,
            south.SurfaceCode);
    }

    [Theory]
    [InlineData(CardinalDirection.North, WeatherSolarSurfaceCodes.North)]
    [InlineData(CardinalDirection.NorthEast, WeatherSolarSurfaceCodes.NorthEast)]
    [InlineData(CardinalDirection.East, WeatherSolarSurfaceCodes.East)]
    [InlineData(CardinalDirection.SouthEast, WeatherSolarSurfaceCodes.SouthEast)]
    [InlineData(CardinalDirection.South, WeatherSolarSurfaceCodes.South)]
    [InlineData(CardinalDirection.SouthWest, WeatherSolarSurfaceCodes.SouthWest)]
    [InlineData(CardinalDirection.West, WeatherSolarSurfaceCodes.West)]
    [InlineData(CardinalDirection.NorthWest, WeatherSolarSurfaceCodes.NorthWest)]
    public void GetSurface_ByCardinalDirection_ReturnsMappedSurface(
        CardinalDirection direction,
        string expectedSurfaceCode)
    {
        var hour = CreateHour();

        var surface = hour.GetSurface(
            direction);

        Assert.Equal(
            expectedSurfaceCode,
            surface.SurfaceCode);
    }

    [Fact]
    public void GetSurface_ThrowsWhenSurfaceCodeDoesNotExist()
    {
        var hour = CreateHour();

        Assert.Throws<KeyNotFoundException>(() =>
            hour.GetSurface("unknown-surface"));
    }

    private static Iso52016HourlyWeatherSolarRecord CreateHour()
    {
        var surfaces = WeatherSolarSurface.DefaultSurfaces
            .Where(surface => surface.Code != WeatherSolarSurfaceCodes.Horizontal)
            .Select(surface => new Iso52016SurfaceWeatherSolarRecord(
                SurfaceCode: surface.Code,
                Orientation: surface.Orientation,
                IncidenceAngleDegrees: 45,
                BeamIrradianceWm2: 100,
                DiffuseSkyIrradianceWm2: 50,
                GroundReflectedIrradianceWm2: 10,
                TotalIrradianceWm2: 160))
            .ToArray();

        return new Iso52016HourlyWeatherSolarRecord(
            HourOfYear: 12,
            Month: 1,
            Day: 1,
            Hour: 12,
            OutdoorTemperatureC: 10,
            GroundBoundaryTemperatureC: 12,
            SolarAltitudeDegrees: 30,
            SolarAzimuthDegrees: 180,
            DirectNormalIrradianceWm2: 600,
            DiffuseHorizontalIrradianceWm2: 100,
            GlobalHorizontalIrradianceWm2: 400,
            SurfaceIrradiance: surfaces);
    }
}