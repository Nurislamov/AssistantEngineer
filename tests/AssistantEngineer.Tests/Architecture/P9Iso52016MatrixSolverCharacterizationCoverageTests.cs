using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P9Iso52016MatrixSolverCharacterizationCoverageTests
{
    [Fact]
    public void SeamDesignRequiredCharacterizationReferencesB1CoverageOrExplicitGap()
    {
        using var seamDesign = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-design.json"));

        var required = seamDesign.RootElement.GetProperty("requiredCharacterizationBeforeExtraction")
            .EnumerateArray()
            .ToArray();
        Assert.NotEmpty(required);

        var statuses = required
            .Select(item => item.GetProperty("status").GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(statuses, status => status.Contains("ImplementedInP9-01B1", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(statuses, status => status.Contains("PartiallyImplementedInP9-01B1", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RiskRegisterContainsMitigationEvidenceForCoreRisks()
    {
        using var riskRegister = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-risk-register.json"));

        var risks = riskRegister.RootElement.GetProperty("risks").EnumerateArray().ToArray();
        Assert.NotEmpty(risks);

        foreach (var risk in risks)
        {
            Assert.NotEmpty(risk.GetProperty("mitigationEvidence").EnumerateArray());
            Assert.False(risk.GetProperty("expectedValueChangeAllowed").GetBoolean());
        }

        var descriptions = risks
            .Select(item => item.GetProperty("description").GetString() ?? string.Empty)
            .ToArray();
        var evidence = risks
            .SelectMany(item => item.GetProperty("mitigationEvidence").EnumerateArray().Select(value => value.GetString() ?? string.Empty))
            .ToArray();

        Assert.Contains(descriptions, item => item.Contains("coefficient sign", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(descriptions, item => item.Contains("load-vector", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(descriptions, item => item.Contains("multi-zone coupling", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(descriptions, item => item.Contains("tolerance", StringComparison.OrdinalIgnoreCase));

        Assert.Contains("Iso52016MatrixAssemblyInvariantTests", evidence);
        Assert.Contains("Iso52016LoadVectorCharacterizationTests", evidence);
        Assert.Contains("Iso52016SolverKernelCharacterizationTests", evidence);
    }

    [Fact]
    public void NewCharacterizationTestsListedInHardeningJsonExistInSourceTree()
    {
        using var hardening = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-characterization-hardening.json"));

        var listed = hardening.RootElement.GetProperty("newCharacterizationTests")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();
        Assert.NotEmpty(listed);

        foreach (var testName in listed)
        {
            var expectedFile = Path.Combine(
                TestPaths.RepoRoot,
                "tests",
                "AssistantEngineer.Tests",
                "Calculations",
                "Iso52016",
                "Matrix",
                $"{testName}.cs");

            Assert.True(
                File.Exists(expectedFile),
                $"Expected characterization test source file is missing: {expectedFile}");
        }
    }

    [Fact]
    public void ProposedExtractionStagesDoNotAllowBehaviorChange()
    {
        using var seamDesign = GovernanceJsonTestHelper.Parse(
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-design.json"));

        var stages = seamDesign.RootElement.GetProperty("proposedExtractionStages").EnumerateArray().ToArray();
        Assert.NotEmpty(stages);

        foreach (var stage in stages)
            Assert.False(stage.GetProperty("behaviorChangeAllowed").GetBoolean());
    }
}
