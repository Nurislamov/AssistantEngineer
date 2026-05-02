namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity.BenchmarkFixtures;

internal sealed record EnergyBenchmarkComparisonResult(
    string FixtureName,
    string FieldPath,
    double Expected,
    double Actual,
    double AbsoluteDifference,
    double RelativeDifferencePercent,
    double? AbsoluteTolerance,
    double? RelativeTolerancePercent,
    bool Passed,
    string Message);
