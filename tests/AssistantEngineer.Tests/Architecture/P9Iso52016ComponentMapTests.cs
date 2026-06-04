using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P9Iso52016ComponentMapTests
{
    [Fact]
    public void ComponentMapArtifactsExist()
    {
        GovernanceSemanticAssertions.AssertDocumentArtifactsExist(
            MapMarkdownPath,
            MapJsonPath,
            MapSchemaPath);
    }

    [Fact]
    public void ComponentMapJsonParsesAndNoChangeFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(MapJsonPath);
        var root = document.RootElement;

        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            root,
            [
                "runtimeBehaviorChanged",
                "calculationPhysicsChanged",
                "expectedValuesChanged",
                "publicApiChanged",
                "validationClaimChanged"
            ]);
    }

    [Fact]
    public void ComponentMapContainsRequiredCategories()
    {
        using var document = GovernanceJsonTestHelper.Parse(MapJsonPath);
        var components = document.RootElement.GetProperty("components").EnumerateArray().ToArray();
        Assert.NotEmpty(components);

        var categories = components
            .Select(item => item.GetProperty("category").GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(categories, value => value.Contains("Matrix assembly", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(categories, value => value.Contains("Matrix solving", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(categories, value => value.Contains("Weather/solar", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(categories, value => value.Contains("Internal gains", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(categories, value => value.Contains("Report/diagnostics", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ComponentsContainFilesAndTestReferencesOrKnownLimitations()
    {
        using var document = GovernanceJsonTestHelper.Parse(MapJsonPath);
        var components = document.RootElement.GetProperty("components").EnumerateArray().ToArray();

        foreach (var component in components)
        {
            var files = component.GetProperty("files").EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToArray();

            var tests = component.GetProperty("tests").EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToArray();

            var evidence = component.GetProperty("validationEvidenceEntries").EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToArray();

            var limitations = component.GetProperty("knownLimitations").EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToArray();

            var hasCoverage = files.Length > 0 && (tests.Length > 0 || evidence.Length > 0 || limitations.Length > 0);
            Assert.True(hasCoverage, $"Component {component.GetProperty("id").GetString()} must include files and tests/evidence/limitations.");
        }
    }

    [Fact]
    public void ProvenanceReferencesExistWherePresent()
    {
        using var map = GovernanceJsonTestHelper.Parse(MapJsonPath);
        var provenanceIds = map.RootElement.GetProperty("components")
            .EnumerateArray()
            .SelectMany(component => component.GetProperty("provenanceEntries").EnumerateArray())
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(provenanceIds);

        using var provenance = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-fixture-provenance-inventory.json"));
        var inventoryIds = provenance.RootElement.GetProperty("entries")
            .EnumerateArray()
            .Select(item => item.GetProperty("fixtureId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var id in provenanceIds)
            Assert.Contains(id, inventoryIds);
    }

    [Fact]
    public void BehaviorCharacterizationInventoryReferenceIsPresent()
    {
        using var map = GovernanceJsonTestHelper.Parse(MapJsonPath);
        var root = map.RootElement;

        var inventoryReference = root.GetProperty("behaviorCharacterizationInventoryReference").GetString() ?? string.Empty;
        Assert.Equal("docs/validation/iso52016-behavior-characterization-inventory.json", inventoryReference);

        var coverage = root.GetProperty("characterizationCoverage").EnumerateArray().ToArray();
        Assert.NotEmpty(coverage);
        Assert.Equal(
            root.GetProperty("components").GetArrayLength(),
            coverage.Length);

        var seamDesignReference = root.GetProperty("seamDesignReference").GetString() ?? string.Empty;
        Assert.Equal("docs/validation/iso52016-matrix-solver-seam-design.json", seamDesignReference);

        var hardeningReference = root.GetProperty("hardeningReportReference").GetString() ?? string.Empty;
        Assert.Equal("docs/validation/iso52016-matrix-solver-characterization-hardening.json", hardeningReference);

        var componentsWithSeamRef = root.GetProperty("components")
            .EnumerateArray()
            .Where(item => item.TryGetProperty("seamDesignReference", out _))
            .ToArray();
        Assert.NotEmpty(componentsWithSeamRef);
    }

    private static string MapMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-component-map.md");

    private static string MapJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-component-map.json");

    private static string MapSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-component-map.schema.json");
}
