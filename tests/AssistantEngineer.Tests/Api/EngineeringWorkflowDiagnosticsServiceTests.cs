using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow;

namespace AssistantEngineer.Tests.Api;

public sealed class EngineeringWorkflowDiagnosticsServiceTests
{
    private readonly EngineeringWorkflowDiagnosticsService _service = new();

    [Fact]
    public void ValidateStateAddsDeterministicMissingInputDiagnostics()
    {
        var state = CreateState(projectId: 0, buildingId: null);

        var diagnostics = _service.ValidateState(state);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "WORKFLOW_PROJECT_ID_INVALID");
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "WORKFLOW_BUILDING_NOT_SELECTED");
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "WORKFLOW_ZONES_MISSING");
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "WORKFLOW_BOUNDARIES_MISSING");
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "WORKFLOW_GROUND_BOUNDARY_MISSING");
    }

    [Fact]
    public void BuildStepStatusesMarksMissingBuildingZonesEnvelopeAndGroundAsIncomplete()
    {
        var state = CreateState(projectId: 1, buildingId: null);
        var diagnostics = _service.ValidateState(state);

        var steps = _service.BuildStepStatuses(state, diagnostics);

        Assert.Contains(steps, step => step.Kind == "Building" && step.Status == "incomplete");
        Assert.Contains(steps, step => step.Kind == "Zones" && step.Status == "incomplete");
        Assert.Contains(steps, step => step.Kind == "Envelope" && step.Status == "incomplete");
        Assert.Contains(steps, step => step.Kind == "Ground" && step.Status == "incomplete");
    }

    [Fact]
    public void SortAndDistinctDiagnosticsDeduplicatesByStepCodeMessageAndTargetField()
    {
        var diagnostics = _service.SortAndDistinctDiagnostics(
        [
            new EngineeringWorkflowDiagnosticDto("warning", "DUPLICATE", "same", "Validation", TargetField: "field"),
            new EngineeringWorkflowDiagnosticDto("warning", "DUPLICATE", "same", "Validation", TargetField: "field"),
            new EngineeringWorkflowDiagnosticDto("error", "ERROR", "first", "Project")
        ]);

        Assert.Equal(2, diagnostics.Count);
        Assert.Equal("ERROR", diagnostics[0].Code);
    }

    [Fact]
    public void AddMissingPersistedStateDiagnosticAddsProviderMetadataAndRefreshesSteps()
    {
        var state = CreateState(projectId: 1, buildingId: 2);
        var providerInfo = new EngineeringWorkflowPersistenceProviderInfo(
            EngineeringWorkflowPersistenceProvider.SQLite,
            DurableEnabled: true,
            ProviderLabel: "SQLite workflow persistence");

        var updated = _service.AddMissingPersistedStateDiagnostic(state, providerInfo);

        Assert.Contains(updated.Diagnostics, diagnostic => diagnostic.Code == "WORKFLOW_STATE_NOT_PERSISTED_YET");
        Assert.Equal("SQLite workflow persistence", updated.Metadata["persistence"]);
        Assert.Equal("SQLite", updated.Metadata["persistenceProvider"]);
        Assert.Equal("true", updated.Metadata["durablePersistenceEnabled"]);
        Assert.NotEmpty(updated.Steps);
    }

    private static EngineeringWorkflowStateDto CreateState(int projectId, int? buildingId)
    {
        return new EngineeringWorkflowStateDto(
            ProjectId: projectId,
            ProjectName: "Test project",
            BuildingId: buildingId,
            CurrentStep: "Project",
            Steps: [],
            AvailableModules: EngineeringWorkflowCatalog.AvailableModules,
            BuildingMetadata: new EngineeringWorkflowBuildingMetadataDto(null, null, null, null, null, null),
            Zones: [],
            Boundaries: [],
            WeatherSolarSettings: new EngineeringWorkflowWeatherSolarSettingsDto("n/a", "n/a", "n/a"),
            VentilationSettings: new EngineeringWorkflowVentilationSettingsDto(0, "n/a", "n/a", []),
            GroundSettings: new EngineeringWorkflowGroundSettingsDto(0, "n/a", "incomplete"),
            DomesticHotWaterSettings: new EngineeringWorkflowDomesticHotWaterSettingsDto("n/a", "n/a", "n/a", "n/a"),
            SystemEnergySettings: new EngineeringWorkflowSystemEnergySettingsDto("n/a", "n/a", "n/a"),
            Diagnostics: [],
            Assumptions: [],
            Links: [],
            CalculationTraceSummary: null,
            ReportSummary: null,
            Metadata: new SortedDictionary<string, string>(StringComparer.Ordinal));
    }
}
