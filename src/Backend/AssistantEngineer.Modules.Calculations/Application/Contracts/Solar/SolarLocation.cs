namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;

/// <summary>
/// Geographic and civil-time location used by ISO 52010 solar calculations.
/// Longitude follows the common convention: east positive, west negative.
/// </summary>
public sealed record SolarLocation(
    double LatitudeDegrees,
    double LongitudeDegrees,
    TimeSpan UtcOffset);
