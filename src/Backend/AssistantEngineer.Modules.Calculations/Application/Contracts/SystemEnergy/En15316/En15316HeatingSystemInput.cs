namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;

public sealed record En15316HeatingSystemInput(
    string CalculationId,
    IReadOnlyList<HeatingCircuit> Circuits,
    IReadOnlyList<HeatingOperatingCondition> OperatingConditions,
    IReadOnlyList<HeatingSystemTimeStepInput> TimeSteps,
    string? DiagnosticsContext = null);
