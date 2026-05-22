using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Trace;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow;

public sealed class EngineeringWorkflowTracePreviewService : IEngineeringWorkflowTracePreviewService
{
    private readonly ICalculationTraceBuilder _traceBuilder;
    private readonly ICalculationTraceSanitizer _traceSanitizer;

    public EngineeringWorkflowTracePreviewService(
        ICalculationTraceBuilder traceBuilder,
        ICalculationTraceSanitizer traceSanitizer)
    {
        _traceBuilder = traceBuilder;
        _traceSanitizer = traceSanitizer;
    }

    public CalculationTraceDetailLevel ParseDetailLevel(string? detailLevel)
    {
        if (!string.IsNullOrWhiteSpace(detailLevel) &&
            Enum.TryParse<CalculationTraceDetailLevel>(detailLevel, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return CalculationTraceDetailLevel.Standard;
    }

    public CalculationTraceDocument BuildTraceDocument(
        EngineeringWorkflowStateDto state,
        CalculationTraceDetailLevel detailLevel,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics)
    {
        _traceBuilder.SetDetailLevel(detailLevel);
        _traceBuilder.Initialize(
            traceId: $"workflow-trace-{state.ProjectId}-{state.BuildingId?.ToString() ?? "none"}",
            calculationType: "EngineeringWorkflowPreview",
            rootModule: CalculationTraceModuleKind.Generic,
            calculationId: state.CalculationTraceSummary?.CalculationId ?? state.BuildingId?.ToString(),
            metadata: new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["workflow.mode"] = "api",
                ["workflow.step"] = state.CurrentStep
            });

        var projectStepId = _traceBuilder.AddStep(
            moduleKind: CalculationTraceModuleKind.Validation,
            stepName: "Project and building context",
            formulaOrConventionLabel: "Workflow foundation context aggregation");

        _traceBuilder.AddInputValue(projectStepId, new CalculationTraceValue(
            Key: "project_id",
            Label: "Project id",
            Value: state.ProjectId,
            Unit: null,
            ValueKind: CalculationTraceValueKind.Input));

        _traceBuilder.AddInputValue(projectStepId, new CalculationTraceValue(
            Key: "building_id",
            Label: "Building id",
            Value: state.BuildingId,
            Unit: null,
            ValueKind: CalculationTraceValueKind.Input));

        _traceBuilder.AddOutputValue(projectStepId, new CalculationTraceValue(
            Key: "zones_count",
            Label: "Zones count",
            Value: state.Zones.Count,
            Unit: null,
            ValueKind: CalculationTraceValueKind.Output));

        _traceBuilder.AddOutputValue(projectStepId, new CalculationTraceValue(
            Key: "boundaries_count",
            Label: "Boundary count",
            Value: state.Boundaries.Count,
            Unit: null,
            ValueKind: CalculationTraceValueKind.Output));

        var diagnosticsStepId = _traceBuilder.AddStep(
            moduleKind: CalculationTraceModuleKind.Validation,
            stepName: "Validation diagnostics aggregation",
            formulaOrConventionLabel: "Deterministic severity-sorted diagnostics merge");

        foreach (var diagnostic in diagnostics)
        {
            _traceBuilder.AddDiagnostic(diagnosticsStepId, new CalculationTraceDiagnostic(
                Severity: ParseTraceSeverity(diagnostic.Severity),
                Code: diagnostic.Code,
                Message: diagnostic.Message,
                ModuleKind: ParseTraceModuleKind(diagnostic.SourceStep),
                Context: diagnostic.TargetField,
                Source: diagnostic.SourceModule));
        }

        var reportStepId = _traceBuilder.AddStep(
            moduleKind: CalculationTraceModuleKind.Reporting,
            stepName: "Report preview readiness",
            formulaOrConventionLabel: "Workflow foundation report orchestration");

        _traceBuilder.AddOutputValue(reportStepId, new CalculationTraceValue(
            Key: "current_step",
            Label: "Current step",
            Value: state.CurrentStep,
            Unit: null,
            ValueKind: CalculationTraceValueKind.Output));

        _traceBuilder.AddOutputValue(reportStepId, new CalculationTraceValue(
            Key: "diagnostics_count",
            Label: "Diagnostics count",
            Value: diagnostics.Count,
            Unit: null,
            ValueKind: CalculationTraceValueKind.Output));

        foreach (var assumption in state.Assumptions)
        {
            _traceBuilder.AddDocumentAssumption(assumption);
        }

        foreach (var warning in diagnostics
                     .Where(diagnostic => diagnostic.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
                     .Select(diagnostic => diagnostic.Message)
                     .Distinct(StringComparer.Ordinal)
                     .Order(StringComparer.Ordinal))
        {
            _traceBuilder.AddDocumentWarning(warning);
        }

        foreach (var diagnostic in diagnostics)
        {
            _traceBuilder.AddDocumentDiagnostic(new CalculationTraceDiagnostic(
                Severity: ParseTraceSeverity(diagnostic.Severity),
                Code: diagnostic.Code,
                Message: diagnostic.Message,
                ModuleKind: ParseTraceModuleKind(diagnostic.SourceStep),
                Context: diagnostic.TargetField,
                Source: diagnostic.SourceModule));
        }

        var trace = _traceBuilder.Build();
        return _traceSanitizer.Sanitize(trace, detailLevel);
    }

    public EngineeringWorkflowTraceSummaryDto BuildTraceSummary(
        CalculationTraceDocument trace,
        string detailLevel)
    {
        var modules = trace.Steps
            .Select(step => step.ModuleKind.ToString())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var stepSummaries = trace.Steps
            .OrderBy(step => step.Sequence)
            .Select(step => new EngineeringWorkflowTraceStepSummaryDto(
                StepId: step.StepId,
                ModuleKind: step.ModuleKind.ToString(),
                StepName: step.StepName,
                Sequence: step.Sequence,
                Assumptions: step.Assumptions,
                Warnings: step.Warnings,
                DiagnosticsCount: step.Diagnostics.Count))
            .ToArray();

        return new EngineeringWorkflowTraceSummaryDto(
            TraceId: trace.TraceId,
            CalculationId: trace.CalculationId,
            DetailLevel: detailLevel,
            Modules: modules,
            Assumptions: trace.Assumptions,
            Warnings: trace.Warnings,
            Steps: stepSummaries);
    }

    private static CalculationTraceSeverity ParseTraceSeverity(string severity)
    {
        if (severity.Equals("error", StringComparison.OrdinalIgnoreCase))
        {
            return CalculationTraceSeverity.Error;
        }

        if (severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
        {
            return CalculationTraceSeverity.Warning;
        }

        if (severity.Equals("assumption", StringComparison.OrdinalIgnoreCase))
        {
            return CalculationTraceSeverity.Assumption;
        }

        if (severity.Equals("debug", StringComparison.OrdinalIgnoreCase))
        {
            return CalculationTraceSeverity.Debug;
        }

        return CalculationTraceSeverity.Info;
    }

    private static CalculationTraceModuleKind ParseTraceModuleKind(string sourceStep)
    {
        return sourceStep switch
        {
            "WeatherSolar" => CalculationTraceModuleKind.Weather,
            "Zones" => CalculationTraceModuleKind.ThermalTopology,
            "Envelope" => CalculationTraceModuleKind.ThermalTopology,
            "Ventilation" => CalculationTraceModuleKind.Ventilation,
            "Ground" => CalculationTraceModuleKind.Ground,
            "DomesticHotWater" => CalculationTraceModuleKind.DomesticHotWater,
            "SystemEnergy" => CalculationTraceModuleKind.SystemEnergy,
            "Reports" => CalculationTraceModuleKind.Reporting,
            "Validation" => CalculationTraceModuleKind.Validation,
            _ => CalculationTraceModuleKind.Generic
        };
    }
}
