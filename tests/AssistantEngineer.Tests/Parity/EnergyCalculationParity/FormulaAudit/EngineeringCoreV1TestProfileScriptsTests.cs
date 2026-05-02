using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity.FormulaAudit;

public class EngineeringCoreV1TestProfileScriptsTests
{
    [Fact]
    public void TestProfileScriptsAndRunbookExist()
    {
        var requiredFiles = new[]
        {
            RegenerateArtifactsScriptPath,
            SmokeVerificationScriptPath,
            ContractsVerificationScriptPath,
            TestProfilesRunbookPath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(requiredFile),
                $"Required Engineering Core V1 test profile artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void RegenerateArtifactsScriptRunsAllKnownGenerators()
    {
        var content = File.ReadAllText(RegenerateArtifactsScriptPath);

        var requiredGenerators = new[]
        {
            "generate-engineering-core-v1-release-evidence.ps1",
            "generate-engineering-core-v1-api-contract-snapshots.ps1",
            "generate-engineering-core-v1-report-contract-snapshots.ps1",
            "generate-engineering-core-v1-export-disclosure-checklist.ps1",
            "generate-engineering-core-v1-validation-readiness.ps1",
            "generate-engineering-core-v1-traceability-matrix.ps1"
        };

        foreach (var requiredGenerator in requiredGenerators)
        {
            Assert.Contains(
                requiredGenerator,
                content,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void SmokeVerificationScriptRunsFocusedEngineeringCoreChecks()
    {
        var content = File.ReadAllText(SmokeVerificationScriptPath);

        Assert.Contains("SkipFrontend", content, StringComparison.Ordinal);
        Assert.Contains("npm --prefix .\\src\\Frontend run build", content, StringComparison.Ordinal);
        Assert.Contains("FormulaAudit", content, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreStatus", content, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreReportDisclosureTests", content, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreDiagnosticsCatalogFacadeAndApiTests", content, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreFrontendIntegrationGuardTests", content, StringComparison.Ordinal);
        Assert.Contains("AnnualEnergy8760ScenarioTests", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Full backend test suite", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ContractsVerificationScriptRunsGeneratedArtifactAndContractGuards()
    {
        var content = File.ReadAllText(ContractsVerificationScriptPath);

        Assert.Contains("SkipFrontend", content, StringComparison.Ordinal);
        Assert.Contains("SkipRegenerate", content, StringComparison.Ordinal);
        Assert.Contains("regenerate-engineering-core-v1-artifacts.ps1", content, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreV1ApiContractSnapshotTests", content, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreV1OpenApiContractTests", content, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreV1ReportContractSnapshotTests", content, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreV1ReportExportDisclosureGuardTests", content, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreV1ReleaseEvidencePackageTests", content, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreV1TraceabilityMatrixTests", content, StringComparison.Ordinal);
        Assert.Contains("EnergyPlusValidationCaseRegistryTests", content, StringComparison.Ordinal);
    }

    [Fact]
    public void TestProfilesRunbookDocumentsSmokeContractsFastAndFullProfiles()
    {
        var content = File.ReadAllText(TestProfilesRunbookPath);

        var requiredPhrases = new[]
        {
            "Profile 1: Smoke",
            "Profile 2: Contracts",
            "Profile 3: Fast full engineering-core verification",
            "Profile 4: Full verification",
            "Artifact regeneration only",
            "verify-engineering-core-v1-smoke.ps1",
            "verify-engineering-core-v1-contracts.ps1",
            "verify-engineering-core-v1.ps1 -Fast",
            "regenerate-engineering-core-v1-artifacts.ps1",
            "Do not remove guards to make tests faster"
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
    public void MainVerificationScriptStillContainsFullAndFastModes()
    {
        var content = File.ReadAllText(MainVerificationScriptPath);

        Assert.Contains("Fast", content, StringComparison.Ordinal);
        Assert.Contains("SkipFullDotnet", content, StringComparison.Ordinal);
        Assert.Contains("Full backend test suite", content, StringComparison.Ordinal);
        Assert.Contains("Engineering Core V1 verification completed successfully", content, StringComparison.Ordinal);
    }

    private static string RegenerateArtifactsScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "regenerate-engineering-core-v1-artifacts.ps1");

    private static string SmokeVerificationScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "verify-engineering-core-v1-smoke.ps1");

    private static string ContractsVerificationScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "verify-engineering-core-v1-contracts.ps1");

    private static string MainVerificationScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "verify-engineering-core-v1.ps1");

    private static string TestProfilesRunbookPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "runbooks", "EngineeringCoreV1TestProfiles.md");
}
