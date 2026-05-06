using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Services.Governance;

namespace AssistantEngineer.Tests.Calculations.Governance;

public sealed class EngineeringGovernanceTraceabilityTests
{
    [Fact]
    public void GovernanceDocsAndManifests_Exist()
    {
        var docs = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "governance", "EngineeringStageManifestRegistry.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "governance", "EngineeringClaimBoundaryScanner.md"),
            Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "governance", "EngineeringCoreV2ReleaseReadiness.md")
        };

        var manifests = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "docs", "releases", "EngineeringGovernanceStageManifestRegistryManifest.json"),
            Path.Combine(TestPaths.RepoRoot, "docs", "releases", "EngineeringClaimBoundaryScannerManifest.json"),
            Path.Combine(TestPaths.RepoRoot, "docs", "releases", "EngineeringCoreV2ReleaseReadinessManifest.json"),
            Path.Combine(TestPaths.RepoRoot, "docs", "releases", "EngineeringCorporateStatusSampleManifest.json")
        };

        Assert.All(docs, file => Assert.True(File.Exists(file), $"Missing governance doc: {file}"));
        Assert.All(manifests, file => Assert.True(File.Exists(file), $"Missing governance manifest: {file}"));
    }

    [Fact]
    public void TargetedDisclosureFiles_DoNotContainPositiveForbiddenClaims()
    {
        var scanner = new EngineeringClaimBoundaryScanner();
        var result = scanner.ScanRepository(
            repositoryRoot: TestPaths.RepoRoot,
            explicitFiles:
            [
                Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "governance", "EngineeringStageManifestRegistry.md"),
                Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "governance", "EngineeringClaimBoundaryScanner.md"),
                Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "governance", "EngineeringCoreV2ReleaseReadiness.md"),
                Path.Combine(TestPaths.RepoRoot, "docs", "api", "engineering-core-v2", "engineering-release-readiness.sample.json"),
                Path.Combine(TestPaths.RepoRoot, "docs", "api", "engineering-core-v2", "status.sample.json")
            ]);

        Assert.Equal(0, result.ErrorCount);
    }

    [Fact]
    public void GovernanceManifests_DeclareEmptyGeneratedArtifacts()
    {
        var manifestPaths = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "docs", "releases", "EngineeringGovernanceStageManifestRegistryManifest.json"),
            Path.Combine(TestPaths.RepoRoot, "docs", "releases", "EngineeringClaimBoundaryScannerManifest.json"),
            Path.Combine(TestPaths.RepoRoot, "docs", "releases", "EngineeringCoreV2ReleaseReadinessManifest.json"),
            Path.Combine(TestPaths.RepoRoot, "docs", "releases", "EngineeringCorporateStatusSampleManifest.json")
        };

        foreach (var manifestPath in manifestPaths)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
            var generatedArtifacts = document.RootElement.GetProperty("generatedArtifacts");
            Assert.Equal(JsonValueKind.Array, generatedArtifacts.ValueKind);
            Assert.Equal(0, generatedArtifacts.GetArrayLength());
        }
    }

    [Fact]
    public void GovernanceTool_DoesNotAddPowerShellWrappers()
    {
        var ps1Files = Directory.GetFiles(
            Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EngineeringGovernance"),
            "*.ps1",
            SearchOption.AllDirectories);

        Assert.Empty(ps1Files);
    }
}
