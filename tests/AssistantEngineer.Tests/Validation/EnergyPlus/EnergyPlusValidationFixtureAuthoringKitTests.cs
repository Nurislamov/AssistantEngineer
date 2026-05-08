using System.Text.Json;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Validation.EnergyPlus;

public class EnergyPlusValidationFixtureAuthoringKitTests
{
    [Fact]
    public void TemplateFilesScaffoldScriptAndAuthoringGuideExist()
    {
        var requiredFiles = new[]
        {
            CaseMetadataTemplatePath,
            AssistantEngineerInputTemplatePath,
            PlaceholderReferenceTemplatePath,
            ComparisonTolerancesTemplatePath,
            ProvenanceTemplatePath,
            RealReferenceTemplatePath,
            ReadmeTemplatePath,
            ScaffoldScriptPath,
            AuthoringGuidePath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(requiredFile),
                $"Required fixture authoring artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void TemplatesContainCaseIdTokenAndRequiredNonClaims()
    {
        foreach (var templatePath in TemplatePaths)
        {
            var content = File.ReadAllText(templatePath);

            Assert.Contains("{{CASE_ID}}", content, StringComparison.Ordinal);

            Assert.Contains(
                "EnergyPlus",
                content,
                StringComparison.OrdinalIgnoreCase);

            Assert.Contains(
                "ASHRAE 140",
                content,
                StringComparison.OrdinalIgnoreCase);
        }

        var combined = string.Join(Environment.NewLine, TemplatePaths.Select(File.ReadAllText));

        Assert.Contains("Does not claim exact EnergyPlus numerical equivalence.", combined, StringComparison.Ordinal);
        Assert.Contains("Does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.", combined, StringComparison.Ordinal);
        Assert.Contains("Does not claim full ISO 52016 node/matrix solver equivalence.", combined, StringComparison.Ordinal);
    }

    [Fact]
    public void JsonTemplatesAreValidAfterTokenReplacement()
    {
        var replacements = new Dictionary<string, string>
        {
            ["{{CASE_ID}}"] = "EP-SMOKE-999",
            ["{{CASE_NAME}}"] = "Template validation case",
            ["{{STAGE}}"] = "Smoke",
            ["{{PURPOSE}}"] = "Template purpose",
            ["{{WEATHER_SOURCE}}"] = "Synthetic weather fixture.",
            ["{{GEOMETRY_DESCRIPTION}}"] = "Geometry description.",
            ["{{ENVELOPE_DESCRIPTION}}"] = "Envelope description.",
            ["{{WEATHER_PROFILE}}"] = "synthetic",
            ["{{INTERNAL_GAINS_DESCRIPTION}}"] = "Internal gains.",
            ["{{VENTILATION_DESCRIPTION}}"] = "Ventilation.",
            ["{{HVAC_CONTROL_DESCRIPTION}}"] = "HVAC control.",
            ["{{EXPECTED_BEHAVIOR_1}}"] = "Expected behavior one.",
            ["{{EXPECTED_BEHAVIOR_2}}"] = "Expected behavior two.",
            ["{{EXPECTED_BEHAVIOR_3}}"] = "Expected behavior three.",
            ["{{CALCULATION_SCOPE}}"] = "Calculation scope.",
            ["{{PRIMARY_METRIC_FORMULA}}"] = "Formula.",
            ["{{ENERGYPLUS_VERSION}}"] = "23.2",
            ["{{OPERATING_SYSTEM}}"] = "Windows",
            ["{{RUN_DATE_UTC}}"] = "2026-01-01T00:00:00Z",
            ["{{OUTPUT_VARIABLE_1}}"] = "Output variable 1",
            ["{{OUTPUT_VARIABLE_2}}"] = "Output variable 2",
            ["{{UNIT_CONVERSION_1}}"] = "Unit conversion"
        };

        foreach (var templatePath in JsonTemplatePaths)
        {
            var content = File.ReadAllText(templatePath);

            foreach (var replacement in replacements)
            {
                content = content.Replace(replacement.Key, replacement.Value);
            }

            using var document = JsonDocument.Parse(content);

            Assert.Equal(
                "EP-SMOKE-999",
                document.RootElement.GetProperty("caseId").GetString());
        }
    }

    [Fact]
    public void ComparisonToleranceTemplateContainsSupportedMetricTypesAndPaths()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(ComparisonTolerancesTemplatePath));
        var root = document.RootElement;

        var metrics = root.GetProperty("metrics").EnumerateArray().ToArray();

        Assert.NotEmpty(metrics);

        var types = metrics
            .Select(item => item.GetProperty("type").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("NumericWithinTolerance", types);
        Assert.Contains("DirectionalTrend", types);

        foreach (var metric in metrics)
        {
            Assert.False(string.IsNullOrWhiteSpace(metric.GetProperty("assistantEngineerPath").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(metric.GetProperty("referencePath").GetString()));
            Assert.True(metric.GetProperty("tolerancePercent").GetDouble() >= 0);
            Assert.True(metric.GetProperty("absoluteTolerance").GetDouble() >= 0);
        }
    }

    [Fact]
    public void ProvenanceTemplateDocumentsRealEnergyPlusRequiredMetadata()
    {
        var content = File.ReadAllText(ProvenanceTemplatePath);

        var requiredPhrases = new[]
        {
            "RealEnergyPlusOutput",
            "energyPlusVersion",
            "operatingSystem",
            "runDateUtc",
            "sourceModelFile",
            "weatherFile",
            "rawOutputFile",
            "normalizedReferenceOutputFile",
            "outputVariables",
            "unitConversions",
            "knownDifferences",
            "tolerancePolicy",
            "requiredNonClaims"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ScaffoldScriptUsesTemplatesValidatesCaseIdAndExplainsNextSteps()
    {
        var script = File.ReadAllText(ScaffoldScriptPath);

        Assert.Contains("new-energyplus-validation-fixture.ps1", script, StringComparison.Ordinal);
        Assert.Contains("CaseId", script, StringComparison.Ordinal);
        Assert.Contains("Name", script, StringComparison.Ordinal);
        Assert.Contains("Stage", script, StringComparison.Ordinal);
        Assert.Contains("Force", script, StringComparison.Ordinal);
        Assert.Contains("AssistantEngineer.Tools.EnergyPlusFixtureAuthoring.csproj", script, StringComparison.Ordinal);
        Assert.Contains("new-fixture", script, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project", script, StringComparison.Ordinal);

        Assert.DoesNotContain("function Expand-Template", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Set-Content $DestinationPath", script, StringComparison.Ordinal);

        var tool = File.ReadAllText(FixtureAuthoringToolProgramPath);

        Assert.Contains("case-metadata.template.json", tool, StringComparison.Ordinal);
        Assert.Contains("assistantengineer-input.template.json", tool, StringComparison.Ordinal);
        Assert.Contains("reference-output.placeholder.template.json", tool, StringComparison.Ordinal);
        Assert.Contains("comparison-tolerances.template.json", tool, StringComparison.Ordinal);
        Assert.Contains("README.template.md", tool, StringComparison.Ordinal);
        Assert.Contains("EnergyPlusValidationCaseRegistry.json", tool, StringComparison.Ordinal);
        Assert.Contains("compare-energyplus-validation-fixtures.ps1", tool, StringComparison.Ordinal);
        Assert.Contains("generate-energyplus-validation-fixture-catalog.ps1", tool, StringComparison.Ordinal);
    }
    [Fact]
    public void AuthoringGuideDocumentsTemplateFolderScaffoldCommandRegistryUpdateGenerationRealReferenceAndNonClaims()
    {
        var content = File.ReadAllText(AuthoringGuidePath);

        var requiredPhrases = new[]
        {
            "EnergyPlus Validation Fixture Authoring Guide",
            "docs/validation/fixtures/_template",
            "new-energyplus-validation-fixture.ps1",
            "Required generated fixture files",
            "Required registry update",
            "Required local generation",
            "Future real EnergyPlus reference",
            "provenance.template.json",
            "energyplus-output.reference.template.json",
            "PlaceholderComparison is not real EnergyPlus validation",
            "future real validation must remain tolerance-based",
            "EnergyPlusValidationFixtureAuthoringKitTests"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(requiredPhrase, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void MainVerificationProfileIncludesAuthoringKitGuardTests()
    {
        var wrapper = File.ReadAllText(MainVerificationScriptPath);

        Assert.Contains("AssistantEngineer.Tools.EngineeringCoreVerification.csproj", wrapper, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project", wrapper, StringComparison.Ordinal);

        var tool = File.ReadAllText(VerificationToolProgramPath);

        Assert.Contains("EnergyPlusValidationFixtureAuthoringKitTests", tool, StringComparison.Ordinal);
    }
    private static string TemplateRoot =>
            Path.Combine(TestPaths.RepoRoot, "docs", "validation", "fixtures", "_template");

    private static string CaseMetadataTemplatePath =>
        Path.Combine(TemplateRoot, "case-metadata.template.json");

    private static string AssistantEngineerInputTemplatePath =>
        Path.Combine(TemplateRoot, "assistantengineer-input.template.json");

    private static string PlaceholderReferenceTemplatePath =>
        Path.Combine(TemplateRoot, "reference-output.placeholder.template.json");

    private static string ComparisonTolerancesTemplatePath =>
        Path.Combine(TemplateRoot, "comparison-tolerances.template.json");

    private static string ProvenanceTemplatePath =>
        Path.Combine(TemplateRoot, "provenance.template.json");

    private static string RealReferenceTemplatePath =>
        Path.Combine(TemplateRoot, "energyplus-output.reference.template.json");

    private static string ReadmeTemplatePath =>
        Path.Combine(TemplateRoot, "README.template.md");

    private static string ScaffoldScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "new-energyplus-validation-fixture.ps1");

    private static string AuthoringGuidePath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "EnergyPlusValidationFixtureAuthoringGuide.md");

    private static string MainVerificationScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "verify-engineering-core-v1.ps1");

    private static string[] TemplatePaths =>
    [
        CaseMetadataTemplatePath,
        AssistantEngineerInputTemplatePath,
        PlaceholderReferenceTemplatePath,
        ComparisonTolerancesTemplatePath,
        ProvenanceTemplatePath,
        RealReferenceTemplatePath,
        ReadmeTemplatePath
    ];

    private static string[] JsonTemplatePaths =>
    [
        CaseMetadataTemplatePath,
        AssistantEngineerInputTemplatePath,
        PlaceholderReferenceTemplatePath,
        ComparisonTolerancesTemplatePath,
        ProvenanceTemplatePath,
        RealReferenceTemplatePath
    ];
    private static string FixtureAuthoringToolProgramPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "tools",
            "AssistantEngineer.Tools.EnergyPlusFixtureAuthoring",
            "Program.cs");

    private static string VerificationToolProgramPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "tools",
            "AssistantEngineer.Tools.EngineeringCoreVerification",
            "Program.cs");
}

