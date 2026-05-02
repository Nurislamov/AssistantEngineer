using AssistantEngineer.Modules.Buildings.Domain.Enums;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;

public static class WeatherSolarSurfaceCodes
{
    public const string Horizontal = "horizontal";

    public const string North = "north";
    public const string NorthEast = "north-east";
    public const string East = "east";
    public const string SouthEast = "south-east";
    public const string South = "south";
    public const string SouthWest = "south-west";
    public const string West = "west";
    public const string NorthWest = "north-west";

    public static string FromCardinalDirection(
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