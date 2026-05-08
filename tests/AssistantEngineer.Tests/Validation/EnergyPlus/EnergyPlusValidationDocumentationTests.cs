using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Validation.EnergyPlus;

public class EnergyPlusValidationDocumentationTests
{
    [Fact]
    public void ValidationHarnessDocumentExists()
    {
        Assert.True(
            File.Exists(HarnessDocumentPath),
            $"Validation harness document must exist: {HarnessDocumentPath}");
    }

    [Fact]
    public void ValidationCaseTemplateExists()
    {
        Assert.True(
            File.Exists(CaseTemplatePath),
            $"Validation case template must exist: {CaseTemplatePath}");
    }

    [Fact]
    public void ValidationHarnessDocumentStatesFixtureBasedApproach()
    {
        var content = ReadHarnessDocument();

        Assert.Contains(
            "fixture-based",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "does not run EnergyPlus during normal unit tests",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "not full EnergyPlus comparison workflow",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "not ASHRAE 140 certification",
            content,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidationHarnessDocumentListsInitialSmokeCases()
    {
        var content = ReadHarnessDocument();

        Assert.Contains("EP-SMOKE-001", content, StringComparison.Ordinal);
        Assert.Contains("EP-SMOKE-002", content, StringComparison.Ordinal);
        Assert.Contains("EP-SMOKE-003", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidationHarnessDocumentDefinesMetricTypesAndTolerances()
    {
        var content = ReadHarnessDocument();

        Assert.Contains(
            "NumericWithinTolerance",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "DirectionalTrend",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "SameSign",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "Annual heating energy",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "20 percent",
            content,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidationCaseTemplateContainsRequiredSections()
    {
        var content = ReadCaseTemplate();

        var requiredSections = new[]
        {
            "Case metadata",
            "AssistantEngineer setup",
            "Reference setup",
            "Metrics",
            "Assumptions",
            "Known differences",
            "Non-claims"
        };

        foreach (var requiredSection in requiredSections)
        {
            Assert.Contains(
                requiredSection,
                content,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ValidationCaseTemplateContainsRequiredNonClaims()
    {
        var content = ReadCaseTemplate();

        Assert.Contains(
            "Does not claim exact EnergyPlus numerical equivalence",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "Does not claim ASHRAE 140 / BESTEST-style validation anchor coverage",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "Does not claim full ISO 52016 node/matrix solver equivalence",
            content,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string HarnessDocumentPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "validation",
            "EnergyPlusAshrae140ValidationHarness.md");

    private static string CaseTemplatePath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "validation",
            "EnergyPlusValidationCaseTemplate.md");

    private static string ReadHarnessDocument() =>
        File.ReadAllText(HarnessDocumentPath);

    private static string ReadCaseTemplate() =>
        File.ReadAllText(CaseTemplatePath);
}
