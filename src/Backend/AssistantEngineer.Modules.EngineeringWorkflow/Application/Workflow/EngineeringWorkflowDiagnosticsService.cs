using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;

namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow;

public sealed class EngineeringWorkflowDiagnosticsService : IEngineeringWorkflowDiagnosticsService
{
    public IReadOnlyList<EngineeringWorkflowDiagnosticDto> ValidateState(EngineeringWorkflowStateDto state)
    {
        var diagnostics = new List<EngineeringWorkflowDiagnosticDto>(state.Diagnostics);

        if (state.ProjectId <= 0)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "error",
                Code: "WORKFLOW_PROJECT_ID_INVALID",
                Message: "Project id must be greater than zero.",
                SourceStep: "Project",
                TargetField: "projectId"));
        }

        if (!state.BuildingId.HasValue || state.BuildingId <= 0)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "warning",
                Code: "WORKFLOW_BUILDING_NOT_SELECTED",
                Message: "Building is not selected.",
                SourceStep: "Building",
                TargetField: "buildingId"));
        }

        if (state.Zones.Count == 0)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "warning",
                Code: "WORKFLOW_ZONES_MISSING",
                Message: "No zones available in workflow state.",
                SourceStep: "Zones"));
        }

        if (state.Boundaries.Count == 0)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "warning",
                Code: "WORKFLOW_BOUNDARIES_MISSING",
                Message: "No boundaries available in workflow state.",
                SourceStep: "Envelope"));
        }

        if (state.GroundSettings.GroundBoundaryCount <= 0)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "warning",
                Code: "WORKFLOW_GROUND_BOUNDARY_MISSING",
                Message: "Ground settings are not configured.",
                SourceStep: "Ground"));
        }

        return SortAndDistinctDiagnostics(diagnostics);
    }

    public IReadOnlyList<EngineeringWorkflowStepDto> BuildStepStatuses(
        EngineeringWorkflowStateDto state,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics)
    {
        return EngineeringWorkflowCatalog.WorkflowSteps
            .Select(step =>
            {
                var diagnosticsForStep = diagnostics.Where(diagnostic =>
                    diagnostic.SourceStep.Equals(step, StringComparison.OrdinalIgnoreCase)).ToArray();

                var hasErrors = diagnosticsForStep.Any(diagnostic => IsErrorSeverity(diagnostic.Severity));
                var hasWarnings = diagnosticsForStep.Any(diagnostic =>
                    diagnostic.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase));

                var status = "valid";
                if (step == "Project" && state.ProjectId <= 0)
                {
                    status = "incomplete";
                }
                else if (step == "Building" && (!state.BuildingId.HasValue || state.BuildingId <= 0))
                {
                    status = "incomplete";
                }
                else if (step == "Zones" && state.Zones.Count == 0)
                {
                    status = "incomplete";
                }
                else if (step == "Envelope" && state.Boundaries.Count == 0)
                {
                    status = "incomplete";
                }
                else if (step == "Ground" && state.GroundSettings.GroundBoundaryCount <= 0)
                {
                    status = "incomplete";
                }
                else if (step == "CalculationTrace" && state.CalculationTraceSummary is null)
                {
                    status = "incomplete";
                }
                else if (step == "Reports" && state.ReportSummary is null)
                {
                    status = "incomplete";
                }

                if (hasErrors)
                {
                    status = "errors";
                }
                else if (hasWarnings && !status.Equals("incomplete", StringComparison.OrdinalIgnoreCase))
                {
                    status = "warnings";
                }

                var isComplete = status is "valid" or "warnings" or "errors";
                return new EngineeringWorkflowStepDto(step, status, isComplete);
            })
            .ToArray();
    }

    public IReadOnlyList<EngineeringWorkflowStepDto> BuildStepStatusesForMissingBuilding(
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics)
    {
        return BuildStepStatuses(new EngineeringWorkflowStateDto(
            ProjectId: 1,
            ProjectName: "n/a",
            BuildingId: null,
            CurrentStep: "Building",
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
            Diagnostics: diagnostics,
            Assumptions: [],
            Links: [],
            CalculationTraceSummary: null,
            ReportSummary: null,
            Metadata: new Dictionary<string, string>()), diagnostics);
    }

    public EngineeringWorkflowStateDto AddMissingPersistedStateDiagnostic(
        EngineeringWorkflowStateDto state,
        EngineeringWorkflowPersistenceProviderInfo providerInfo)
    {
        var persistenceMessage = providerInfo.Provider == EngineeringWorkflowPersistenceProvider.SQLite
            ? "No persisted workflow state existed for this project; deterministic foundation state was generated and persisted in SQLite provider."
            : "No persisted workflow state existed for this project; deterministic foundation state was generated and persisted in in-memory provider.";

        var diagnostics = SortAndDistinctDiagnostics(state.Diagnostics.Concat(
        [
            new EngineeringWorkflowDiagnosticDto(
                Severity: "info",
                Code: "WORKFLOW_STATE_NOT_PERSISTED_YET",
                Message: persistenceMessage,
                SourceStep: "Project",
                SuggestedCorrection: "Continue workflow edits and use validate/prepare/run endpoints to create scenario history.")
        ]));

        var metadata = state.Metadata
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        metadata["persistence"] = providerInfo.ProviderLabel;
        metadata["persistenceProvider"] = providerInfo.Provider.ToString();
        metadata["durablePersistenceEnabled"] = providerInfo.DurableEnabled ? "true" : "false";
        metadata["stateSource"] = "generated-and-persisted";

        var updated = state with
        {
            Diagnostics = diagnostics,
            Metadata = metadata
        };

        return updated with { Steps = BuildStepStatuses(updated, diagnostics) };
    }

    public IReadOnlyList<EngineeringWorkflowDiagnosticDto> SortAndDistinctDiagnostics(
        IEnumerable<EngineeringWorkflowDiagnosticDto> diagnostics)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        return diagnostics
            .OrderByDescending(diagnostic => SeverityRank(diagnostic.Severity))
            .ThenBy(diagnostic => diagnostic.SourceStep, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .Where(diagnostic => seen.Add(
                $"{diagnostic.SourceStep}|{diagnostic.Code}|{diagnostic.Message}|{diagnostic.TargetField}"))
            .ToArray();
    }

    public string SelectCurrentStep(
        IReadOnlyList<EngineeringWorkflowZoneDto> zones,
        IReadOnlyList<EngineeringWorkflowBoundaryDto> boundaries,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics)
    {
        if (!zones.Any())
        {
            return "Zones";
        }

        if (!boundaries.Any())
        {
            return "Envelope";
        }

        if (diagnostics.Any(diagnostic => IsErrorSeverity(diagnostic.Severity)))
        {
            return "Validation";
        }

        return "Review";
    }

    public string MapBoundaryIndicator(WallBoundaryTypeDto boundaryType)
    {
        return boundaryType switch
        {
            WallBoundaryTypeDto.External => "exterior",
            WallBoundaryTypeDto.Ground => "ground",
            WallBoundaryTypeDto.Adiabatic => "adiabatic",
            _ => "adjacent"
        };
    }

    public string NormalizeSeverity(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return "info";
        }

        var normalized = source.Trim();
        if (normalized.Equals("error", StringComparison.OrdinalIgnoreCase))
        {
            return "error";
        }

        if (normalized.Equals("warning", StringComparison.OrdinalIgnoreCase))
        {
            return "warning";
        }

        if (normalized.Equals("assumption", StringComparison.OrdinalIgnoreCase))
        {
            return "assumption";
        }

        return "info";
    }

    public string NormalizeSeverity(BuildingCalculationReadinessSeverity severity)
    {
        return severity switch
        {
            BuildingCalculationReadinessSeverity.Error => "error",
            BuildingCalculationReadinessSeverity.Warning => "warning",
            _ => "info"
        };
    }

    public bool IsErrorSeverity(string severity) =>
        severity.Equals("error", StringComparison.OrdinalIgnoreCase);

    private static int SeverityRank(string severity)
    {
        if (severity.Equals("error", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        if (severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (severity.Equals("assumption", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        return 1;
    }
}
