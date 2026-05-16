using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Services.Governance;

namespace AssistantEngineer.Tests.Validation.Governance;

public sealed class CalculationTraceExplainabilityGovernanceTests
{
    [Fact]
    public void CalculationTraceExplainabilityDocumentsExist()
    {
        Assert.True(File.Exists(ExplainabilityDocPath), $"Calculation trace explainability doc is missing: {ExplainabilityDocPath}");
        Assert.True(File.Exists(TraceSchemaPath), $"Calculation trace schema descriptor is missing: {TraceSchemaPath}");
    }

    [Fact]
    public void ExplainabilityDocumentContainsRequiredSections()
    {
        var content = File.ReadAllText(ExplainabilityDocPath);

        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Trace principles",
            "## Trace section model",
            "## Relationship to input quality",
            "## Relationship to assumptions registry",
            "## Relationship to units governance",
            "## Future integration"
        };

        foreach (var section in requiredSections)
            Assert.Contains(section, content, StringComparison.Ordinal);
    }

    [Fact]
    public void ExplainabilityDocumentContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(ExplainabilityDocPath);

        var py = "pyBuilding";
        var energy = "Energy";
        var exactPyPhrase = $"No {py}{energy} parity claim";
        var escapedPyPhrase = "No pyBuilding\\u0045nergy parity claim";

        Assert.Contains("No ASHRAE 140 compliance claim", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No exact EnergyPlus equivalence claim", content, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            content.Contains(exactPyPhrase, StringComparison.OrdinalIgnoreCase) ||
            content.Contains(escapedPyPhrase, StringComparison.OrdinalIgnoreCase),
            "Explainability document must include external-calculator parity non-claim wording.");
        Assert.Contains("No full ISO/EN compliance claim", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No certified/certification claim", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExplainabilityDocumentReferencesInputQualityAssumptionsAndUnitsGovernance()
    {
        var content = File.ReadAllText(ExplainabilityDocPath);

        Assert.Contains("docs/engineering/input-quality-checks.md", content, StringComparison.Ordinal);
        Assert.Contains("docs/engineering/engineering-assumptions-registry.md", content, StringComparison.Ordinal);
        Assert.Contains("docs/engineering/units-governance.md", content, StringComparison.Ordinal);
    }

    [Fact]
    public void TraceSchemaContainsRequiredFields()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(TraceSchemaPath));
        var root = document.RootElement;

        var requiredRootFields = root
            .GetProperty("requiredRootFields")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("traceId", requiredRootFields);
        Assert.Contains("scope", requiredRootFields);
        Assert.Contains("subjectType", requiredRootFields);
        Assert.Contains("calculationType", requiredRootFields);
        Assert.Contains("sections", requiredRootFields);
        Assert.Contains("assumptions", requiredRootFields);
        Assert.Contains("excludedEffects", requiredRootFields);
        Assert.Contains("diagnosticReferences", requiredRootFields);

        var sectionRequired = root
            .GetProperty("sectionRequiredFields")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("sectionId", sectionRequired);
        Assert.Contains("title", sectionRequired);
        Assert.Contains("category", sectionRequired);
        Assert.Contains("lines", sectionRequired);

        var lineRequired = root
            .GetProperty("lineRequiredFields")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("lineId", lineRequired);
        Assert.Contains("label", lineRequired);
    }

    [Fact]
    public void ExplainabilityDocumentsPassClaimBoundaryScanner()
    {
        var scanner = new EngineeringClaimBoundaryScanner();
        var result = scanner.ScanRepository(
            repositoryRoot: TestPaths.RepoRoot,
            explicitFiles:
            [
                ExplainabilityDocPath,
                TraceSchemaPath
            ]);

        Assert.Equal(0, result.ErrorCount);
    }

    private static string ExplainabilityDocPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "engineering", "calculation-trace-explainability.md");

    private static string TraceSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "engineering", "calculation-trace.schema.json");
}
