namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Rollup;

public sealed record EngineeringCalculationModeComparisonRequest(
    EngineeringCalculationModeDomain Domain,
    string CompatibilityModeId,
    string InspiredModeId,
    IReadOnlyList<EngineeringCalculationModeMetric> CompatibilityMetrics,
    IReadOnlyList<EngineeringCalculationModeMetric> InspiredMetrics,
    IReadOnlyDictionary<string, double>? AbsoluteTolerances = null,
    IReadOnlyDictionary<string, double>? RelativeTolerancesPercent = null,
    double DefaultAbsoluteTolerance = 0.0,
    double DefaultRelativeTolerancePercent = 0.0,
    string? DiagnosticsContext = null);
