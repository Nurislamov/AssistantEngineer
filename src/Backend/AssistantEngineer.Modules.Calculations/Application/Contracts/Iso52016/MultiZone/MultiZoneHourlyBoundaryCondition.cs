namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

public sealed record MultiZoneHourlyBoundaryCondition(
    string BoundaryId,
    IReadOnlyList<double> TemperatureProfileCelsius);
