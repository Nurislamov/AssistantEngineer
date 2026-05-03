namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;

public sealed record SolarPositionRequest(
    DateTimeOffset Timestamp,
    double LatitudeDegrees,
    double LongitudeDegrees)
{
    public SolarPositionRequest(
        DateTimeOffset Timestamp,
        SolarLocation Location)
        : this(
            Timestamp,
            Location.LatitudeDegrees,
            Location.LongitudeDegrees)
    {
        this.Location = Location;
    }

    public SolarLocation Location { get; init; } =
        new(
            LatitudeDegrees,
            LongitudeDegrees,
            Timestamp.Offset);
}
