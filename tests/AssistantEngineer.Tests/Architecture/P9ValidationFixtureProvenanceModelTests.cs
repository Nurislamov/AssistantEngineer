using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P9ValidationFixtureProvenanceModelTests
{
    [Fact]
    public void ProvenanceModelArtifactsExist()
    {
        GovernanceSemanticAssertions.AssertDocumentArtifactsExist(
            ModelMarkdownPath,
            ModelJsonPath,
            ModelSchemaPath);
    }

    [Fact]
    public void ProvenanceModelJsonParsesAndNoChangeFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(ModelJsonPath);
        var root = document.RootElement;

        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            root,
            [
                "runtimeBehaviorChanged",
                "calculationPhysicsChanged",
                "expectedValuesChanged",
                "validationClaimChanged"
            ]);
    }

    [Fact]
    public void ProvenanceModelContainsRequiredMetadataFieldsAndStrengthLevels()
    {
        using var document = GovernanceJsonTestHelper.Parse(ModelJsonPath);
        var root = document.RootElement;

        var metadataFields = root.GetProperty("requiredMetadataFields")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        var requiredFields = new[]
        {
            "fixtureId",
            "path",
            "area",
            "provenanceCategory",
            "evidenceStrength",
            "sourceType",
            "sourceDescription",
            "sourceDateOrVersion",
            "generatedBy",
            "reviewedBy",
            "lastReviewedDate",
            "expectedValuePolicy",
            "allowedClaim",
            "forbiddenClaims",
            "relatedTests",
            "knownLimitations"
        };

        foreach (var required in requiredFields)
            Assert.Contains(required, metadataFields);

        var strengthLevels = root.GetProperty("evidenceStrengthLevels")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var required in new[]
                 {
                     "InternalInvariant",
                     "ManualReferenceAnchor",
                     "IndependentReferenceFixture",
                     "ExternalToolCandidate",
                     "CrossImplementationCandidate",
                     "FormalValidationNotClaimed"
                 })
        {
            Assert.Contains(required, strengthLevels);
        }
    }

    [Fact]
    public void ForbiddenClaimsAndNonClaimsContainRequiredBoundaries()
    {
        using var document = GovernanceJsonTestHelper.Parse(ModelJsonPath);
        var root = document.RootElement;

        var forbidden = root.GetProperty("forbiddenClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(forbidden, claim => claim.Contains("EnergyPlus parity", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(forbidden, claim => claim.Contains("pyBuildingEnergy", StringComparison.OrdinalIgnoreCase) && claim.Contains("parity", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(forbidden, claim => claim.Contains("ASHRAE 140 validated", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(forbidden, claim => claim.Contains("ISO certified", StringComparison.OrdinalIgnoreCase));

        var nonClaims = root.GetProperty("nonClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        GovernanceSemanticAssertions.AssertNonClaimsContainConcepts(
            nonClaims,
            [
                "No expected numerical values change claim",
                "No EnergyPlus parity claim",
                "No ASHRAE 140 validation claim"
            ]);
    }

    [Fact]
    public void ReadinessInventoryAndGuardrailsContainP9_03References()
    {
        using var readiness = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("production-saas-readiness-inventory.json"));
        var roadmapItems = readiness.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("P9-03", roadmapItems);

        using var guardrails = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("security-regression-guardrails.json"));
        var ids = guardrails.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SEC-GUARD-VALIDATION-FIXTURE-PROVENANCE", ids);
    }

    private static string ModelMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-fixture-provenance-model.md");

    private static string ModelJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-fixture-provenance-model.json");

    private static string ModelSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-fixture-provenance-model.schema.json");
}
