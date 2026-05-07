using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Frontend;

public class EngineeringCoreFrontendIntegrationGuardTests
{
    [Fact]
    public void FrontendCalculationTypesExposeEngineeringCoreStatusDto()
    {
        var content = ReadFrontendFile(
            "src",
            "Frontend",
            "src",
            "entities",
            "calculation",
            "types.ts");

        Assert.Contains(
            "EngineeringCoreV1StatusApiResponse",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "EngineeringCoreV1GateStatusApiResponse",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "formulaGatesClosed",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "weather8760GatesClosed",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "annualHourly8760GateClosed",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "explicitNonClaims",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "outOfScopeV1",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "requiredAnnual8760Flags",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void FrontendApiRoutesExposeEngineeringCoreV1StatusEndpoint()
    {
        var content = ReadFrontendFile(
            "src",
            "Frontend",
            "src",
            "shared",
            "api",
            "apiRoutes.ts");

        Assert.Contains(
            "engineeringCoreV1Status",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "/calculations/engineering-core/v1/status",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void FrontendCalculationsApiExposesEngineeringCoreV1StatusCall()
    {
        var content = ReadFrontendFile(
            "src",
            "Frontend",
            "src",
            "entities",
            "calculation",
            "api",
            "calculationsApi.ts");

        Assert.Contains(
            "getEngineeringCoreV1Status",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "EngineeringCoreV1StatusApiResponse",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "apiRoutes.calculations.engineeringCoreV1Status()",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void FrontendQueryKeysExposeEngineeringCoreV1StatusKey()
    {
        var content = ReadFrontendFile(
            "src",
            "Frontend",
            "src",
            "shared",
            "api",
            "queryKeys.ts");

        Assert.Contains(
            "engineeringCoreV1Status",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "engineering-core",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void FrontendHookUsesEngineeringCoreV1StatusApiAndQueryKey()
    {
        var content = ReadFrontendFile(
            "src",
            "Frontend",
            "src",
            "entities",
            "calculation",
            "model",
            "useEngineeringCoreStatus.ts");

        Assert.Contains(
            "useEngineeringCoreStatus",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "calculationsApi.getEngineeringCoreV1Status",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "queryKeys.calculations.engineeringCoreV1Status",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void DashboardPageRendersEngineeringCoreStatusPanel()
    {
        var content = ReadFrontendFile(
            "src",
            "Frontend",
            "src",
            "pages",
            "dashboard",
            "ui",
            "DashboardPage.tsx");

        Assert.Contains(
            "EngineeringCoreStatusPanel",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "@/widgets/engineering-core-status/ui/EngineeringCoreStatusPanel",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void EngineeringCoreStatusPanelShowsClosedV1GatesAndNonClaims()
    {
        var content = ReadFrontendFile(
            "src",
            "Frontend",
            "src",
            "widgets",
            "engineering-core-status",
            "ui",
            "EngineeringCoreStatusPanel.tsx");

        Assert.Contains(
            "ClosedV1",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "formulaGatesClosed",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "weather8760GatesClosed",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "annualHourly8760GateClosed",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "requiredAnnual8760Flags",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "explicitNonClaims",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "outOfScopeV1",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "EnergyPlus",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "pyBuildingEnergy",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "ASHRAE 140",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void BuildingWorkspaceRendersEngineeringCoreDisclosurePanelBeforeRawReportJson()
    {
        var workspaceContent = ReadFrontendFile(
            "src",
            "Frontend",
            "src",
            "widgets",
            "building-workspace",
            "ui",
            "BuildingWorkspace.tsx");
        var reportsPanelContent = ReadFrontendFile(
            "src",
            "Frontend",
            "src",
            "widgets",
            "building-workspace",
            "ui",
            "ReportsPanel.tsx");

        var content = string.Join(Environment.NewLine, workspaceContent, reportsPanelContent);

        Assert.Contains(
            "EngineeringCoreDisclosurePanel",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "@/widgets/engineering-core-disclosure/ui/EngineeringCoreDisclosurePanel",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "report={report}",
            content,
            StringComparison.Ordinal);

        var disclosureIndex = content.IndexOf(
            "EngineeringCoreDisclosurePanel",
            StringComparison.Ordinal);

        var reportJsonIndex = content.IndexOf(
            "JsonBlock title=\"Report\"",
            StringComparison.Ordinal);

        Assert.True(
            disclosureIndex >= 0,
            "BuildingWorkspace must render EngineeringCoreDisclosurePanel.");

        Assert.True(
            reportJsonIndex >= 0,
            "BuildingWorkspace must still render raw report JSON.");

        Assert.True(
            disclosureIndex < reportJsonIndex,
            "EngineeringCoreDisclosurePanel must appear before raw report JSON so warnings/non-claims are visible before debug output.");
    }

    [Fact]
    public void EngineeringCoreDisclosurePanelShowsWarningsAssumptionsNonClaimsOutOfScopeAndDocs()
    {
        var content = ReadFrontendFile(
            "src",
            "Frontend",
            "src",
            "widgets",
            "engineering-core-disclosure",
            "ui",
            "EngineeringCoreDisclosurePanel.tsx");

        Assert.Contains(
            "CalculationDisclosureApiResponse",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "calculationDisclosure",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "warnings",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "assumptions",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "explicitNonClaims",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "outOfScopeV1",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "documentationFiles",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "Engineering Core disclosure",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void FrontendEngineeringCoreDocumentationFilesExist()
    {
        var requiredFiles = new[]
        {
            Path.Combine("docs", "frontend", "EngineeringCoreV1StatusPanel.md"),
            Path.Combine("docs", "frontend", "EngineeringCoreV1ReportDisclosurePanel.md"),
            Path.Combine("docs", "calculations", "EngineeringCoreV1ApiExamples.md"),
            Path.Combine("docs", "calculations", "EngineeringCoreV1DeveloperGuide.md"),
            Path.Combine("docs", "releases", "EngineeringCoreV1.md")
        };

        foreach (var relativePath in requiredFiles)
        {
            var fullPath = Path.Combine(TestPaths.RepoRoot, relativePath);

            Assert.True(
                File.Exists(fullPath),
                $"Required frontend/project documentation file is missing: {fullPath}");
        }
    }

    [Fact]
    public void FrontendDocsRequireWarningsNonClaimsVisibleInNormalUi()
    {
        var statusPanelDocs = ReadRepoFile(
            "docs",
            "frontend",
            "EngineeringCoreV1StatusPanel.md");

        var disclosurePanelDocs = ReadRepoFile(
            "docs",
            "frontend",
            "EngineeringCoreV1ReportDisclosurePanel.md");

        Assert.Contains(
            "must not be hidden behind debug-only UI",
            statusPanelDocs,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "must not be hidden only inside raw JSON",
            disclosurePanelDocs,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "no exact EnergyPlus numerical parity claim",
            disclosurePanelDocs,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "no ASHRAE 140 validation coverage claim",
            disclosurePanelDocs,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadFrontendFile(params string[] parts) =>
        ReadRepoFile(parts);

    private static string ReadRepoFile(params string[] parts)
    {
        var path = Path.Combine(
            parts.Prepend(TestPaths.RepoRoot).ToArray());

        Assert.True(
            File.Exists(path),
            $"Expected file does not exist: {path}");

        return File.ReadAllText(path);
    }
}
