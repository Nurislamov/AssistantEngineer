using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Validation.EnergyPlus;

public class EnergyPlusRealFixtureIntakeGateTests
{
    [Fact]
    public void RealFixtureIntakePolicyChecklistScriptAndReadinessReportExist()
    {
        var requiredFiles = new[]
        {
            IntakePolicyPath,
            EpSmoke001ChecklistPath,
            RealFixtureReadinessScriptPath,
            RealFixtureReadinessReportPath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(requiredFile),
                $"Required EnergyPlus real fixture intake artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void IntakePolicyDocumentsRequiredFilesProvenanceComparisonAndNonClaims()
    {
        var content = File.ReadAllText(IntakePolicyPath);

        var requiredPhrases = new[]
        {
            "EnergyPlus Real Fixture Intake Policy",
            "source EnergyPlus model file",
            "weather file",
            "raw EnergyPlus output file",
            "normalized reference output JSON",
            "provenance metadata",
            "Comparison requirements",
            "NumericWithinTolerance",
            "DirectionalTrend",
            "SameSign",
            "does not claim exact EnergyPlus numerical equivalence",
            "does not claim ASHRAE 140 / BESTEST-style validation anchor coverage",
            "assert-ep-smoke-001-real-fixture-ready.ps1 -RequireRealFixture"
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
    public void EpSmoke001ChecklistDocumentsFutureFilesProvenanceReferenceOutputAndStrictGate()
    {
        var content = File.ReadAllText(EpSmoke001ChecklistPath);

        var requiredPhrases = new[]
        {
            "PlaceholderComparison",
            "energyplus-model.idf",
            "weather.epw",
            "energyplus-output.raw.csv",
            "energyplus-output.reference.json",
            "provenance.json",
            "EnergyPlus version",
            "referenceStatus = RealEnergyPlusOutput",
            "comparison script reads real reference output when available",
            "assert-ep-smoke-001-real-fixture-ready.ps1 -RequireRealFixture",
            "exact EnergyPlus numerical equivalence",
            "ASHRAE 140 / BESTEST-style validation anchor coverage"
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
    public void ReadinessScriptSupportsRequireRealFixtureAndChecksExpectedFiles()
    {
        var content = File.ReadAllText(RealFixtureReadinessScriptPath);

        var requiredPhrases = new[]
        {
            "[switch] $RequireRealFixture",
            "case-metadata.json",
            "assistantengineer-input.json",
            "reference-output.placeholder.json",
            "comparison-tolerances.json",
            "energyplus-model.idf",
            "weather.epw",
            "energyplus-output.raw.csv",
            "energyplus-output.reference.json",
            "provenance.json",
            "NotReadyRealFixtureMissingFiles",
            "ReadyForRealComparison"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(
                requiredPhrase,
                content,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void GeneratedReadinessReportShowsNotReadyAndMissingRealFixtureFiles()
    {
        var content = File.ReadAllText(RealFixtureReadinessReportPath);

        Assert.Contains("EP-SMOKE-001 Real Fixture Readiness", content, StringComparison.Ordinal);
        Assert.Contains("NotReadyRealFixtureMissingFiles", content, StringComparison.Ordinal);
        Assert.Contains("energyplus-model.idf", content, StringComparison.Ordinal);
        Assert.Contains("energyplus-output.reference.json", content, StringComparison.Ordinal);
        Assert.Contains("Missing real fixture files", content, StringComparison.Ordinal);
        Assert.Contains("Missing real fixture files do not fail Engineering Core V1 closure", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PlaceholderComparison is not real EnergyPlus validation", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MainVerificationScriptIncludesRealFixtureIntakeGateTests()
    {
        var content = File.ReadAllText(MainVerificationScriptPath);

        Assert.Contains(
            "EnergyPlusRealFixtureIntakeGateTests",
            content,
            StringComparison.Ordinal);
    }

    private static string IntakePolicyPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "EnergyPlusRealFixtureIntakePolicy.md");

    private static string EpSmoke001ChecklistPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "fixtures", "EP-SMOKE-001", "RealEnergyPlusFixtureIntakeChecklist.md");

    private static string RealFixtureReadinessScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "assert-ep-smoke-001-real-fixture-ready.ps1");

    private static string RealFixtureReadinessReportPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "reports", "validation", "EP-SMOKE-001-RealFixtureReadiness.md");

    private static string MainVerificationScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "verify-engineering-core-v1.ps1");
}
