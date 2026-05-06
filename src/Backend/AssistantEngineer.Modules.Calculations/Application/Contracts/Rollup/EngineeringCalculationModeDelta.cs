namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Rollup;

public sealed record EngineeringCalculationModeDelta(
    string MetricName,
    double CompatibilityValue,
    double InspiredValue,
    double AbsoluteDelta,
    double? RelativeDeltaPercent,
    double AbsoluteTolerance,
    double RelativeTolerancePercent,
    bool IsPass,
    bool IsWarning,
    string DiagnosticMessage);
