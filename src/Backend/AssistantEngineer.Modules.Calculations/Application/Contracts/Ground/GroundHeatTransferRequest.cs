namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

public sealed record GroundHeatTransferRequest(
    GroundBoundaryDefinition Boundary,
    IReadOnlyList<double> ZoneIndoorTemperatureProfileCelsius,
    IReadOnlyList<double> GroundTemperatureProfileCelsius,
    double TimeStepHours);
