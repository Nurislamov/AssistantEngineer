namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

public sealed record Iso52016AdjacentUnconditionedZoneTemperatureResult(
    double TemperatureC,
    double HeatFlowToConditionedZoneW,
    double TotalBoundaryConductanceWPerK,
    double TotalGainsW);