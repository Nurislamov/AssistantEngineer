using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P9ValidationFixtureProvenanceInventoryTests
{
    [Fact]
    public void ProvenanceInventoryArtifactsExist()
    {
        GovernanceSemanticAssertions.AssertDocumentArtifactsExist(
            InventoryMarkdownPath,
            InventoryJsonPath,
            InventorySchemaPath);
    }

    [Fact]
    public void ProvenanceInventoryJsonParsesAndNoChangeFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var root = document.RootElement;

        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            root,
            [
                "runtimeBehaviorChanged",
                "calculationPhysicsChanged",
                "expectedValuesChanged",
                "validationClaimChanged"
            ]);

        var componentMapReference = root.GetProperty("iso52016ComponentMapReference").GetString() ?? string.Empty;
        Assert.Equal("docs/validation/iso52016-component-map.json", componentMapReference);
    }

    [Fact]
    public void EntriesContainRequiredMetadataAndExpectedValuesRemainUnchanged()
    {
        using var document = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var root = document.RootElement;
        var entries = root.GetProperty("entries").EnumerateArray().ToArray();
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

        var allowedStrength = new HashSet<string>(StringComparer.Ordinal)
        {
            "InternalInvariant",
            "ManualReferenceAnchor",
            "IndependentReferenceFixture",
            "ExternalToolCandidate",
            "CrossImplementationCandidate",
            "FormalValidationNotClaimed"
        };

        foreach (var entry in entries)
        {
            Assert.True(entry.GetProperty("expectedValuesChangedInP903").ValueKind == System.Text.Json.JsonValueKind.False);

            var category = entry.GetProperty("provenanceCategory").GetString() ?? string.Empty;
            Assert.Contains(category, allowedCategories);

            var strength = entry.GetProperty("evidenceStrength").GetString() ?? string.Empty;
            Assert.Contains(strength, allowedStrength);

            var fixtureId = entry.GetProperty("fixtureId").GetString() ?? string.Empty;
            Assert.False(string.IsNullOrWhiteSpace(fixtureId));
            Assert.False(string.IsNullOrWhiteSpace(entry.GetProperty("path").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(entry.GetProperty("allowedClaim").GetString()));

            var forbidden = entry.GetProperty("forbiddenClaims").EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .ToArray();
            Assert.NotEmpty(forbidden);
            Assert.DoesNotContain(forbidden, claim => claim.Contains("allowed:", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void PlannedAndUnknownEntriesRequireFollowUpStageAndAreNotAchievedEvidence()
    {
        using var document = GovernanceJsonTestHelper.Parse(InventoryJsonPath);
        var entries = document.RootElement.GetProperty("entries").EnumerateArray().ToArray();

        foreach (var entry in entries)
        {
            var category = entry.GetProperty("provenanceCategory").GetString() ?? string.Empty;
            if (category is not ("PlannedPlaceholder" or "UnknownNeedsReview"))
                continue;

            var stage = entry.GetProperty("proposedFollowUpStage").GetString() ?? string.Empty;
            Assert.False(string.IsNullOrWhiteSpace(stage));

            if (category == "PlannedPlaceholder")
            {
                var claim = entry.GetProperty("allowedClaim").GetString() ?? string.Empty;
                Assert.DoesNotContain("achieved evidence", claim, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private static string InventoryMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-fixture-provenance-inventory.md");

    private static string InventoryJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-fixture-provenance-inventory.json");

    private static string InventorySchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-fixture-provenance-inventory.schema.json");
}
