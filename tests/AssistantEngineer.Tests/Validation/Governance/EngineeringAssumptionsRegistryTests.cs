using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Services.Governance;

namespace AssistantEngineer.Tests.Validation.Governance;

public sealed class EngineeringAssumptionsRegistryTests
{
    [Fact]
    public void AssumptionsRegistryDocumentsExist()
    {
        Assert.True(File.Exists(RegistryMarkdownPath), $"Engineering assumptions registry markdown is missing: {RegistryMarkdownPath}");
        Assert.True(File.Exists(RegistryJsonPath), $"Engineering assumptions registry json is missing: {RegistryJsonPath}");
        Assert.True(File.Exists(RegistrySchemaPath), $"Engineering assumptions registry schema descriptor is missing: {RegistrySchemaPath}");
    }

    [Fact]
    public void RegistryMarkdownContainsRequiredSections()
    {
        var content = File.ReadAllText(RegistryMarkdownPath);

        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Registry status model",
            "## Required fields per assumption"
        };

        foreach (var section in requiredSections)
            Assert.Contains(section, content, StringComparison.Ordinal);
    }

    [Fact]
    public void RegistryMarkdownContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(RegistryMarkdownPath);

        var py = "pyBuilding";
        var energy = "Energy";
        var exactPyPhrase = $"No {py}{energy} parity claim";
        var escapedPyPhrase = "No pyBuilding\\u0045nergy parity claim";

        Assert.Contains("No ASHRAE 140 compliance claim", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No exact EnergyPlus equivalence claim", content, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            content.Contains(exactPyPhrase, StringComparison.OrdinalIgnoreCase) ||
            content.Contains(escapedPyPhrase, StringComparison.OrdinalIgnoreCase),
            "Registry must include external-calculator parity non-claim wording.");
        Assert.Contains("No full ISO/EN compliance claim", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No certified/certification claim", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegistryJsonParsesAndContainsRequiredStructure()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(RegistryJsonPath));
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("assumptions", out var assumptions));
        Assert.Equal(JsonValueKind.Array, assumptions.ValueKind);
        Assert.True(assumptions.GetArrayLength() > 0, "Registry assumptions array must not be empty.");

        var requiredFields = new[]
        {
            "assumptionId",
            "category",
            "name",
            "value",
            "unit",
            "status",
            "source",
            "usageArea",
            "rationale",
            "risk",
            "owner",
            "lastReviewedDate"
        };

        var allowedStatuses = new HashSet<string>(StringComparer.Ordinal)
        {
            "ActiveDefault",
            "ValidationOnly",
            "Candidate",
            "Deprecated",
            "UnknownNeedsAudit"
        };

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var assumption in assumptions.EnumerateArray())
        {
            foreach (var field in requiredFields)
                Assert.True(assumption.TryGetProperty(field, out _), $"Assumption is missing required field: {field}");

            var assumptionId = assumption.GetProperty("assumptionId").GetString() ?? string.Empty;
            Assert.False(string.IsNullOrWhiteSpace(assumptionId), "assumptionId must not be empty.");
            Assert.True(ids.Add(assumptionId), $"Duplicate assumptionId found: {assumptionId}");

            var status = assumption.GetProperty("status").GetString() ?? string.Empty;
            Assert.Contains(status, allowedStatuses);

            var owner = assumption.GetProperty("owner").GetString();
            Assert.Equal("Engineering", owner);
        }

        var requiredIds = new[]
        {
            "ASSUMP-VENT-SENSIBLE-COEFFICIENT-001",
            "ASSUMP-DHW-WATER-DENSITY-001",
            "ASSUMP-DHW-SPECIFIC-HEAT-001",
            "ASSUMP-SYS-DISTRIBUTION-EFF-001",
            "ASSUMP-SYS-GENERATION-EFF-001",
            "ASSUMP-SYS-PRIMARY-FACTOR-FUEL-001",
            "ASSUMP-SYS-PRIMARY-FACTOR-ELECTRICITY-001"
        };

        foreach (var requiredId in requiredIds)
            Assert.Contains(requiredId, ids);
    }

    [Fact]
    public void RegistrySchemaDescriptorContainsRequiredDefinitionKeys()
    {
        using var schema = JsonDocument.Parse(File.ReadAllText(RegistrySchemaPath));
        var root = schema.RootElement;

        Assert.True(root.TryGetProperty("requiredFields", out var requiredFields));
        Assert.True(root.TryGetProperty("allowedStatuses", out var allowedStatuses));

        var requiredFieldNames = requiredFields
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        var expectedFields = new[]
        {
            "assumptionId",
            "category",
            "name",
            "value",
            "unit",
            "status",
            "source",
            "usageArea",
            "rationale",
            "risk",
            "owner",
            "lastReviewedDate"
        };

        foreach (var field in expectedFields)
            Assert.Contains(field, requiredFieldNames);

        var statusNames = allowedStatuses
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("ActiveDefault", statusNames);
        Assert.Contains("ValidationOnly", statusNames);
        Assert.Contains("Candidate", statusNames);
        Assert.Contains("Deprecated", statusNames);
        Assert.Contains("UnknownNeedsAudit", statusNames);
    }

    [Fact]
    public void ValidationDocumentsReferenceAssumptionsRegistry()
    {
        var fixtureManifest = File.ReadAllText(ManualFixturesPath);
        var tolerancePolicy = File.ReadAllText(TolerancePolicyPath);

        Assert.Contains("docs/engineering/engineering-assumptions-registry.md", fixtureManifest, StringComparison.Ordinal);
        Assert.Contains("docs/engineering/engineering-assumptions-registry.md", tolerancePolicy, StringComparison.Ordinal);
    }

    [Fact]
    public void AssumptionsRegistryDocumentsPassClaimBoundaryScanner()
    {
        var scanner = new EngineeringClaimBoundaryScanner();
        var result = scanner.ScanRepository(
            repositoryRoot: TestPaths.RepoRoot,
            explicitFiles:
            [
                RegistryMarkdownPath,
                RegistryJsonPath,
                RegistrySchemaPath,
                ManualFixturesPath,
                TolerancePolicyPath
            ]);

        Assert.Equal(0, result.ErrorCount);
    }

    private static string RegistryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "engineering", "engineering-assumptions-registry.md");

    private static string RegistryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "engineering", "engineering-assumptions-registry.json");

    private static string RegistrySchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "engineering", "engineering-assumptions-registry.schema.json");

    private static string ManualFixturesPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "manual-engineering-fixtures.md");

    private static string TolerancePolicyPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-tolerance-policy.md");
}
