using System.Text.Json;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity.FormulaAudit;

public class EngineeringCoreV1ReportExportDisclosureGuardTests
{
    [Fact]
    public void ExportDisclosurePolicyTemplateChecklistAndGeneratorExist()
    {
        var requiredFiles = new[]
        {
            ExportPolicyPath,
            ExportTemplatePath,
            ExportChecklistPath,
            GeneratorScriptPath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(requiredFile),
                $"Required export disclosure artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void ExportDisclosurePolicyDocumentsAllExportSurfacesAndRequiredFields()
    {
        var content = File.ReadAllText(ExportPolicyPath);

        var requiredPhrases = new[]
        {
            "frontend report UI",
            "raw JSON exports",
            "PDF exports",
            "Excel exports",
            "calculationDisclosure.coreStatus",
            "calculationDisclosure.warnings",
            "calculationDisclosure.assumptions",
            "calculationDisclosure.explicitNonClaims",
            "calculationDisclosure.outOfScopeV1",
            "calculationDisclosure.documentationFiles"
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
    public void ExportDisclosurePolicyDocumentsAnnual8760RulesAndNonClaims()
    {
        var content = File.ReadAllText(ExportPolicyPath);

        Assert.Contains("EnergyDataSource = TrueHourlySimulation", content, StringComparison.Ordinal);
        Assert.Contains("IsTrueHourly8760 = true", content, StringComparison.Ordinal);
        Assert.Contains("HourlyRecordCount = 8760", content, StringComparison.Ordinal);

        Assert.Contains("no exact EnergyPlus numerical parity", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no exact pyBuildingEnergy numerical parity", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no ASHRAE 140 validation coverage", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no full ISO 52016 node/matrix solver parity", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no latent/moisture/humidity support in v1", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExportDisclosurePolicyDocumentsPdfExcelJsonAndFrontendRules()
    {
        var content = File.ReadAllText(ExportPolicyPath);

        var requiredPhrases = new[]
        {
            "Excel exports should include a dedicated worksheet",
            "PDF exports should include a visible disclosure section",
            "JSON exports should preserve calculationDisclosure as structured data",
            "Frontend report UI must render calculationDisclosure before raw JSON",
            "Warnings must not be hidden"
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
    public void ExportDisclosureTemplateContainsRequiredSections()
    {
        var content = File.ReadAllText(ExportTemplatePath);

        var requiredSections = new[]
        {
            "Calculation scope",
            "Main results",
            "Warnings",
            "Assumptions",
            "Explicit non-claims",
            "Out-of-scope v1",
            "Annual 8760 requirements",
            "Documentation references",
            "Export checklist"
        };

        foreach (var requiredSection in requiredSections)
        {
            Assert.Contains(
                requiredSection,
                content,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void GeneratedExportChecklistContainsSnapshotStatusExportSurfacesFieldsAndApprovalChecklist()
    {
        var content = File.ReadAllText(ExportChecklistPath);

        var requiredPhrases = new[]
        {
            "Snapshot status",
            "heating-report.sample.json",
            "cooling-report.sample.json",
            "annual-energy-disclosure.sample.json",
            "Required export surfaces",
            "Required disclosure fields",
            "Required visible sections",
            "Annual 8760 requirements",
            "Required non-claims",
            "Export approval checklist",
            "Excel exports include a visible disclosure sheet/table",
            "JSON exports preserve structured calculationDisclosure"
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
    public void ReportSnapshotsContainExportCriticalDisclosureFields()
    {
        foreach (var snapshotPath in SnapshotPaths)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(snapshotPath));
            var disclosure = document.RootElement.GetProperty("calculationDisclosure");

            var requiredProperties = new[]
            {
                "coreStatus",
                "calculationScope",
                "calculationMethod",
                "actualMethod",
                "warnings",
                "assumptions",
                "explicitNonClaims",
                "outOfScopeV1",
                "documentationFiles"
            };

            foreach (var requiredProperty in requiredProperties)
            {
                Assert.True(
                    disclosure.TryGetProperty(requiredProperty, out _),
                    $"{Path.GetFileName(snapshotPath)} disclosure is missing {requiredProperty}.");
            }

            Assert.NotEmpty(disclosure.GetProperty("warnings").EnumerateArray());
            Assert.NotEmpty(disclosure.GetProperty("assumptions").EnumerateArray());
            Assert.NotEmpty(disclosure.GetProperty("explicitNonClaims").EnumerateArray());
            Assert.NotEmpty(disclosure.GetProperty("outOfScopeV1").EnumerateArray());
            Assert.NotEmpty(disclosure.GetProperty("documentationFiles").EnumerateArray());
        }
    }

    [Fact]
    public void ReportSnapshotsKeepExportCriticalNonClaimsVisible()
    {
        foreach (var snapshotPath in SnapshotPaths)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(snapshotPath));
            var disclosure = document.RootElement.GetProperty("calculationDisclosure");

            var nonClaims = disclosure
                .GetProperty("explicitNonClaims")
                .EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .ToArray();

            Assert.Contains(nonClaims, claim => claim.Contains("No exact EnergyPlus", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(nonClaims, claim => claim.Contains("No exact pyBuildingEnergy", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(nonClaims, claim => claim.Contains("No ASHRAE 140", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(nonClaims, claim => claim.Contains("No full ISO 52016", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(nonClaims, claim => claim.Contains("No latent/moisture/humidity", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void GeneratorScriptReadsSnapshotsAndWritesChecklist()
    {
        var content = File.ReadAllText(GeneratorScriptPath);

        Assert.Contains("heating-report.sample.json", content, StringComparison.Ordinal);
        Assert.Contains("cooling-report.sample.json", content, StringComparison.Ordinal);
        Assert.Contains("annual-energy-disclosure.sample.json", content, StringComparison.Ordinal);
        Assert.Contains("ExportDisclosureChecklist.md", content, StringComparison.Ordinal);
        Assert.Contains("calculationDisclosure", content, StringComparison.Ordinal);
        Assert.Contains("No exact EnergyPlus numerical parity claim.", content, StringComparison.Ordinal);
    }

    private static string[] SnapshotPaths =>
    [
        Path.Combine(TestPaths.RepoRoot, "docs", "reports", "engineering-core-v1", "heating-report.sample.json"),
        Path.Combine(TestPaths.RepoRoot, "docs", "reports", "engineering-core-v1", "cooling-report.sample.json"),
        Path.Combine(TestPaths.RepoRoot, "docs", "reports", "engineering-core-v1", "annual-energy-disclosure.sample.json")
    ];

    private static string ExportPolicyPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "reports", "engineering-core-v1", "ExportDisclosurePolicy.md");

    private static string ExportTemplatePath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "reports", "engineering-core-v1", "ExportDisclosureTemplate.md");

    private static string ExportChecklistPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "reports", "engineering-core-v1", "ExportDisclosureChecklist.md");

    private static string GeneratorScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "generate-engineering-core-v1-export-disclosure-checklist.ps1");
}
