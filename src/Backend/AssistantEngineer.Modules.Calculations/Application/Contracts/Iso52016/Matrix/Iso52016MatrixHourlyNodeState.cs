namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;

public sealed record Iso52016MatrixHourlyNodeState(
    string NodeId,
    double TemperatureBeforeHvacC,
    double TemperatureAfterHvacC,
    double HeatGainW);