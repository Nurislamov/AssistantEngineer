using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Validation.EnergyPlus;

public class EnergyPlusValidationProfileScriptsTests
{
    [Fact]
    public void ValidationProfileScriptsWorkflowDocsExist()
    {
        var requiredFiles = new[]
        {
            RegenerateValidationArtifactsScriptPath,
            VerifyValidationProfileScriptPath,
            ValidationWorkflowPath,
            ValidationProfileRunbookPath,
            ValidationCiDocumentPath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(requiredFile),
                $"Required validation profile artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void RegenerateValidationArtifactsScriptRunsAllValidationGeneratorsInExpectedOrder()
    {
        var content = File.ReadAllText(RegenerateValidationArtifactsScriptPath);

        var requiredPhrases = new[]
        {
            "generate-engineering-core-v1-validation-readiness.ps1",
            "generate-ep-smoke-001-comparison-readiness.ps1",
            "compare-ep-smoke-001-placeholder.ps1",
            "compare-energyplus-validation-fixtures.ps1",
            "generate-engineering-core-v1-validation-comparison-summary.ps1",
            "assert-ep-smoke-001-real-fixture-ready.ps1",
            "generate-energyplus-validation-fixture-catalog.ps1",
            "RequireRealReferences"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void VerifyValidationProfileScriptRunsAllValidationGuardGroups()
    {
        var content = File.ReadAllText(VerifyValidationProfileScriptPath);

        var requiredPhrases = new[]
        {
            "SkipRegenerate",
            "RequireRealReferences",
            "regenerate-engineering-core-v1-validation-artifacts.ps1",
            "EnergyPlusValidationCaseRegistryTests",
            "EnergyPlusValidation",
            "EnergyPlusSmoke001FixtureScaffoldTests",
            "EnergyPlusSmoke001ComparisonHarnessTests",
            "EnergyPlusSmoke002And003FixtureScaffoldTests",
            "EnergyPlusValidationGenericComparisonRunnerTests",
            "EnergyPlusValidationComparisonSummaryTests",
            "EnergyPlusRealFixtureIntakeGateTests",
            "EnergyPlusValidationFixtureCatalogTests",
            "EnergyPlusValidationFixtureAuthoringKitTests"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ValidationWorkflowRunsValidationProfileAndFailsOnStaleArtifacts()
    {
        var content = File.ReadAllText(ValidationWorkflowPath);

        var requiredPhrases = new[]
        {
            "Engineering Core V1 Validation",
            "pull_request:",
            "push:",
            "workflow_dispatch:",
            "windows-latest",
            "actions/setup-dotnet@v4",
            "dotnet-version: 10.0.x",
            "dotnet restore .\\AssistantEngineer.sln",
            ".\\scripts\\engineering-core\\verify-engineering-core-v1-validation.ps1",
            "Validation generated artifacts are stale",
            "regenerate-engineering-core-v1-validation-artifacts.ps1",
            "docs/validation/**",
            "tests/fixtures/validation/**"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ValidationProfileRunbookDocumentsCommandsStrictModeCiFixturesAndNonClaims()
    {
        var content = File.ReadAllText(ValidationProfileRunbookPath);

        var requiredPhrases = new[]
        {
            "Engineering Core V1 Validation Profile",
            "verify-engineering-core-v1-validation.ps1",
            "-SkipRegenerate",
            "-RequireRealReferences",
            "regenerate-engineering-core-v1-validation-artifacts.ps1",
            ".github/workflows/engineering-core-v1-validation.yml",
            "EP-SMOKE-001",
            "EP-SMOKE-002",
            "EP-SMOKE-003",
            "PlaceholderComparison",
            "exact EnergyPlus numerical equivalence",
            "ASHRAE 140 / BESTEST-style validation anchor coverage"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ValidationCiDocumentDocumentsWorkflowTriggersStaleArtifactProtectionAndNonClaims()
    {
        var content = File.ReadAllText(ValidationCiDocumentPath);

        var requiredPhrases = new[]
        {
            "Engineering Core V1 Validation CI",
            ".github/workflows/engineering-core-v1-validation.yml",
            "verify-engineering-core-v1-validation.ps1",
            "docs/validation/**",
            "docs/reports/validation/**",
            "tests/fixtures/validation/**",
            "stale",
            "PlaceholderComparison",
            "exact EnergyPlus numerical equivalence",
            "ASHRAE 140 / BESTEST-style validation anchor coverage"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void MainVerificationScriptIncludesValidationProfileScriptGuardTests()
    {
        var content = File.ReadAllText(MainVerificationScriptPath);

        Assert.Contains("EnergyPlusValidationProfileScriptsTests", content, StringComparison.Ordinal);
    }

    private static string RegenerateValidationArtifactsScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "regenerate-engineering-core-v1-validation-artifacts.ps1");

    private static string VerifyValidationProfileScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "verify-engineering-core-v1-validation.ps1");

    private static string ValidationWorkflowPath =>
        Path.Combine(TestPaths.RepoRoot, ".github", "workflows", "engineering-core-v1-validation.yml");

    private static string ValidationProfileRunbookPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "runbooks", "EngineeringCoreV1ValidationProfile.md");

    private static string ValidationCiDocumentPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "ci", "EngineeringCoreV1ValidationCI.md");

    private static string MainVerificationScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "verify-engineering-core-v1.ps1");
}
