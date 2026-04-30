namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Aggregation;

public sealed record AggregationComponentBreakdown(
    double TransmissionW,
    double SolarW,
    double VentilationW,
    double InfiltrationW,
    double InternalW,
    double GroundW);
