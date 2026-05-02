using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity.FormulaAudit;

public class EngineeringCoreV1VerificationRunbookTests
{
    [Fact]
    public void VerificationScriptExists()
    {
        Assert.True(
            File.Exists(VerificationScriptPath),
            $"Engineering Core V1 verification script must exist: {VerificationScriptPath}");
    }

    [Fact]
    public void VerificationRunbookExists()
    {
        Assert.True(
            File.Exists(VerificationRunbookPath),
            $"Engineering Core V1 verification runbook must exist: {VerificationRunbookPath}");
    }

    [Fact]
    public void VerificationScriptRunsFrontendBuildUnlessSkipped()
    {
        var content = ReadVerificationScript();

        Assert.Contains(
            "SkipFrontend",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "npm --prefix .\\src\\Frontend run build",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void VerificationScriptRunsFormulaAuditAndEngineeringCoreStatusTests()
    {
        var content = ReadVerificationScript();

        Assert.Contains(
            "FormulaAudit|EngineeringCoreStatus|EngineeringCoreReportDisclosureTests",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "EngineeringCoreFrontendIntegrationGuardTests",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "EnergyPlusValidation",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void VerificationScriptRunsWeatherAnnualHourlyZoneGroundAndAdjacentGateTests()
    {
        var content = ReadVerificationScript();

        var requiredFilters = new[]
        {
            "EpwAnnualClimateDataImportServiceTests",
            "PvgisAnnualClimateDataImportServiceTests",
            "AnnualEnergy8760ScenarioTests",
            "Iso52016EngineeringCoreV1ClosureTests",
            "GroundSimplifiedEngineeringCoreV1ClosureTests",
            "AdjacentZoneSimplifiedEngineeringCoreV1ClosureTests"
        };

        foreach (var requiredFilter in requiredFilters)
        {
            Assert.Contains(
                requiredFilter,
                content,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void VerificationScriptRunsFullDotnetSuiteByDefault()
    {
        var content = ReadVerificationScript();

        Assert.Contains(
            "SkipFullDotnet",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "dotnet test .\\AssistantEngineer.sln",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "Full backend test suite",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void VerificationRunbookDocumentsMainCommandAndFastMode()
    {
        var content = ReadVerificationRunbook();

        Assert.Contains(
            ".\\scripts\\engineering-core\\verify-engineering-core-v1.ps1",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "-Fast",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "-SkipFrontend",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "-SkipFullDotnet",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void VerificationRunbookDocumentsWhatIsVerified()
    {
        var content = ReadVerificationRunbook();

        var requiredPhrases = new[]
        {
            "Engineering Core V1 formula gates remain ClosedV1",
            "EPW and PVGIS weather import gates normalize to 8760 records",
            "annual energy has a true hourly 8760 scenario",
            "heating/cooling reports expose calculationDisclosure",
            "frontend displays Engineering Core V1 status and report disclosures",
            "EnergyPlus/ASHRAE 140 validation remains a future comparative harness"
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
    public void VerificationRunbookLinksRelatedEngineeringCoreDocuments()
    {
        var content = ReadVerificationRunbook();

        var requiredDocs = new[]
        {
            "docs/calculations/EngineeringCoreV1Scope.md",
            "docs/calculations/EngineeringCoreV1ReleaseNotes.md",
            "docs/calculations/EngineeringCoreV1ApiExamples.md",
            "docs/calculations/EngineeringCoreV1DeveloperGuide.md",
            "docs/calculations/EnergyPlusAshrae140ValidationPlan.md",
            "docs/validation/EnergyPlusAshrae140ValidationHarness.md",
            "docs/frontend/EngineeringCoreV1StatusPanel.md",
            "docs/frontend/EngineeringCoreV1ReportDisclosurePanel.md",
            "docs/releases/EngineeringCoreV1.md"
        };

        foreach (var requiredDoc in requiredDocs)
        {
            Assert.Contains(
                requiredDoc,
                content,
                StringComparison.Ordinal);
        }
    }

    private static string VerificationScriptPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "scripts",
            "engineering-core",
            "verify-engineering-core-v1.ps1");

    private static string VerificationRunbookPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "EngineeringCoreV1VerificationRunbook.md");

    private static string ReadVerificationScript() =>
        File.ReadAllText(VerificationScriptPath);

    private static string ReadVerificationRunbook() =>
        File.ReadAllText(VerificationRunbookPath);
}
