namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;

public sealed record Iso52016AdjacentUnconditionedZoneTemperatureResult(
    double TemperatureC,
    double HeatFlowToConditionedZoneW,
    double TotalBoundaryConductanceWPerK,
    double TotalGainsW);