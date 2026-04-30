using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.EquipmentSizing;

public sealed record EquipmentSizingResult(
    int TargetId,
    EquipmentSizingTargetType TargetType,
    double RequiredHeatingCapacityW,
    double RequiredCoolingCapacityW,
    double SafetyFactor,
    double RequiredHeatingCapacityWithReserveW,
    double RequiredCoolingCapacityWithReserveW,
    IReadOnlyList<EquipmentSizingRecommendedItem> RecommendedEquipment,
    IReadOnlyList<EquipmentSizingRejectedItem> RejectedEquipment,
    EquipmentSizingRecommendedItem? BestMatch,
    IReadOnlyList<CalculationDiagnostic> Diagnostics,
    DateTimeOffset CalculatedAtUtc)
{
    public bool HasErrors =>
        Diagnostics.Any(diagnostic => diagnostic.Severity == CalculationDiagnosticSeverity.Error);
}
