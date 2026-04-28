namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;

public sealed record SolarPositionRequest(
    DateTimeOffset Timestamp,
    double LatitudeDegrees,
    double LongitudeDegrees);