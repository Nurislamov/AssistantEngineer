namespace AssistantEngineer.Tests.Validation.EnergyPlus;

public sealed record EnergyPlusValidationCase(
    string CaseId,
    string Name,
    EnergyPlusValidationStage Stage,
    string Source,
    string WeatherSource,
    string Geometry,
    string Envelope,
    string InternalGains,
    string Ventilation,
    string HvacControl,
    IReadOnlyList<EnergyPlusValidationMetric> Metrics,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> KnownDifferences,
    IReadOnlyList<string> NonClaims);

public sealed record EnergyPlusValidationMetric(
    string MetricId,
    string Name,
    string Unit,
    double AssistantEngineerValue,
    double ReferenceValue,
    double TolerancePercent,
    EnergyPlusValidationMetricType Type,
    string Notes);

public enum EnergyPlusValidationStage
{
    Smoke,
    SimplifiedEnergyPlusComparison,
    Ashrae140Style
}

public enum EnergyPlusValidationMetricType
{
    NumericWithinTolerance,
    DirectionalTrend,
    SameSign
}
