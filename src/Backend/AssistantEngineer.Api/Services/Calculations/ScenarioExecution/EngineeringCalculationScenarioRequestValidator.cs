using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

namespace AssistantEngineer.Api.Services.Calculations;

public sealed class EngineeringCalculationScenarioRequestValidator : IEngineeringCalculationScenarioRequestValidator
{
    public IReadOnlyList<EngineeringWorkflowDiagnosticDto> Validate(
        EngineeringCalculationScenarioRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.State);

        var diagnostics = new List<EngineeringWorkflowDiagnosticDto>();

        if (string.IsNullOrWhiteSpace(request.ScenarioId))
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "error",
                Code: "SCENARIO_ID_MISSING",
                Message: "Scenario id is required.",
                SourceStep: "Review",
                TargetField: "scenarioId"));
        }

        if (request.State.ProjectId <= 0)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "error",
                Code: "SCENARIO_PROJECT_ID_INVALID",
                Message: "Project id must be greater than zero for scenario execution.",
                SourceStep: "Project",
                TargetField: "projectId"));
        }

        if (!request.State.BuildingId.HasValue || request.State.BuildingId <= 0)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "warning",
                Code: "SCENARIO_BUILDING_ID_MISSING",
                Message: "Building id is missing; only modules independent from building persistence can run.",
                SourceStep: "Building",
                TargetField: "buildingId"));
        }

        if (request.ExecutionMode == EngineeringCalculationExecutionMode.ExecuteFullRequired)
        {
            if (request.State.Zones.Count == 0)
            {
                diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                    Severity: "error",
                    Code: "SCENARIO_ZONES_REQUIRED",
                    Message: "ExecuteFullRequired mode requires at least one zone.",
                    SourceStep: "Zones"));
            }

            if (request.State.Boundaries.Count == 0)
            {
                diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                    Severity: "error",
                    Code: "SCENARIO_BOUNDARIES_REQUIRED",
                    Message: "ExecuteFullRequired mode requires at least one boundary.",
                    SourceStep: "Envelope"));
            }
        }

        return SortAndDistinct(diagnostics);
    }

    public IReadOnlyList<EngineeringWorkflowDiagnosticDto> SortAndDistinct(
        IEnumerable<EngineeringWorkflowDiagnosticDto> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        var seen = new HashSet<string>(StringComparer.Ordinal);

        return diagnostics
            .Where(item => !string.IsNullOrWhiteSpace(item.Message))
            .OrderByDescending(item => SeverityRank(item.Severity))
            .ThenBy(item => item.SourceStep.ToString(), StringComparer.Ordinal)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.Message, StringComparer.Ordinal)
            .Where(item => seen.Add($"{item.SourceStep}|{item.Code}|{item.Message}|{item.TargetField}"))
            .ToArray();
    }

    public bool HasErrors(IEnumerable<EngineeringWorkflowDiagnosticDto> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        return diagnostics.Any(item => item.Severity.Equals("error", StringComparison.OrdinalIgnoreCase));
    }

    private static int SeverityRank(string severity)
    {
        if (severity.Equals("error", StringComparison.OrdinalIgnoreCase))
            return 4;
        if (severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
            return 3;
        if (severity.Equals("assumption", StringComparison.OrdinalIgnoreCase))
            return 2;

        return 1;
    }
}