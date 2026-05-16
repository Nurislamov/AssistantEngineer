using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

namespace AssistantEngineer.Modules.Calculations.Application.Models.Trace;

public sealed record RoomHeatingLoadTraceInput(
    int? RoomId,
    string? RoomName,
    double TransmissionHeatLossW,
    double VentilationHeatLossW,
    double InfiltrationHeatLossW,
    double GroundHeatLossW,
    double SolarGainW,
    double InternalGainW,
    double TotalHeatingLoadW,
    IReadOnlyList<EngineeringCalculationTraceAssumption> Assumptions,
    IReadOnlyList<EngineeringCalculationTraceExcludedEffect> ExcludedEffects,
    IReadOnlyList<EngineeringCalculationTraceDiagnosticReference> DiagnosticReferences);
