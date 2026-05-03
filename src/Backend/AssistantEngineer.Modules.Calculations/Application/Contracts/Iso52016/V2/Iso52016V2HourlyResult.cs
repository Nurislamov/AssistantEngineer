namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

public sealed record Iso52016V2HourlyResult(
    int HourOfYear,
    int Month,
    int Day,
    int Hour,
    double HeatingSetpointC,
    double CoolingSetpointC,
    double AirTemperatureBeforeHvacC,
    double AirTemperatureAfterHvacC,
    double HeatingLoadW,
    double CoolingLoadW,
    double TimeStepSeconds,
    IReadOnlyList<Iso52016V2HourlyNodeState> NodeStates)
{
    public double HeatingEnergyKWh => HeatingLoadW * TimeStepSeconds / 3_600_000.0;

    public double CoolingEnergyKWh => CoolingLoadW * TimeStepSeconds / 3_600_000.0;

    public double TotalNodeHeatGainsW => NodeStates.Sum(node => node.HeatGainW);

    public double TotalNodeHeatGainsKWh => TotalNodeHeatGainsW * TimeStepSeconds / 3_600_000.0;

    public double GetNodeTemperatureAfterHvacC(string nodeId) =>
        NodeStates.First(node => string.Equals(node.NodeId, nodeId, StringComparison.OrdinalIgnoreCase)).TemperatureAfterHvacC;

    public double GetNodeTemperatureBeforeHvacC(string nodeId) =>
        NodeStates.First(node => string.Equals(node.NodeId, nodeId, StringComparison.OrdinalIgnoreCase)).TemperatureBeforeHvacC;
}