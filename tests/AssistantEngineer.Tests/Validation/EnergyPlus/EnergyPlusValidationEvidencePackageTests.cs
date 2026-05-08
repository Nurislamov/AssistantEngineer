using System.Text.Json;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Validation.EnergyPlus;

public class EnergyPlusValidationEvidencePackageTests
{
    [Fact]
    public void ValidationEvidenceScriptGeneratedFilesAndGuideExist()
    {
        var requiredFiles = new[]
        {
            EvidenceScriptPath,
            EvidenceJsonPath,
            EvidenceMarkdownPath,
            EvidenceGuidePath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(requiredFile),
                $"Required validation evidence artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void ValidationEvidenceDeclaresPlannedValidationAndSourceFiles()
    {
        using var document = ReadJson(EvidenceJsonPath);
        var root = document.RootElement;

        Assert.Equal("Engineering Core V1 Validation Evidence", root.GetProperty("evidenceName").GetString());
        Assert.Equal("v1", root.GetProperty("version").GetString());
        Assert.Equal("PlannedValidation", root.GetProperty("status").GetString());

        var sources = root.GetProperty("sources");

        Assert.Equal(
            "docs/validation/EnergyPlusValidationCaseRegistry.json",
            sources.GetProperty("registry").GetString());

        Assert.Equal(
            "docs/validation/EnergyPlusValidationFixtureCatalog.json",
            sources.GetProperty("fixtureCatalog").GetString());

        Assert.Equal(
            "docs/reports/validation/EnergyPlusValidationGenericComparisonSummary.json",
            sources.GetProperty("genericComparisonSummary").GetString());

        Assert.Equal(
            "docs/reports/validation/EngineeringCoreV1ValidationComparisonSummary.json",
            sources.GetProperty("validationComparisonSummary").GetString());
    }

    [Fact]
    public void ValidationEvidenceTotalsReflectSmokeFixturesAndPlaceholderComparisons()
    {
        using var document = ReadJson(EvidenceJsonPath);
        var totals = document.RootElement.GetProperty("totals");

        Assert.True(totals.GetProperty("registryCases").GetInt32() >= 5);
        Assert.True(totals.GetProperty("registrySmokeCases").GetInt32() >= 3);
        Assert.True(totals.GetProperty("fixtureCatalogCases").GetInt32() >= 3);
        Assert.True(totals.GetProperty("genericRunnerFixturesDiscovered").GetInt32() >= 3);
        Assert.True(totals.GetProperty("genericRunnerComparisonsGenerated").GetInt32() >= 3);
        Assert.True(totals.GetProperty("validationSummaryCasesWithComparison").GetInt32() >= 3);
        Assert.True(totals.GetProperty("placeholderComparisons").GetInt32() >= 3);
        Assert.Equal(0, totals.GetProperty("realEnergyPlusComparisons").GetInt32());
        Assert.True(totals.GetProperty("passingComparisons").GetInt32() >= 3);
        Assert.Equal(0, totals.GetProperty("missingEvidenceFiles").GetInt32());
    }

    [Fact]
    public void ValidationEvidenceContainsSmoke001002003Cases()
    {
        using var document = ReadJson(EvidenceJsonPath);

        var cases = document
            .RootElement
            .GetProperty("cases")
            .EnumerateArray()
            .ToArray();

        var caseIds = cases
            .Select(item => item.GetProperty("caseId").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("EP-SMOKE-001", caseIds);
        Assert.Contains("EP-SMOKE-002", caseIds);
        Assert.Contains("EP-SMOKE-003", caseIds);

        foreach (var caseId in new[] { "EP-SMOKE-001", "EP-SMOKE-002", "EP-SMOKE-003" })
        {
            var validationCase = cases.Single(item => item.GetProperty("caseId").GetString() == caseId);

            Assert.True(validationCase.GetProperty("registryListed").GetBoolean());
            Assert.Equal("Smoke", validationCase.GetProperty("registryStage").GetString());
            Assert.Equal("PlaceholderComparison", validationCase.GetProperty("comparisonStatus").GetString());
            Assert.Equal("PlaceholderReferenceOutput", validationCase.GetProperty("referenceStatus").GetString());
            Assert.True(validationCase.GetProperty("allMetricsPassed").GetBoolean());
            Assert.True(validationCase.GetProperty("metricCount").GetInt32() >= 3);
            Assert.True(validationCase.GetProperty("hasFixtureReadme").GetBoolean());
            Assert.True(validationCase.GetProperty("hasComparisonJson").GetBoolean());
            Assert.True(validationCase.GetProperty("hasComparisonMarkdown").GetBoolean());
            Assert.False(validationCase.GetProperty("hasRealReference").GetBoolean());
        }
    }

    [Fact]
    public void ValidationEvidenceFilesAllExist()
    {
        using var document = ReadJson(EvidenceJsonPath);

        var missing = document
            .RootElement
            .GetProperty("evidenceFiles")
            .EnumerateArray()
            .Where(item => !item.GetProperty("exists").GetBoolean())
            .Select(item => item.GetProperty("path").GetString() ?? string.Empty)
            .ToArray();

        Assert.Empty(missing);
    }

    [Fact]
    public void ValidationEvidenceKeepsRequiredNonClaimsAndNextMilestonesVisible()
    {
        using var document = ReadJson(EvidenceJsonPath);
        var root = document.RootElement;

        var nonClaims = root
            .GetProperty("requiredNonClaims")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(nonClaims, claim => claim.Contains("Does not claim exact EnergyPlus", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, claim => claim.Contains("Does not claim exact StandardReference", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, claim => claim.Contains("Does not claim ASHRAE 140", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, claim => claim.Contains("Does not claim full ISO 52016", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, claim => claim.Contains("PlaceholderComparison is not real EnergyPlus validation", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(nonClaims, claim => claim.Contains("Future real validation must remain tolerance-based", StringComparison.OrdinalIgnoreCase));

        var milestones = root
            .GetProperty("nextMilestones")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(milestones, item => item.Contains("first real EnergyPlus model and output for EP-SMOKE-001", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(milestones, item => item.Contains("provenance.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(milestones, item => item.Contains("RealEnergyPlusComparison", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidationEvidenceMarkdownDocumentsStatusCasesFilesNonClaimsAndMilestones()
    {
        var content = File.ReadAllText(EvidenceMarkdownPath);

        var requiredPhrases = new[]
        {
            "Engineering Core V1 Validation Evidence",
            "PlannedValidation",
            "EP-SMOKE-001",
            "EP-SMOKE-002",
            "EP-SMOKE-003",
            "PlaceholderComparison",
            "Real EnergyPlus comparisons | 0",
            "Evidence files",
            "Required non-claims",
            "Next milestones",
            "does not claim exact EnergyPlus comparison workflow",
            "does not claim ASHRAE 140 / BESTEST-style validation anchor coverage"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void EvidenceGuideDocumentsPurposeGenerationStatusMeaningNextMilestoneAndGuardTests()
    {
        var content = File.ReadAllText(EvidenceGuidePath);

        var requiredPhrases = new[]
        {
            "Engineering Core V1 Validation Evidence Guide",
            "generate-engineering-core-v1-validation-evidence.ps1",
            "regenerate-engineering-core-v1-validation-artifacts.ps1",
            "PlannedValidation",
            "PlaceholderComparison",
            "Current real EnergyPlus comparison count",
            "What this evidence proves",
            "What this evidence does not prove",
            "Add first real EnergyPlus model/output for EP-SMOKE-001",
            "EnergyPlusValidationEvidencePackageTests"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void EvidenceScriptReadsAllValidationSourcesAndWritesJsonAndMarkdown()
    {
        var content = File.ReadAllText(EvidenceScriptPath);

        var requiredPhrases = new[]
        {
            "EnergyPlusValidationCaseRegistry.json",
            "EnergyPlusValidationFixtureCatalog.json",
            "EnergyPlusValidationGenericComparisonSummary.json",
            "EngineeringCoreV1ValidationComparisonSummary.json",
            "EP-SMOKE-001-RealFixtureReadiness.md",
            "EngineeringCoreV1ValidationReadiness.md",
            "EngineeringCoreV1ValidationEvidence.json",
            "EngineeringCoreV1ValidationEvidence.md",
            "PlaceholderComparison",
            "Future real validation must remain tolerance-based"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ValidationRegenerateScriptRunsEvidenceGenerator()
    {
        var content = File.ReadAllText(ValidationRegenerateScriptPath);

        Assert.Contains("generate-engineering-core-v1-validation-evidence.ps1", content, StringComparison.Ordinal);
    }

    [Fact]
    public void MainVerificationScriptIncludesValidationEvidenceTests()
    {
        var content = File.ReadAllText(MainVerificationScriptPath);

        Assert.Contains("EnergyPlusValidationEvidencePackageTests", content, StringComparison.Ordinal);
    }

    private static JsonDocument ReadJson(string path) =>
        JsonDocument.Parse(File.ReadAllText(path));

    private static string EvidenceScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "generate-engineering-core-v1-validation-evidence.ps1");

    private static string EvidenceJsonPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "reports", "validation", "EngineeringCoreV1ValidationEvidence.json");

    private static string EvidenceMarkdownPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "reports", "validation", "EngineeringCoreV1ValidationEvidence.md");

    private static string EvidenceGuidePath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "EnergyPlusValidationEvidenceGuide.md");

    private static string ValidationRegenerateScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "regenerate-engineering-core-v1-validation-artifacts.ps1");

    private static string MainVerificationScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "verify-engineering-core-v1.ps1");
}
