using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity.FormulaAudit;

public class EngineeringCoreV1CiProfileWorkflowTests
{
    [Fact]
    public void CiProfileWorkflowFilesAndDocumentationExist()
    {
        var requiredFiles = new[]
        {
            SmokeWorkflowPath,
            ContractsWorkflowPath,
            ReleaseReadyWorkflowPath,
            CiProfilesDocumentPath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(requiredFile),
                $"Required Engineering Core V1 CI profile artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void SmokeWorkflowRunsSmokeProfileWithDotnetNodeFrontendAndBackendRestore()
    {
        var content = File.ReadAllText(SmokeWorkflowPath);

        Assert.Contains("Engineering Core V1 Smoke", content, StringComparison.Ordinal);
        Assert.Contains("pull_request:", content, StringComparison.Ordinal);
        Assert.Contains("push:", content, StringComparison.Ordinal);
        Assert.Contains("workflow_dispatch:", content, StringComparison.Ordinal);
        Assert.Contains("windows-latest", content, StringComparison.Ordinal);
        Assert.Contains("actions/setup-dotnet@v4", content, StringComparison.Ordinal);
        Assert.Contains("dotnet-version: 10.0.x", content, StringComparison.Ordinal);
        Assert.Contains("actions/setup-node@v4", content, StringComparison.Ordinal);
        Assert.Contains("node-version: 22", content, StringComparison.Ordinal);
        Assert.Contains("dotnet restore .\\AssistantEngineer.sln", content, StringComparison.Ordinal);
        Assert.Contains("npm ci --prefix .\\src\\Frontend", content, StringComparison.Ordinal);
        Assert.Contains(".\\scripts\\engineering-core\\verify-engineering-core-v1-smoke.ps1", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ContractsWorkflowRunsContractsProfileAndFailsOnStaleGeneratedArtifacts()
    {
        var content = File.ReadAllText(ContractsWorkflowPath);

        Assert.Contains("Engineering Core V1 Contracts", content, StringComparison.Ordinal);
        Assert.Contains("pull_request:", content, StringComparison.Ordinal);
        Assert.Contains("workflow_dispatch:", content, StringComparison.Ordinal);
        Assert.Contains(".\\scripts\\engineering-core\\verify-engineering-core-v1-contracts.ps1", content, StringComparison.Ordinal);
        Assert.Contains("Generated artifacts are stale", content, StringComparison.Ordinal);
        Assert.Contains("regenerate-engineering-core-v1-artifacts.ps1", content, StringComparison.Ordinal);
        Assert.Contains("git status --short", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseReadyWorkflowRunsReleaseReadinessGateOnDispatchAndTags()
    {
        var content = File.ReadAllText(ReleaseReadyWorkflowPath);

        Assert.Contains("Engineering Core V1 Release Ready", content, StringComparison.Ordinal);
        Assert.Contains("workflow_dispatch:", content, StringComparison.Ordinal);
        Assert.Contains("engineering-core-v1*", content, StringComparison.Ordinal);
        Assert.Contains(".\\scripts\\engineering-core\\assert-engineering-core-v1-release-ready.ps1", content, StringComparison.Ordinal);
        Assert.Contains("git status --short", content, StringComparison.Ordinal);
    }

    [Fact]
    public void CiProfilesDocumentExplainsSmokeContractsReleaseReadyAndOriginalFullWorkflow()
    {
        var content = File.ReadAllText(CiProfilesDocumentPath);

        var requiredPhrases = new[]
        {
            "Engineering Core V1 CI Profiles",
            "Smoke workflow",
            "Contracts workflow",
            "Release readiness workflow",
            ".github/workflows/engineering-core-v1-smoke.yml",
            ".github/workflows/engineering-core-v1-contracts.yml",
            ".github/workflows/engineering-core-v1-release-ready.yml",
            ".github/workflows/engineering-core-v1.yml",
            "generated artifacts are stale",
            "Recommended PR policy"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(
                requiredPhrase,
                content,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void CiProfilesDocumentKeepsNonClaimsVisible()
    {
        var content = File.ReadAllText(CiProfilesDocumentPath);

        var requiredNonClaims = new[]
        {
            "exact EnergyPlus numerical parity",
            "exact pyBuildingEnergy numerical parity",
            "ASHRAE 140 validation coverage",
            "full ISO 52016 node/matrix solver parity",
            "latent/moisture/humidity support in V1"
        };

        foreach (var requiredNonClaim in requiredNonClaims)
        {
            Assert.Contains(
                requiredNonClaim,
                content,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void MainVerificationScriptIncludesCiProfileWorkflowGuardTests()
    {
        var content = File.ReadAllText(MainVerificationScriptPath);

        Assert.Contains(
            "EngineeringCoreV1CiProfileWorkflowTests",
            content,
            StringComparison.Ordinal);
    }

    private static string SmokeWorkflowPath =>
        Path.Combine(TestPaths.RepoRoot, ".github", "workflows", "engineering-core-v1-smoke.yml");

    private static string ContractsWorkflowPath =>
        Path.Combine(TestPaths.RepoRoot, ".github", "workflows", "engineering-core-v1-contracts.yml");

    private static string ReleaseReadyWorkflowPath =>
        Path.Combine(TestPaths.RepoRoot, ".github", "workflows", "engineering-core-v1-release-ready.yml");

    private static string CiProfilesDocumentPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "ci", "EngineeringCoreV1CIProfiles.md");

    private static string MainVerificationScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "verify-engineering-core-v1.ps1");
}
