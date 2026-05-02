using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Frontend;

public class EngineeringCoreDiagnosticsCatalogPanelFrontendGuardTests
{
    [Fact]
    public void DiagnosticsCatalogPanelComponentExists()
    {
        Assert.True(
            File.Exists(PanelPath),
            $"Diagnostics catalog panel component must exist: {PanelPath}");
    }

    [Fact]
    public void DiagnosticsCatalogPanelUsesHookAndDisplaysRulesCountsAnnual8760AndUserActions()
    {
        var content = File.ReadAllText(PanelPath);

        var requiredPhrases = new[]
        {
            "useEngineeringCoreDiagnosticsCatalog",
            "catalog.rules.successRule",
            "catalog.rules.error",
            "catalog.rules.warning",
            "catalog.rules.info",
            "Annual 8760 safeguards",
            "AnnualEnergy.Not8760",
            "AnnualEnergy.MonthlyBalanceAdapter",
            "SolarWeather.SyntheticWeatherUsed",
            "Blocking Error diagnostics",
            "Warnings and user actions",
            "diagnostic.userAction",
            "diagnostic.closedV1Gate"
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
    public void DashboardPageRendersDiagnosticsCatalogPanel()
    {
        var content = File.ReadAllText(DashboardPath);

        Assert.Contains(
            "EngineeringCoreDiagnosticsCatalogPanel",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "@/widgets/engineering-core-diagnostics-catalog/ui/EngineeringCoreDiagnosticsCatalogPanel",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void DashboardRendersDiagnosticsCatalogPanelAfterStatusPanelWhenStatusPanelExists()
    {
        var content = File.ReadAllText(DashboardPath);

        var statusIndex = content.IndexOf(
            "EngineeringCoreStatusPanel",
            StringComparison.Ordinal);

        var diagnosticsIndex = content.IndexOf(
            "EngineeringCoreDiagnosticsCatalogPanel",
            StringComparison.Ordinal);

        Assert.True(
            diagnosticsIndex >= 0,
            "DashboardPage must render EngineeringCoreDiagnosticsCatalogPanel.");

        if (statusIndex >= 0)
        {
            Assert.True(
                statusIndex < diagnosticsIndex,
                "Diagnostics catalog panel should be rendered after EngineeringCoreStatusPanel.");
        }
    }

    [Fact]
    public void DiagnosticsCatalogPanelDocumentationExistsAndStatesUxRules()
    {
        Assert.True(
            File.Exists(DocumentationPath),
            $"Diagnostics catalog panel documentation must exist: {DocumentationPath}");

        var content = File.ReadAllText(DocumentationPath);

        Assert.Contains(
            "GET /api/v1/calculations/engineering-core/v1/diagnostics-catalog",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "AnnualEnergy.Not8760",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "MonthlyBalanceAdapter",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "Warnings must not be hidden",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "Error diagnostics are blocking",
            content,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string PanelPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Frontend",
            "src",
            "widgets",
            "engineering-core-diagnostics-catalog",
            "ui",
            "EngineeringCoreDiagnosticsCatalogPanel.tsx");

    private static string DashboardPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Frontend",
            "src",
            "pages",
            "dashboard",
            "ui",
            "DashboardPage.tsx");

    private static string DocumentationPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "frontend",
            "EngineeringCoreV1DiagnosticsCatalogPanel.md");
}
