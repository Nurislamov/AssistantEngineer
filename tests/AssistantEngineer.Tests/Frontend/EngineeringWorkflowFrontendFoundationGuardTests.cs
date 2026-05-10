using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Frontend;

public class EngineeringWorkflowFrontendFoundationGuardTests
{
    [Fact]
    public void WorkflowPageAndRouteAreRegistered()
    {
        var appRouter = ReadRepoFile(
            "src",
            "Frontend",
            "src",
            "app",
            "router",
            "AppRouter.tsx");

        var paths = ReadRepoFile(
            "src",
            "Frontend",
            "src",
            "app",
            "router",
            "paths.ts");

        var sidebar = ReadRepoFile(
            "src",
            "Frontend",
            "src",
            "widgets",
            "app-sidebar",
            "ui",
            "AppSidebar.tsx");

        Assert.Contains("engineeringWorkflow", paths, StringComparison.Ordinal);
        Assert.Contains("EngineeringWorkflowPage", appRouter, StringComparison.Ordinal);
        Assert.Contains("paths.engineeringWorkflow", appRouter, StringComparison.Ordinal);
        Assert.Contains("Workflow", sidebar, StringComparison.Ordinal);
        Assert.Contains("paths.engineeringWorkflow", sidebar, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkflowClientExposesRequiredMethodsAndExplicitModes()
    {
        var client = ReadRepoFile(
            "src",
            "Frontend",
            "src",
            "entities",
            "engineering-workflow",
            "api",
            "engineeringWorkflowClient.ts");

        Assert.Contains("interface EngineeringWorkflowClient", client, StringComparison.Ordinal);
        Assert.Contains("getWorkflowState", client, StringComparison.Ordinal);
        Assert.Contains("validateWorkflow", client, StringComparison.Ordinal);
        Assert.Contains("buildCalculationRequest", client, StringComparison.Ordinal);
        Assert.Contains("prepareCalculation", client, StringComparison.Ordinal);
        Assert.Contains("runCalculation", client, StringComparison.Ordinal);
        Assert.Contains("getScenarioResult", client, StringComparison.Ordinal);
        Assert.Contains("listProjectScenarios", client, StringComparison.Ordinal);
        Assert.Contains("getScenarioArtifacts", client, StringComparison.Ordinal);
        Assert.Contains("getScenarioArtifact", client, StringComparison.Ordinal);
        Assert.Contains("getTracePreview", client, StringComparison.Ordinal);
        Assert.Contains("generateReport", client, StringComparison.Ordinal);
        Assert.Contains("exportReportJson", client, StringComparison.Ordinal);
        Assert.Contains("exportReportMarkdown", client, StringComparison.Ordinal);
        Assert.Contains("mode: \"api\" | \"dev\"", client, StringComparison.Ordinal);
        Assert.Contains("internal dev adapter", client, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("apiRoutes.engineeringWorkflow.state", client, StringComparison.Ordinal);
        Assert.Contains("apiRoutes.engineeringWorkflow.validate()", client, StringComparison.Ordinal);
        Assert.Contains("apiRoutes.engineeringWorkflow.prepareCalculation()", client, StringComparison.Ordinal);
        Assert.Contains("apiRoutes.engineeringWorkflow.runCalculation()", client, StringComparison.Ordinal);
        Assert.Contains("apiRoutes.engineeringWorkflow.projectScenarios", client, StringComparison.Ordinal);
        Assert.Contains("apiRoutes.engineeringWorkflow.scenarioById", client, StringComparison.Ordinal);
        Assert.Contains("apiRoutes.engineeringWorkflow.scenarioArtifacts", client, StringComparison.Ordinal);
        Assert.Contains("apiRoutes.engineeringWorkflow.scenarioArtifactByKind", client, StringComparison.Ordinal);
        Assert.Contains("apiRoutes.engineeringWorkflow.tracePreview()", client, StringComparison.Ordinal);
        Assert.Contains("apiRoutes.engineeringWorkflow.report()", client, StringComparison.Ordinal);
        Assert.Contains("apiRoutes.engineeringWorkflow.reportExportJson()", client, StringComparison.Ordinal);
        Assert.Contains("apiRoutes.engineeringWorkflow.reportExportMarkdown()", client, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkflowShellContainsMainStepSequenceAndStatusBadges()
    {
        var shell = ReadRepoFile(
            "src",
            "Frontend",
            "src",
            "widgets",
            "engineering-workflow",
            "ui",
            "EngineeringWorkflowShell.tsx");
        var historyPanel = ReadRepoFile(
            "src",
            "Frontend",
            "src",
            "widgets",
            "engineering-workflow",
            "ui",
            "EngineeringScenarioHistoryPanel.tsx");

        Assert.Contains("\"Project\"", shell, StringComparison.Ordinal);
        Assert.Contains("\"Building\"", shell, StringComparison.Ordinal);
        Assert.Contains("\"Zones\"", shell, StringComparison.Ordinal);
        Assert.Contains("\"Envelope\"", shell, StringComparison.Ordinal);
        Assert.Contains("\"WeatherSolar\"", shell, StringComparison.Ordinal);
        Assert.Contains("\"Ventilation\"", shell, StringComparison.Ordinal);
        Assert.Contains("\"Ground\"", shell, StringComparison.Ordinal);
        Assert.Contains("\"DomesticHotWater\"", shell, StringComparison.Ordinal);
        Assert.Contains("\"SystemEnergy\"", shell, StringComparison.Ordinal);
        Assert.Contains("\"Validation\"", shell, StringComparison.Ordinal);
        Assert.Contains("\"CalculationTrace\"", shell, StringComparison.Ordinal);
        Assert.Contains("\"Reports\"", shell, StringComparison.Ordinal);
        Assert.Contains("\"Review\"", shell, StringComparison.Ordinal);

        Assert.Contains("Incomplete", shell, StringComparison.Ordinal);
        Assert.Contains("Valid", shell, StringComparison.Ordinal);
        Assert.Contains("Warnings", shell, StringComparison.Ordinal);
        Assert.Contains("Errors", shell, StringComparison.Ordinal);
        Assert.Contains("Ready", shell, StringComparison.Ordinal);
        Assert.Contains("Run available modules", shell, StringComparison.Ordinal);
        Assert.Contains("Module execution status", shell, StringComparison.Ordinal);
        Assert.Contains("Scenario history", historyPanel, StringComparison.Ordinal);
        Assert.Contains("Artifacts", historyPanel, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkflowDiagnosticsPanelSupportsSeverityGroupingAndSuggestedCorrections()
    {
        var panel = ReadRepoFile(
            "src",
            "Frontend",
            "src",
            "widgets",
            "engineering-workflow",
            "ui",
            "WorkflowDiagnosticsPanel.tsx");

        Assert.Contains("Severity filter", panel, StringComparison.Ordinal);
        Assert.Contains("suggestedCorrection", panel, StringComparison.Ordinal);
        Assert.Contains("sourceStep", panel, StringComparison.Ordinal);
        Assert.Contains("Target field", panel, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkflowTracePanelSupportsSummaryStandardDetailedAndCompactView()
    {
        var panel = ReadRepoFile(
            "src",
            "Frontend",
            "src",
            "widgets",
            "engineering-workflow",
            "ui",
            "CalculationTracePanel.tsx");

        Assert.Contains("Summary", panel, StringComparison.Ordinal);
        Assert.Contains("Standard", panel, StringComparison.Ordinal);
        Assert.Contains("Detailed", panel, StringComparison.Ordinal);
        Assert.Contains("slice", panel, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Detailed JSON endpoint pending backend wiring", panel, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkflowReportPreviewContainsJsonAndMarkdownExportActions()
    {
        var preview = ReadRepoFile(
            "src",
            "Frontend",
            "src",
            "widgets",
            "engineering-workflow",
            "ui",
            "EngineeringReportPreview.tsx");

        Assert.Contains("Export JSON", preview, StringComparison.Ordinal);
        Assert.Contains("Export Markdown", preview, StringComparison.Ordinal);
        Assert.Contains("JSON output", preview, StringComparison.Ordinal);
        Assert.Contains("Markdown output", preview, StringComparison.Ordinal);
        Assert.Contains("Limitations", preview, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkflowClientExplicitlyAvoidsFrontendPhysicsRecalculation()
    {
        var client = ReadRepoFile(
            "src",
            "Frontend",
            "src",
            "entities",
            "engineering-workflow",
            "api",
            "engineeringWorkflowClient.ts");

        Assert.DoesNotContain("Fourier", client, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("enthalpy", client, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("psychrometric", client, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("matrix solver", client, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("does not execute calculation physics in browser", ReadRepoFile(
            "src",
            "Frontend",
            "src",
            "pages",
            "engineering-workflow",
            "ui",
            "EngineeringWorkflowPage.tsx"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WorkflowFrontendDocsExistAndIncludeRequiredLimitations()
    {
        var doc = ReadRepoFile(
            "docs",
            "frontend",
            "engineering-workflow.md");

        Assert.Contains("frontend workflow is foundation-level", doc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not all production endpoints may be wired yet", doc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("frontend does not prove calculation validity", doc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("report preview summarizes current internal engineering calculations only", doc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not a legal compliance certificate", doc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not external validation evidence", doc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not prove full standard compliance", doc, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WorkflowFrontendDoesNotUseUnsupportedComplianceBadgesOrClaims()
    {
        var files = new[]
        {
            ReadRepoFile("src", "Frontend", "src", "pages", "engineering-workflow", "ui", "EngineeringWorkflowPage.tsx"),
            ReadRepoFile("src", "Frontend", "src", "widgets", "engineering-workflow", "ui", "EngineeringWorkflowShell.tsx"),
            ReadRepoFile("docs", "frontend", "engineering-workflow.md"),
        };

        var combined = string.Join(Environment.NewLine, files);

        Assert.DoesNotContain("full compliance", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fully compliant", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("validated/compliant", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("certified", combined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WorkflowHookHandlesMissingStateWithoutCrashByReturningControlledDiagnostic()
    {
        var hook = ReadRepoFile(
            "src",
            "Frontend",
            "src",
            "entities",
            "engineering-workflow",
            "model",
            "useEngineeringWorkflow.ts");

        Assert.Contains("WORKFLOW_STATE_MISSING", hook, StringComparison.Ordinal);
        Assert.Contains("status: \"blocked\"", hook, StringComparison.Ordinal);
        Assert.Contains("runCalculation", hook, StringComparison.Ordinal);
        Assert.Contains("listScenarios", hook, StringComparison.Ordinal);
        Assert.Contains("getScenarioResult", hook, StringComparison.Ordinal);
        Assert.Contains("getScenarioArtifacts", hook, StringComparison.Ordinal);
        Assert.Contains("getScenarioArtifact", hook, StringComparison.Ordinal);
        Assert.Contains("FailedValidation", hook, StringComparison.Ordinal);
        Assert.Contains("resolveWorkflowMode", hook, StringComparison.Ordinal);
    }

    private static string ReadRepoFile(params string[] parts)
    {
        var path = Path.Combine(parts.Prepend(TestPaths.RepoRoot).ToArray());

        Assert.True(
            File.Exists(path),
            $"Expected file does not exist: {path}");

        return File.ReadAllText(path);
    }
}
