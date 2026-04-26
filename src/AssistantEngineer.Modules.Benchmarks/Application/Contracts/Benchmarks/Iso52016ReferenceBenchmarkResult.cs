namespace AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;

public sealed record Iso52016ReferenceBenchmarkResult(
    string CaseId,
    string Description,
    bool Passed,
    IReadOnlyDictionary<string, double> Metrics,
    IReadOnlyList<Iso52016ReferenceBenchmarkAssertion> Assertions);

public sealed record Iso52016ReferenceBenchmarkAssertion(
    string Name,
    double Actual,
    string Operator,
    double Expected,
    double Tolerance,
    bool Passed);

public sealed record Iso52016BenchmarkMetricExpectation(
    double ExpectedValue,
    double AbsoluteTolerance);