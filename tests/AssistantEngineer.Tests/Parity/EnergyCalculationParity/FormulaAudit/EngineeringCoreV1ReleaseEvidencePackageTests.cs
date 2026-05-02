using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity.FormulaAudit;

public class EngineeringCoreV1ReleaseEvidencePackageTests
{
    [Fact]
    public void ReleaseEvidenceGenerationScriptExists()
    {
        Assert.True(
            File.Exists(ReleaseEvidenceScriptPath),
            $"Release evidence generation script must exist: {ReleaseEvidenceScriptPath}");
    }

    [Fact]
    public void OperationalRunbookTroubleshootingAdrAndIndexExist()
    {
        var requiredFiles = new[]
        {
            OperationalRunbookPath,
            TroubleshootingPath,
            AdrPath,
            DocumentationIndexPath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(requiredFile),
                $"Required release evidence package file is missing: {requiredFile}");
        }
    }

    [Fact]
    public void ReleaseEvidenceScriptReadsManifestAndDiagnosticsCatalogAndWritesReport()
    {
        var content = File.ReadAllText(ReleaseEvidenceScriptPath);

        Assert.Contains(
            "EngineeringCoreV1Manifest.json",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "EngineeringCoreV1DiagnosticsCatalog.json",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "EngineeringCoreV1ReleaseEvidence.md",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "Closed formula gates",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "Diagnostics by category",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedReleaseEvidenceReportExistsAndContainsSummary()
    {
        Assert.True(
            File.Exists(ReleaseEvidenceReportPath),
            $"Release evidence report must exist. Generate it with scripts/engineering-core/generate-engineering-core-v1-release-evidence.ps1: {ReleaseEvidenceReportPath}");

        var content = File.ReadAllText(ReleaseEvidenceReportPath);

        Assert.Contains(
            "Engineering Core V1 Release Evidence",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "Closed formula gates",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "Diagnostics catalog",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "Explicit non-claims",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "Required verification command",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void OperationalRunbookDocumentsHealthVerificationStatusAndSupportProcedure()
    {
        var content = File.ReadAllText(OperationalRunbookPath);

        var requiredPhrases = new[]
        {
            "Daily verification",
            "Health indicators",
            "GET /api/v1/calculations/engineering-core/v1/status",
            "GET /api/v1/calculations/engineering-core/v1/diagnostics-catalog",
            "generate-engineering-core-v1-release-evidence.ps1",
            "Support procedure",
            "Escalate as future work"
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
    public void TroubleshootingGuideDocumentsCommonFrontendFormulaAnnualDiagnosticsManifestAndCiFailures()
    {
        var content = File.ReadAllText(TroubleshootingPath);

        var requiredPhrases = new[]
        {
            "Frontend build fails after route changes",
            "DashboardPage.tsx",
            "EngineeringCoreDisclosurePanel",
            "FormulaAuditMatrix test fails",
            "Annual 8760 claim looks wrong",
            "Diagnostics catalog test fails",
            "Manifest consistency fails",
            "CI fails but local fast verification passes",
            "Do not fix by weakening claims"
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
    public void AdrDefinesEngineeringCoreV1ClosurePolicyAllowedClaimsAndForbiddenClaims()
    {
        var content = File.ReadAllText(AdrPath);

        Assert.Contains("Accepted", content, StringComparison.Ordinal);
        Assert.Contains("FormulaAuditMatrix has no Partial formula items", content, StringComparison.Ordinal);
        Assert.Contains("Allowed claims", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Forbidden claims", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("exact EnergyPlus numerical parity", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ASHRAE 140 validation coverage", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("latent/moisture/humidity support in v1", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DocumentationIndexLinksCoreScopeDiagnosticsFrontendValidationOperationsAdrAndEvidence()
    {
        var content = File.ReadAllText(DocumentationIndexPath);

        var requiredLinks = new[]
        {
            "docs/releases/EngineeringCoreV1.md",
            "docs/releases/EngineeringCoreV1Manifest.json",
            "docs/calculations/EngineeringCoreV1Scope.md",
            "docs/calculations/EngineeringCoreV1DiagnosticsCatalog.json",
            "docs/frontend/EngineeringCoreV1StatusPanel.md",
            "docs/validation/EnergyPlusAshrae140ValidationHarness.md",
            "docs/runbooks/EngineeringCoreV1OperationalRunbook.md",
            "docs/troubleshooting/EngineeringCoreV1Troubleshooting.md",
            "docs/adr/0001-engineering-core-v1-closure-policy.md",
            "docs/reports/EngineeringCoreV1ReleaseEvidence.md"
        };

        foreach (var requiredLink in requiredLinks)
        {
            Assert.Contains(
                requiredLink,
                content,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void EvidencePackageKeepsRequiredNonClaimsVisible()
    {
        var combined = string.Join(
            Environment.NewLine,
            File.ReadAllText(OperationalRunbookPath),
            File.ReadAllText(TroubleshootingPath),
            File.ReadAllText(AdrPath),
            File.ReadAllText(DocumentationIndexPath));

        var requiredNonClaims = new[]
        {
            "exact EnergyPlus numerical parity",
            "exact pyBuildingEnergy numerical parity",
            "ASHRAE 140 validation coverage",
            "full ISO 52016 node/matrix solver parity",
            "latent/moisture/humidity support in v1"
        };

        foreach (var requiredNonClaim in requiredNonClaims)
        {
            Assert.Contains(
                requiredNonClaim,
                combined,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ReleaseEvidenceScriptPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "scripts",
            "engineering-core",
            "generate-engineering-core-v1-release-evidence.ps1");

    private static string ReleaseEvidenceReportPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "reports",
            "EngineeringCoreV1ReleaseEvidence.md");

    private static string OperationalRunbookPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "runbooks",
            "EngineeringCoreV1OperationalRunbook.md");

    private static string TroubleshootingPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "troubleshooting",
            "EngineeringCoreV1Troubleshooting.md");

    private static string AdrPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "adr",
            "0001-engineering-core-v1-closure-policy.md");

    private static string DocumentationIndexPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "engineering-core",
            "README.md");
}
