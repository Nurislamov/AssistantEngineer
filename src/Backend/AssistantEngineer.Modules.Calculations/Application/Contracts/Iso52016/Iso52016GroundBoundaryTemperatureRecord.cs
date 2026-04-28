namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016GroundBoundaryTemperatureRecord(
    int HourOfYear,
    DateTimeOffset Timestamp,
    double GroundTemperatureC);