namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Rollup;

public sealed record EngineeringCalculationModeComparisonResult(
    EngineeringCalculationModeDomain Domain,
    string CompatibilityModeId,
    string InspiredModeId,
    IReadOnlyList<EngineeringCalculationModeDelta> Deltas,
    bool IsPass,
    bool HasWarnings,
    string SummaryStatus,
    IReadOnlyList<EngineeringCalculationModeDiagnostic> Diagnostics);
