namespace AssistantEngineer.Tests.Validation.ExternalReferenceValidation.BenchmarkFixtures;

internal sealed class EnergyBenchmarkToleranceSet
{
    public double? DefaultAbsolute { get; set; }
    public double? DefaultRelativePercent { get; set; }
    public Dictionary<string, EnergyBenchmarkTolerance> Fields { get; set; } = [];

    public EnergyBenchmarkTolerance Resolve(string fieldPath)
    {
        var fieldTolerance = Fields
            .FirstOrDefault(field => string.Equals(field.Key, fieldPath, StringComparison.OrdinalIgnoreCase))
            .Value;

        return new EnergyBenchmarkTolerance
        {
            Absolute = fieldTolerance?.Absolute ?? DefaultAbsolute,
            RelativePercent = fieldTolerance?.RelativePercent ?? DefaultRelativePercent
        };
    }
}

internal sealed class EnergyBenchmarkTolerance
{
    public double? Absolute { get; set; }
    public double? RelativePercent { get; set; }
}
