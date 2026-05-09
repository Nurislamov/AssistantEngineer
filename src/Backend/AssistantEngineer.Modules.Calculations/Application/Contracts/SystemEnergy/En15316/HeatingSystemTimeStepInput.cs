namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;

public sealed record HeatingSystemTimeStepInput(
    int TimeStepIndex,
    int Month,
    double UsefulHeatingLoadKWh,
    double UsefulDhwLoadKWh = 0.0,
    double? OutdoorTemperatureC = null,
    string? OperatingConditionId = null,
    IReadOnlyDictionary<string, double>? CircuitLoadFractions = null);
