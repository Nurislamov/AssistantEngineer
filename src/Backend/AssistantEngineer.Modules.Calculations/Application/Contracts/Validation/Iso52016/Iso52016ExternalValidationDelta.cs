namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.Iso52016;

public sealed record Iso52016ExternalValidationDelta(
    string MetricName,
    double ExpectedValue,
    double ActualValue,
    double AbsoluteDelta,
    double RelativeDeltaPercent,
    double AbsoluteTolerance,
    double RelativeTolerancePercent,
    Iso52016ExternalValidationStatus Status,
    string Diagnostics);
