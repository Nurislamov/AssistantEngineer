using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P9Iso52016MatrixSolverSeamDesignTests
{
    [Fact]
    public void SeamDesignArtifactsExist()
    {
        GovernanceSemanticAssertions.AssertDocumentArtifactsExist(
            DesignMarkdownPath,
            DesignJsonPath,
            DesignSchemaPath);
    }

    [Fact]
    public void SeamDesignJsonParsesAndNoChangeFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(DesignJsonPath);
        var root = document.RootElement;

        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            root,
            [
                "runtimeBehaviorChanged",
                "calculationPhysicsChanged",
                "expectedValuesChanged",
                "publicApiChanged",
                "validationClaimChanged",
                "calculationSourceFilesChanged",
                "fixtureExpectedValueFilesChanged"
            ]);

        Assert.True(root.TryGetProperty("p901b1HardeningStatus", out var hardeningStatus));
        Assert.Equal("P9-01B1", hardeningStatus.GetProperty("stage").GetString());
    }

    [Fact]
    public void SeamDesignContainsRequiredSeamConceptsAndStages()
    {
        using var document = GovernanceJsonTestHelper.Parse(DesignJsonPath);
        var root = document.RootElement;

        var seamNames = root.GetProperty("proposedSeams")
            .EnumerateArray()
            .Select(item => item.GetProperty("name").GetString() ?? string.Empty)
            .ToArray();

        Assert.NotEmpty(seamNames);
        Assert.Contains(seamNames, item => item.Contains("Matrix input preparation", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(seamNames, item => item.Contains("Matrix assembler", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(seamNames, item => item.Contains("Load vector", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(seamNames, item => item.Contains("Solver kernel", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(seamNames, item => item.Contains("Hourly result mapper", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(seamNames, item => item.Contains("diagnostics", StringComparison.OrdinalIgnoreCase));

        Assert.NotEmpty(root.GetProperty("invariantsToPreserve").EnumerateArray());
        Assert.NotEmpty(root.GetProperty("requiredCharacterizationBeforeExtraction").EnumerateArray());

        var stageIds = root.GetProperty("proposedExtractionStages")
            .EnumerateArray()
            .Select(item => item.GetProperty("stage").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var required in new[] { "P9-01B1", "P9-01B2", "P9-01B3", "P9-01B4", "P9-01B5", "P9-01B6" })
            Assert.Contains(required, stageIds);
    }

    [Fact]
    public void ReadinessInventoryAndGuardrailsContainP9_01BReferences()
    {
        using var readiness = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("production-saas-readiness-inventory.json"));
        var roadmapItems = readiness.RootElement.GetProperty("p5Roadmap")
            .EnumerateArray()
            .Select(item => item.GetProperty("item").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("P9-01B", roadmapItems);
        Assert.Contains("P9-01B1", roadmapItems);

        using var guardrails = GovernanceJsonTestHelper.Parse(
            GovernancePathHelper.SecurityDocPath("security-regression-guardrails.json"));
        var ids = guardrails.RootElement.GetProperty("guardrails")
            .EnumerateArray()
            .Select(item => item.GetProperty("guardrailId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SEC-GUARD-ISO52016-MATRIX-SOLVER-SEAM-DESIGN", ids);
    }

    [Fact]
    public void NonClaimsContainRequiredBoundaries()
    {
        using var document = GovernanceJsonTestHelper.Parse(DesignJsonPath);
        var nonClaims = document.RootElement.GetProperty("nonClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        GovernanceSemanticAssertions.AssertNonClaimsContainConcepts(
            nonClaims,
            [
                "No calculation physics change claim",
                "No expected value change claim",
                "No EnergyPlus " + "parity claim",
                "No ISO certification claim"
            ]);
    }

    private static string DesignMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-design.md");

    private static string DesignJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-design.json");

    private static string DesignSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-design.schema.json");
}
