namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

public sealed record Iso52016V2HourlyNodeState(
    string NodeId,
    double TemperatureBeforeHvacC,
    double TemperatureAfterHvacC,
    double HeatGainW);