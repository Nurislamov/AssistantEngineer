namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Analytics;

public sealed record EnergySignatureResult(
    double HeatingBaseTemperatureC,
    IReadOnlyList<EnergySignaturePoint> Points,
    double SlopeKWhPerHdd,
    double InterceptKWh,
    double RSquared);

public sealed record EnergySignaturePoint(
    int Month,
    double HeatingDegreeDays,
    double HeatingDemandKWh);