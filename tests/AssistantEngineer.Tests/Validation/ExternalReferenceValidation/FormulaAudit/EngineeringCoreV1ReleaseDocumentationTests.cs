using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Validation.ExternalReferenceValidation.FormulaAudit;

public class EngineeringCoreV1ReleaseDocumentationTests
{
    [Fact]
    public void ReleaseNotesDocumentExists()
    {
        Assert.True(
            File.Exists(ReleaseNotesPath),
            $"Engineering-core v1 release notes must exist: {ReleaseNotesPath}");
    }

    [Fact]
    public void ValidationPlanDocumentExists()
    {
        Assert.True(
            File.Exists(ValidationPlanPath),
            $"EnergyPlus / ASHRAE 140 / BESTEST-style validation anchor plan must exist: {ValidationPlanPath}");
    }

    [Fact]
    public void ReleaseNotesStateEngineeringCoreV1FormulaGatesAreClosed()
    {
        var content = ReadReleaseNotes();

        Assert.Contains(
            "Engineering-core v1 formula gates are closed",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "does not claim exact numeric equivalence",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "ClosedV1",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseNotesListAllClosedFormulaAuditGates()
    {
        var content = ReadReleaseNotes();

        var closedFeatureIds = FormulaAuditMatrix.Features
            .Where(feature => feature.Status == FormulaAuditStatus.ClosedV1)
            .Select(feature => feature.CalculationId)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(closedFeatureIds);

        foreach (var featureId in closedFeatureIds)
        {
            Assert.Contains(
                featureId,
                content,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ReleaseNotesContainExplicitNonClaims()
    {
        var content = ReadReleaseNotes();

        var requiredNonClaims = new[]
        {
            "full ISO 52016 node/matrix solver equivalence",
            "full ISO 13370 implementation",
            "exact StandardReference numerical equivalence",
            "exact EnergyPlus numerical equivalence",
            "ASHRAE 140 / BESTEST-style validation anchor coverage",
            "latent load calculation",
            "moisture balance"
        };

        foreach (var requiredNonClaim in requiredNonClaims)
        {
            Assert.Contains(
                requiredNonClaim,
                content,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ReleaseNotesDefineWeatherAndAnnualEnergyGate()
    {
        var content = ReadReleaseNotes();

        Assert.Contains(
            "EnergyDataSource = TrueHourlySimulation",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "IsTrueHourly8760 = true",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "HourlyRecordCount = 8760",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "Monthly adapter, synthetic weather and deterministic short fixtures",
            content,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReleaseNotesDefineDiagnosticsRule()
    {
        var content = ReadReleaseNotes();

        Assert.Contains(
            "Error",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "must fail the calculation",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "Warning",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "Info",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ValidationPlanStatesFutureValidationIsNotV1FormulaGate()
    {
        var content = ReadValidationPlan();

        Assert.Contains(
            "future validation layers",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "not required gates for engineering-core v1 formula closure",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "comparative engineering validation",
            content,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidationPlanAvoidsExactParityClaim()
    {
        var content = ReadValidationPlan();

        Assert.Contains(
            "not exact watt-by-watt equivalence",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "should not claim",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "exact EnergyPlus numerical equivalence",
            content,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidationPlanContainsProposedTolerancesAndCaseMetadata()
    {
        var content = ReadValidationPlan();

        Assert.Contains(
            "Suggested tolerances",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "Annual heating energy",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "Required metadata per validation case",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "case id",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "pass/fail",
            content,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormulaAuditMatrixHasNoPartialItemsAfterEngineeringCoreV1Release()
    {
        var partialItems = FormulaAuditMatrix.Features
            .Where(feature => feature.Status == FormulaAuditStatus.Partial)
            .Select(feature => feature.CalculationId)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            partialItems.Length == 0,
            $"Release docs require no remaining Partial formula items: {string.Join(", ", partialItems)}.");
    }

    private static string ReleaseNotesPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "EngineeringCoreV1ReleaseNotes.md");

    private static string ValidationPlanPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "EnergyPlusAshrae140ValidationPlan.md");

    private static string ReadReleaseNotes() =>
        File.ReadAllText(ReleaseNotesPath);

    private static string ReadValidationPlan() =>
        File.ReadAllText(ValidationPlanPath);
}