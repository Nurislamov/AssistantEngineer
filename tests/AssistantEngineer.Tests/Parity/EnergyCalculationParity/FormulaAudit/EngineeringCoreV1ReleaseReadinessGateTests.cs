using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity.FormulaAudit;

public class EngineeringCoreV1ReleaseReadinessGateTests
{
    [Fact]
    public void ReleaseReadinessScriptRunbookAndChecklistExist()
    {
        var requiredFiles = new[]
        {
            ReleaseReadinessScriptPath,
            ReleaseReadinessRunbookPath,
            ReleaseReadinessChecklistPath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(requiredFile),
                $"Required release readiness artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void ReleaseReadinessScriptRunsRegenerateSmokeContractsManifestAndFullProfiles()
    {
        var content = File.ReadAllText(ReleaseReadinessScriptPath);

        Assert.Contains("regenerate-engineering-core-v1-artifacts.ps1", content, StringComparison.Ordinal);
        Assert.Contains("verify-engineering-core-v1-smoke.ps1", content, StringComparison.Ordinal);
        Assert.Contains("verify-engineering-core-v1-contracts.ps1", content, StringComparison.Ordinal);
        Assert.Contains("verify-engineering-core-v1-manifest.ps1", content, StringComparison.Ordinal);
        Assert.Contains("verify-engineering-core-v1.ps1", content, StringComparison.Ordinal);
        Assert.Contains("dotnet test .\\AssistantEngineer.sln", content, StringComparison.Ordinal);
        Assert.Contains("git status --short", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseReadinessScriptSupportsFastSkipFrontendSkipFullDotnetAndSkipGitStatus()
    {
        var content = File.ReadAllText(ReleaseReadinessScriptPath);

        Assert.Contains("[switch] $Fast", content, StringComparison.Ordinal);
        Assert.Contains("[switch] $SkipFrontend", content, StringComparison.Ordinal);
        Assert.Contains("[switch] $SkipFullDotnet", content, StringComparison.Ordinal);
        Assert.Contains("[switch] $SkipGitStatus", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseReadinessScriptChecksRequiredReleaseArtifacts()
    {
        var content = File.ReadAllText(ReleaseReadinessScriptPath);

        var requiredArtifacts = new[]
        {
            "EngineeringCoreV1Manifest.json",
            "EngineeringCoreV1ReleaseManifest.md",
            "EngineeringCoreV1ReleaseChecklist.md",
            "EngineeringCoreV1OwnerHandoff.md",
            "EngineeringCoreV1ReleaseEvidence.md",
            "EngineeringCoreV1TraceabilityMatrix.json",
            "EngineeringCoreV1DiagnosticsCatalog.json",
            "status.sample.json",
            "diagnostics-catalog.sample.json",
            "heating-report.sample.json",
            "cooling-report.sample.json",
            "annual-energy-disclosure.sample.json",
            "EnergyPlusValidationCaseRegistry.json",
            "EngineeringCoreV1ValidationReadiness.md",
            "engineering-core-v1.yml"
        };

        foreach (var requiredArtifact in requiredArtifacts)
        {
            Assert.Contains(
                requiredArtifact,
                content,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ReleaseReadinessRunbookDocumentsReleaseReadyMeaningAndNonClaims()
    {
        var content = File.ReadAllText(ReleaseReadinessRunbookPath);

        var requiredPhrases = new[]
        {
            "Release-ready means",
            "FormulaAuditMatrix contains no unclosed v1 formula gates",
            "Engineering Core status is ClosedV1",
            "report calculationDisclosure is visible",
            "validation registry exists as future planned validation",
            "Release-ready does not mean",
            "exact EnergyPlus numerical parity",
            "ASHRAE 140 validation coverage",
            "latent/moisture/humidity support in v1"
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
    public void ReleaseReadinessChecklistDocumentsCommandsCoreWeatherVisibilityContractsValidationAndDecision()
    {
        var content = File.ReadAllText(ReleaseReadinessChecklistPath);

        var requiredPhrases = new[]
        {
            "Commands",
            "Core closure",
            "Weather and annual energy",
            "User visibility",
            "Generated contracts",
            "Future validation",
            "Required non-claims",
            "Decision",
            "Engineering Core V1 is closed as an engineering formula gate with documented limitations",
            "EnergyPlus parity achieved",
            "ASHRAE 140 validated",
            "Full ISO 52016 implemented"
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
    public void MainVerificationScriptIncludesReleaseReadinessGuardTests()
    {
        var content = File.ReadAllText(MainVerificationScriptPath);

        Assert.Contains(
            "EngineeringCoreV1ReleaseReadinessGateTests",
            content,
            StringComparison.Ordinal);
    }

    private static string ReleaseReadinessScriptPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "scripts",
            "engineering-core",
            "assert-engineering-core-v1-release-ready.ps1");

    private static string ReleaseReadinessRunbookPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "runbooks",
            "EngineeringCoreV1ReleaseReadinessRunbook.md");

    private static string ReleaseReadinessChecklistPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "EngineeringCoreV1ReleaseReadinessChecklist.md");

    private static string MainVerificationScriptPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "scripts",
            "engineering-core",
            "verify-engineering-core-v1.ps1");
}
