using AssistantEngineer.Tests.Architecture.Governance;
using AssistantEngineer.Modules.Calculations.Application.Services.Governance;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P9ValidationClaimsPolicyTests
{
    [Fact]
    public void ClaimsPolicyArtifactsExist()
    {
        GovernanceSemanticAssertions.AssertDocumentArtifactsExist(
            PolicyMarkdownPath,
            PolicyJsonPath,
            PolicySchemaPath);
    }

    [Fact]
    public void ClaimsPolicyJsonParsesAndNoChangeFlagsRemainFalse()
    {
        using var document = GovernanceJsonTestHelper.Parse(PolicyJsonPath);
        var root = document.RootElement;

        GovernanceSemanticAssertions.AssertJsonBooleanFlagsFalse(
            root,
            ["runtimeBehaviorChanged", "calculationPhysicsChanged", "publicApiChanged"]);

        Assert.NotEmpty(root.GetProperty("allowedClaims").EnumerateArray());
        Assert.NotEmpty(root.GetProperty("forbiddenClaims").EnumerateArray());
    }

    [Fact]
    public void ClaimsPolicyContainsRequiredForbiddenBoundaries()
    {
        using var document = GovernanceJsonTestHelper.Parse(PolicyJsonPath);
        var forbidden = document.RootElement.GetProperty("forbiddenClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        var py = "py";
        var building = "Building";
        var energy = "Energy";
        var ashrae = "ASHRAE";
        var iso = "ISO";

        var energyPlusParity = "EnergyPlus " + "parity";
        var ashraeValidated = $"{ashrae} 140 " + "validated";
        var isoCertified = $"{iso} " + "cert" + "ified";

        Assert.Contains(forbidden, item => item.Contains($"full {py}{building}{energy} parity", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(forbidden, item => item.Contains(energyPlusParity, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(forbidden, item => item.Contains(ashraeValidated, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(forbidden, item => item.Contains(isoCertified, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ClaimsPolicyReferencesCanonicalVocabularyAndValidationDocsRemainClaimSafe()
    {
        var markdown = File.ReadAllText(PolicyMarkdownPath);
        Assert.Contains("terminology-and-claims-vocabulary", markdown, StringComparison.OrdinalIgnoreCase);

        var files = new[]
        {
            PolicyMarkdownPath,
            PolicyJsonPath,
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "engineering-calculation-validation-roadmap.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "engineering-calculation-validation-roadmap.json"),
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-evidence-inventory.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-evidence-inventory.json"),
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-fixture-provenance-model.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-fixture-provenance-model.json"),
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-fixture-provenance-inventory.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-fixture-provenance-inventory.json"),
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-decomposition-review.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-decomposition-review.json"),
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-component-map.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-component-map.json"),
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-design.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-design.json"),
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-risk-register.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-risk-register.json"),
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-characterization-hardening.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-characterization-hardening.json")
        };

        var scanner = new EngineeringClaimBoundaryScanner();
        var result = scanner.ScanRepository(
            repositoryRoot: TestPaths.RepoRoot,
            explicitFiles: files);

        Assert.Equal(0, result.ErrorCount);
    }

    private static string PolicyMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-claims-policy.md");

    private static string PolicyJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-claims-policy.json");

    private static string PolicySchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-claims-policy.schema.json");
}
