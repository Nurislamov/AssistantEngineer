using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;

namespace AssistantEngineer.Tests.Calculations.WeatherSolar;

public class WeatherSolarSurfaceTests
{
    [Fact]
    public void DefaultSurfacesContainHorizontalAndAllCardinalDirections()
    {
        var codes = WeatherSolarSurface.DefaultSurfaces
            .Select(surface => surface.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(WeatherSolarSurfaceCodes.Horizontal, codes);
        Assert.Contains(WeatherSolarSurfaceCodes.North, codes);
        Assert.Contains(WeatherSolarSurfaceCodes.NorthEast, codes);
        Assert.Contains(WeatherSolarSurfaceCodes.East, codes);
        Assert.Contains(WeatherSolarSurfaceCodes.SouthEast, codes);
        Assert.Contains(WeatherSolarSurfaceCodes.South, codes);
        Assert.Contains(WeatherSolarSurfaceCodes.SouthWest, codes);
        Assert.Contains(WeatherSolarSurfaceCodes.West, codes);
        Assert.Contains(WeatherSolarSurfaceCodes.NorthWest, codes);
    }

    [Fact]
    public void DefaultSurfaceCodesAreUnique()
    {
        var duplicates = WeatherSolarSurface.DefaultSurfaces
            .GroupBy(surface => surface.Code, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.True(
            duplicates.Length == 0,
            $"Default weather-solar surface codes must be unique: {string.Join(", ", duplicates)}.");
    }

    [Theory]
    [InlineData(CardinalDirection.North, WeatherSolarSurfaceCodes.North, 0)]
    [InlineData(CardinalDirection.NorthEast, WeatherSolarSurfaceCodes.NorthEast, 45)]
    [InlineData(CardinalDirection.East, WeatherSolarSurfaceCodes.East, 90)]
    [InlineData(CardinalDirection.SouthEast, WeatherSolarSurfaceCodes.SouthEast, 135)]
    [InlineData(CardinalDirection.South, WeatherSolarSurfaceCodes.South, 180)]
    [InlineData(CardinalDirection.SouthWest, WeatherSolarSurfaceCodes.SouthWest, 225)]
    [InlineData(CardinalDirection.West, WeatherSolarSurfaceCodes.West, 270)]
    [InlineData(CardinalDirection.NorthWest, WeatherSolarSurfaceCodes.NorthWest, 315)]
    public void FromCardinalDirection_ReturnsExpectedSurface(
        CardinalDirection direction,
        string expectedCode,
        double expectedAzimuthDegrees)
    {
        var surface = WeatherSolarSurface.FromCardinalDirection(
            direction);

        Assert.Equal(
            expectedCode,
            surface.Code);

        Assert.Equal(
            90,
            surface.Orientation.TiltDegrees);

        Assert.Equal(
            expectedAzimuthDegrees,
            surface.Orientation.AzimuthDegrees);
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
    public void SurfaceCodesFromCardinalDirection_ReturnsExpectedCode(
        CardinalDirection direction,
        string expectedCode)
    {
        var code = WeatherSolarSurfaceCodes.FromCardinalDirection(
            direction);

        Assert.Equal(
            expectedCode,
            code);
    }
}