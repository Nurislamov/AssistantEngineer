namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;

public sealed record Iso52016MatrixHourlySolverOptions(
    double TimeStepSeconds = 3600.0,
    string AirNodeId = "air",
    double DefaultHeatingSetpointC = 20.0,
    double DefaultCoolingSetpointC = 26.0);