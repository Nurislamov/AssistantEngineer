using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P9ValidationEvidenceInventoryTests
{
    [Fact]
    public void EvidenceInventoryArtifactsExist()
    {
        GovernanceSemanticAssertions.AssertDocumentArtifactsExist(
            InventoryMarkdownPath,
            InventoryJsonPath,
            InventorySchemaPath);
    }

    [Fact]
    public void EvidenceInventoryJsonParsesAndNoChangeFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var root = document.RootElement;

        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            root,
            ["runtimeBehaviorChanged", "calculationPhysicsChanged", "publicApiChanged"]);

        var provenanceInventoryReference = root.GetProperty("provenanceInventoryReference").GetString() ?? string.Empty;
        Assert.Equal("docs/validation/validation-fixture-provenance-inventory.json", provenanceInventoryReference);

        var componentMapReference = root.GetProperty("iso52016ComponentMapReference").GetString() ?? string.Empty;
        Assert.Equal("docs/validation/iso52016-component-map.json", componentMapReference);
    }

    [Fact]
    public void EvidenceEntriesUseAllowedCategoriesAndClaimFields()
    {
        using var document = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var entries = document.RootElement.GetProperty("evidenceEntries").EnumerateArray().ToArray();
        Assert.NotEmpty(entries);

        var allowedCategories = new HashSet<string>(StringComparer.Ordinal)
        {
            "ManualFixture",
            "IndependentReferenceFixture",
            "InternalInvariant",
            "ReleaseGateManifest",
            "ExternalToolReferenceCandidate",
            "DonorMethodologyReference",
            "HistoricalSmoke",
            "PlannedPlaceholder",
            "UnknownNeedsReview"
        };

        foreach (var entry in entries)
        {
            var category = entry.GetProperty("category").GetString() ?? string.Empty;
            Assert.Contains(category, allowedCategories);

            var provenanceCategory = entry.GetProperty("provenanceCategory").GetString() ?? string.Empty;
            Assert.False(string.IsNullOrWhiteSpace(provenanceCategory));

            var provenanceReference = entry.GetProperty("provenanceInventoryReference").GetString() ?? string.Empty;
            Assert.False(string.IsNullOrWhiteSpace(provenanceReference));

            if (category == "PlannedPlaceholder")
            {
                Assert.True(entry.GetProperty("plannedPlaceholder").GetBoolean());
            }

            var claimAllowed = entry.GetProperty("claimAllowed").EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToArray();
            var claimForbidden = entry.GetProperty("claimForbidden").EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToArray();

            Assert.NotEmpty(claimAllowed);
            Assert.NotEmpty(claimForbidden);
        }
    }

    [Fact]
    public void EvidenceEntriesDoNotContainPositiveParityOrCertificationClaims()
    {
        using var document = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var entries = document.RootElement.GetProperty("evidenceEntries").EnumerateArray().ToArray();

        var forbiddenPositiveFragments = new[]
        {
            " parity",
            "cert" + "ified",
            "fully " + "validated"
        };

        foreach (var entry in entries)
        {
            var allowedClaims = entry.GetProperty("claimAllowed").EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .ToArray();

            foreach (var claim in allowedClaims)
            {
                Assert.DoesNotContain(forbiddenPositiveFragments, fragment => claim.Contains(fragment, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-evidence-inventory.md");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-evidence-inventory.json");

    private static string InventorySchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-evidence-inventory.schema.json");
}
