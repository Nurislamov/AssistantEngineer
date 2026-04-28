using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;

public sealed record WeatherSolarSurface(
    string Code,
    SurfaceOrientation Orientation)
{
    public static WeatherSolarSurface Horizontal { get; } =
        new(
            WeatherSolarSurfaceCodes.Horizontal,
            SurfaceOrientation.Horizontal);

    public static WeatherSolarSurface North { get; } =
        new(
            WeatherSolarSurfaceCodes.North,
            SurfaceOrientation.NorthVertical);

    public static WeatherSolarSurface NorthEast { get; } =
        new(
            WeatherSolarSurfaceCodes.NorthEast,
            new SurfaceOrientation(
                TiltDegrees: 90,
                AzimuthDegrees: 45));

    public static WeatherSolarSurface East { get; } =
        new(
            WeatherSolarSurfaceCodes.East,
            SurfaceOrientation.EastVertical);

    public static WeatherSolarSurface SouthEast { get; } =
        new(
            WeatherSolarSurfaceCodes.SouthEast,
            new SurfaceOrientation(
                TiltDegrees: 90,
                AzimuthDegrees: 135));

    public static WeatherSolarSurface South { get; } =
        new(
            WeatherSolarSurfaceCodes.South,
            SurfaceOrientation.SouthVertical);

    public static WeatherSolarSurface SouthWest { get; } =
        new(
            WeatherSolarSurfaceCodes.SouthWest,
            new SurfaceOrientation(
                TiltDegrees: 90,
                AzimuthDegrees: 225));

    public static WeatherSolarSurface West { get; } =
        new(
            WeatherSolarSurfaceCodes.West,
            SurfaceOrientation.WestVertical);

    public static WeatherSolarSurface NorthWest { get; } =
        new(
            WeatherSolarSurfaceCodes.NorthWest,
            new SurfaceOrientation(
                TiltDegrees: 90,
                AzimuthDegrees: 315));

    public static IReadOnlyList<WeatherSolarSurface> DefaultSurfaces { get; } =
    [
        Horizontal,
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    ];

    public static WeatherSolarSurface FromCardinalDirection(
        CardinalDirection direction) =>
        direction switch
        {
            CardinalDirection.North => North,
            CardinalDirection.NorthEast => NorthEast,
            CardinalDirection.East => East,
            CardinalDirection.SouthEast => SouthEast,
            CardinalDirection.South => South,
            CardinalDirection.SouthWest => SouthWest,
            CardinalDirection.West => West,
            CardinalDirection.NorthWest => NorthWest,
            _ => throw new ArgumentOutOfRangeException(
                nameof(direction),
                direction,
                "Unsupported cardinal direction.")
        };
}