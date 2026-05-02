namespace AssistantEngineer.Tests.Validation.EnergyPlus;

public class EnergyPlusValidationHarnessTests
{
    [Fact]
    public void ValidationFixturesContainUniqueCaseIds()
    {
        var duplicateIds = EnergyPlusValidationFixtures.Cases
            .GroupBy(item => item.CaseId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            duplicateIds.Length == 0,
            $"EnergyPlus validation case ids must be unique: {string.Join(", ", duplicateIds)}.");
    }

    [Fact]
    public void ValidationFixturesContainInitialSmokeCases()
    {
        var caseIds = EnergyPlusValidationFixtures.Cases
            .Select(item => item.CaseId)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("EP-SMOKE-001", caseIds);
        Assert.Contains("EP-SMOKE-002", caseIds);
        Assert.Contains("EP-SMOKE-003", caseIds);
    }

    [Fact]
    public void ValidationCasesHaveRequiredMetadata()
    {
        foreach (var validationCase in EnergyPlusValidationFixtures.Cases)
        {
            Assert.False(string.IsNullOrWhiteSpace(validationCase.CaseId));
            Assert.False(string.IsNullOrWhiteSpace(validationCase.Name));
            Assert.False(string.IsNullOrWhiteSpace(validationCase.Source));
            Assert.False(string.IsNullOrWhiteSpace(validationCase.WeatherSource));
            Assert.False(string.IsNullOrWhiteSpace(validationCase.Geometry));
            Assert.False(string.IsNullOrWhiteSpace(validationCase.Envelope));
            Assert.False(string.IsNullOrWhiteSpace(validationCase.HvacControl));

            Assert.NotEmpty(validationCase.Metrics);
            Assert.NotEmpty(validationCase.Assumptions);
            Assert.NotEmpty(validationCase.KnownDifferences);
            Assert.NotEmpty(validationCase.NonClaims);
        }
    }

    [Fact]
    public void ValidationCaseMetricsHaveUnitsAndNotes()
    {
        var violations = EnergyPlusValidationFixtures.Cases
            .SelectMany(validationCase => validationCase.Metrics.Select(metric => new
            {
                validationCase.CaseId,
                metric.MetricId,
                metric.Unit,
                metric.Notes
            }))
            .Where(item =>
                string.IsNullOrWhiteSpace(item.MetricId) ||
                string.IsNullOrWhiteSpace(item.Unit) ||
                string.IsNullOrWhiteSpace(item.Notes))
            .Select(item => $"{item.CaseId}:{item.MetricId}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Validation metrics must include id, unit and notes: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void NumericValidationMetricsPassConfiguredTolerances()
    {
        var failures = EnergyPlusValidationFixtures.Cases
            .SelectMany(validationCase => validationCase.Metrics.Select(metric => new
            {
                validationCase.CaseId,
                Metric = metric
            }))
            .Where(item => item.Metric.Type == EnergyPlusValidationMetricType.NumericWithinTolerance)
            .Where(item => !IsWithinTolerance(item.Metric))
            .Select(item =>
                $"{item.CaseId}:{item.Metric.MetricId} AE={item.Metric.AssistantEngineerValue} Reference={item.Metric.ReferenceValue} Tolerance={item.Metric.TolerancePercent}%")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            failures.Length == 0,
            $"Validation metrics outside tolerance: {string.Join("; ", failures)}.");
    }

    [Fact]
    public void DirectionalValidationMetricsMatchExpectedDirection()
    {
        var failures = EnergyPlusValidationFixtures.Cases
            .SelectMany(validationCase => validationCase.Metrics.Select(metric => new
            {
                validationCase.CaseId,
                Metric = metric
            }))
            .Where(item => item.Metric.Type == EnergyPlusValidationMetricType.DirectionalTrend)
            .Where(item => Math.Sign(item.Metric.AssistantEngineerValue) != Math.Sign(item.Metric.ReferenceValue))
            .Select(item => $"{item.CaseId}:{item.Metric.MetricId}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            failures.Length == 0,
            $"Directional validation metrics must match expected direction: {string.Join(", ", failures)}.");
    }

    [Fact]
    public void SameSignValidationMetricsMatchExpectedSign()
    {
        var failures = EnergyPlusValidationFixtures.Cases
            .SelectMany(validationCase => validationCase.Metrics.Select(metric => new
            {
                validationCase.CaseId,
                Metric = metric
            }))
            .Where(item => item.Metric.Type == EnergyPlusValidationMetricType.SameSign)
            .Where(item => Math.Sign(item.Metric.AssistantEngineerValue) != Math.Sign(item.Metric.ReferenceValue))
            .Select(item => $"{item.CaseId}:{item.Metric.MetricId}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            failures.Length == 0,
            $"Same-sign validation metrics must match expected sign: {string.Join(", ", failures)}.");
    }

    [Fact]
    public void ValidationFixturesDoNotClaimExactEnergyPlusOrAshrae140Parity()
    {
        foreach (var validationCase in EnergyPlusValidationFixtures.Cases)
        {
            Assert.Contains(validationCase.NonClaims, claim =>
                claim.Contains("Does not claim exact EnergyPlus", StringComparison.OrdinalIgnoreCase));

            Assert.Contains(validationCase.NonClaims, claim =>
                claim.Contains("Does not claim ASHRAE 140", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void ValidationFixturesRemainSmokeOrFutureComparativeCasesNotV1FormulaGates()
    {
        Assert.All(EnergyPlusValidationFixtures.Cases, validationCase =>
        {
            Assert.True(
                validationCase.Stage is EnergyPlusValidationStage.Smoke or
                    EnergyPlusValidationStage.SimplifiedEnergyPlusComparison or
                    EnergyPlusValidationStage.Ashrae140Style);

            Assert.Contains(
                "future",
                validationCase.Source,
                StringComparison.OrdinalIgnoreCase);
        });
    }

    private static bool IsWithinTolerance(EnergyPlusValidationMetric metric)
    {
        if (metric.ReferenceValue == 0)
            return metric.AssistantEngineerValue == 0;

        var percentDifference = Math.Abs(metric.AssistantEngineerValue - metric.ReferenceValue) /
            Math.Abs(metric.ReferenceValue) *
            100.0;

        return percentDifference <= metric.TolerancePercent;
    }
}
