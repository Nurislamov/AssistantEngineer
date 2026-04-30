using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Aggregation;

public sealed record LoadAggregationResult(
    int TargetId,
    LoadAggregationTargetType TargetType,
    string? TargetName,
    int RoomCount,
    double TotalAreaM2,
    double HeatingLoadW,
    double CoolingLoadW,
    double HeatingLoadWPerM2,
    double CoolingLoadWPerM2,
    IReadOnlyList<LoadAggregationRoomBreakdown> RoomBreakdown,
    AggregationComponentBreakdown ComponentBreakdown,
    string AggregationMethod,
    IReadOnlyList<CalculationDiagnostic> Diagnostics,
    DateTimeOffset CalculatedAtUtc)
{
    public bool HasErrors =>
        Diagnostics.Any(diagnostic => diagnostic.Severity == CalculationDiagnosticSeverity.Error);
}
