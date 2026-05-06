namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground.Iso13370;

public sealed record Iso13370MonthlyGroundBoundaryRecord(
    int Month,
    double GroundTemperatureC,
    double OutdoorTemperatureC,
    double BoundaryTemperatureC);
