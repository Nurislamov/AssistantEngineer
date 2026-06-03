using AssistantEngineer.Tests.Architecture.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P9Iso52016MatrixSolverSeamRiskRegisterTests
{
    [Fact]
    public void RiskRegisterArtifactsExist()
    {
        GovernanceSemanticAssertions.AssertDocumentArtifactsExist(
            RiskMarkdownPath,
            RiskJsonPath,
            RiskSchemaPath);
    }

    [Fact]
    public void RiskRegisterJsonParsesAndContainsRequiredRisks()
    {
        using var document = GovernanceJsonTestHelper.Parse(RiskJsonPath);
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

        Assert.True(root.TryGetProperty("expectedValueChangeAllowed", out var expectedValueChangeAllowed));
        Assert.False(expectedValueChangeAllowed.GetBoolean());

        var risks = root.GetProperty("risks").EnumerateArray().ToArray();
        Assert.NotEmpty(risks);

        foreach (var risk in risks)
        {
            Assert.False(string.IsNullOrWhiteSpace(risk.GetProperty("seamId").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(risk.GetProperty("mitigation").GetString()));
            Assert.NotEmpty(risk.GetProperty("requiredTests").EnumerateArray());
            Assert.NotEmpty(risk.GetProperty("mitigationEvidence").EnumerateArray());
            Assert.False(risk.GetProperty("expectedValueChangeAllowed").GetBoolean());
        }

        var descriptions = risks
            .Select(item => item.GetProperty("description").GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(descriptions, d => d.Contains("coefficient sign", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(descriptions, d => d.Contains("load-vector", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(descriptions, d => d.Contains("multi-zone coupling", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(descriptions, d => d.Contains("aggregation", StringComparison.OrdinalIgnoreCase) ||
                                           d.Contains("report", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(descriptions, d => d.Contains("tolerance", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(descriptions, d => d.Contains("overclaim", StringComparison.OrdinalIgnoreCase));
    }

    private static string RiskMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-risk-register.md");

    private static string RiskJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-risk-register.json");

    private static string RiskSchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-risk-register.schema.json");
}
