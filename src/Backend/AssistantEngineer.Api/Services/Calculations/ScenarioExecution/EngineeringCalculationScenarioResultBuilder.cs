using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Trace;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Api.Services.Calculations;

public sealed class EngineeringCalculationScenarioResultBuilder : IEngineeringCalculationScenarioResultBuilder
{
    private readonly ICalculationTraceBuilder _traceBuilder;
    private readonly ICalculationTraceSanitizer _traceSanitizer;
    private readonly IEngineeringReportBuilder _reportBuilder;
    private readonly IEngineeringReportJsonExporter _reportJsonExporter;
    private readonly IEngineeringReportMarkdownExporter _reportMarkdownExporter;

    public EngineeringCalculationScenarioResultBuilder(
        ICalculationTraceBuilder traceBuilder,
        ICalculationTraceSanitizer traceSanitizer,
        IEngineeringReportBuilder reportBuilder,
        IEngineeringReportJsonExporter reportJsonExporter,
        IEngineeringReportMarkdownExporter reportMarkdownExporter)
    {
        _traceBuilder = traceBuilder;
        _traceSanitizer = traceSanitizer;
        _reportBuilder = reportBuilder;
        _reportJsonExporter = reportJsonExporter;
        _reportMarkdownExporter = reportMarkdownExporter;
    }

    public CalculationTraceDocument BuildTrace(
        EngineeringCalculationScenarioRequestDto request,
        IReadOnlyList<EngineeringCalculationModuleExecutionResultDto> moduleResults,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics,
        IReadOnlyList<string> assumptions,
        IReadOnlyList<string> warnings)
    {
        var detailLevel = ParseTraceDetailLevel(request.DetailLevel);

        _traceBuilder.SetDetailLevel(detailLevel);
        _traceBuilder.Initialize(
            traceId: $"scenario-trace-{request.ScenarioId}",
            calculationType: "EngineeringCalculationScenario",
            rootModule: CalculationTraceModuleKind.Generic,
            calculationId: request.State.BuildingId?.ToString(),
            createdTimestampUtc: request.DeterministicTimestampUtc,
            metadata: new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["execution.mode"] = request.ExecutionMode.ToString(),
                ["scenario.kind"] = request.ScenarioKind.ToString()
            });

        foreach (var module in moduleResults.OrderBy(item => item.ModuleKind, StringComparer.Ordinal))
        {
            var stepId = _traceBuilder.AddStep(
                moduleKind: ParseTraceModuleKind(module.ModuleKind),
                stepName: module.ModuleKind,
                formulaOrConventionLabel: "Engineering scenario runner orchestration");

            _traceBuilder.AddOutputValue(stepId, new CalculationTraceValue(
                Key: "module_status",
                Label: "Module execution status",
                Value: module.Status.ToString(),
                Unit: null,
                ValueKind: CalculationTraceValueKind.Output));

            foreach (var value in module.SummaryValues)
            {
                _traceBuilder.AddOutputValue(stepId, new CalculationTraceValue(
                    Key: value.Key,
                    Label: value.Label,
                    Value: value.Value,
                    Unit: value.Unit is null ? null : new CalculationTraceUnit(value.Unit),
                    ValueKind: CalculationTraceValueKind.Output));
            }

            foreach (var warning in module.Warnings)
            {
                _traceBuilder.AddWarning(stepId, warning);
            }

            foreach (var assumption in module.Assumptions)
            {
                _traceBuilder.AddAssumption(stepId, assumption);
            }

            foreach (var diagnostic in module.Diagnostics)
            {
                _traceBuilder.AddDiagnostic(stepId, new CalculationTraceDiagnostic(
                    Severity: ParseTraceSeverity(diagnostic.Severity),
                    Code: diagnostic.Code,
                    Message: diagnostic.Message,
                    ModuleKind: ParseTraceModuleKind(module.ModuleKind),
                    Context: diagnostic.TargetField,
                    Source: diagnostic.SourceModule));
            }
        }

        foreach (var assumption in assumptions)
        {
            _traceBuilder.AddDocumentAssumption(assumption);
        }

        foreach (var warning in warnings)
        {
            _traceBuilder.AddDocumentWarning(warning);
        }

        foreach (var diagnostic in diagnostics)
        {
            _traceBuilder.AddDocumentDiagnostic(new CalculationTraceDiagnostic(
                Severity: ParseTraceSeverity(diagnostic.Severity),
                Code: diagnostic.Code,
                Message: diagnostic.Message,
                ModuleKind: ParseTraceModuleKindFromStep(diagnostic.SourceStep),
                Context: diagnostic.TargetField,
                Source: diagnostic.SourceModule));
        }

        var trace = _traceBuilder.Build();
        return _traceSanitizer.Sanitize(trace, detailLevel);
    }

    public EngineeringCalculationScenarioResultDto BuildScenarioResult(
        EngineeringCalculationScenarioRequestDto request,
        IReadOnlyList<EngineeringCalculationModuleExecutionResultDto> moduleResults,
        IReadOnlyList<EngineeringCalculationModuleTimingDto> timings,
        IReadOnlyList<string> executedModules,
        IReadOnlyList<string> skippedModules,
        IReadOnlyList<string> unavailableModules,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics,
        IReadOnlyList<string> assumptions,
        IReadOnlyList<string> warnings,
        string? topologySummary,
        string? ventilationSummary,
        string? groundSummary,
        string? heatingCoolingSummaryText,
        string? dhwSummary,
        string? systemEnergySummaryText,
        BuildingEnergyBalanceResult? heatingCoolingResult,
        CalculationTraceDocument? calculationTrace,
        bool includeReport,
        IReadOnlyList<string>? reportFormats)
    {
        var status = DetermineStatus(request.ExecutionMode, moduleResults, diagnostics, warnings);

        EngineeringReportDocument? reportDocument = null;
        EngineeringWorkflowReportPreviewDto? reportPreview = null;
        string? reportJson = null;
        string? reportMarkdown = null;

        if (includeReport)
        {
            var report = _reportBuilder.Build(new EngineeringReportGenerationRequest(
                ReportKind: EngineeringReportKind.FullEngineeringCore,
                RequestedFormat: EngineeringReportFormat.Json,
                ReportTitle: $"Scenario {request.ScenarioId}",
                ProjectId: request.State.ProjectId.ToString(),
                BuildingId: request.State.BuildingId?.ToString(),
                HeatingCoolingSummary: heatingCoolingResult,
                ValidationDiagnostics: diagnostics.Select(item =>
                    new CalculationDiagnostic(
                        Severity: ParseCalculationSeverity(item.Severity),
                        Code: item.Code,
                        Message: item.Message,
                        Context: item.SourceStep)).ToArray(),
                CalculationTrace: calculationTrace,
                DetailLevel: ParseReportDetailLevel(request.DetailLevel),
                IncludeTraceAppendix: request.IncludeTrace,
                IncludeLimitations: true,
                DeterministicTimestampUtc: request.DeterministicTimestampUtc,
                Assumptions: assumptions,
                Warnings: warnings,
                SourceCalculationIds: string.IsNullOrWhiteSpace(request.ScenarioId) ? [] : [request.ScenarioId],
                Metadata: new SortedDictionary<string, string>(StringComparer.Ordinal)
                {
                    ["scenario.kind"] = request.ScenarioKind.ToString(),
                    ["execution.mode"] = request.ExecutionMode.ToString()
                }));

            reportDocument = report;
            reportPreview = new EngineeringWorkflowReportPreviewDto(
                ReportKind: report.ReportKind.ToString(),
                Title: report.Title,
                Sections: report.Sections.OrderBy(item => item.Order).Select(item => item.Title).ToArray(),
                WarningsCount: report.Warnings.Count,
                DiagnosticsCount: report.Diagnostics.Count,
                ExportFormatsAvailable: ["Json", "Markdown"],
                GeneratedTimestampUtc: report.GeneratedTimestampUtc,
                Limitations: report.Sections
                    .Where(item => item.SectionKind == EngineeringReportSectionKind.Limitations)
                    .SelectMany(item => item.KeyValues.Select(value => value.Value?.ToString() ?? string.Empty))
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray());

            var formats = (reportFormats ?? ["Json"])
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (formats.Contains("Json", StringComparer.OrdinalIgnoreCase))
            {
                reportJson = _reportJsonExporter.Export(report, indented: true);
            }

            if (formats.Contains("Markdown", StringComparer.OrdinalIgnoreCase))
            {
                reportMarkdown = _reportMarkdownExporter.Export(report);
            }
        }

        var traceSummary = calculationTrace is null
            ? null
            : new EngineeringWorkflowTraceSummaryDto(
                TraceId: calculationTrace.TraceId,
                CalculationId: calculationTrace.CalculationId,
                DetailLevel: request.DetailLevel ?? "Standard",
                Modules: calculationTrace.Steps.Select(item => item.ModuleKind.ToString()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
                Assumptions: calculationTrace.Assumptions,
                Warnings: calculationTrace.Warnings,
                Steps: calculationTrace.Steps
                    .OrderBy(item => item.Sequence)
                    .Select(item => new EngineeringWorkflowTraceStepSummaryDto(
                        StepId: item.StepId,
                        ModuleKind: item.ModuleKind.ToString(),
                        StepName: item.StepName,
                        Sequence: item.Sequence,
                        Assumptions: item.Assumptions,
                        Warnings: item.Warnings,
                        DiagnosticsCount: item.Diagnostics.Count))
                    .ToArray());

        return new EngineeringCalculationScenarioResultDto(
            ScenarioId: request.ScenarioId,
            Status: status,
            Executed: moduleResults.Any(item => item.Status == EngineeringCalculationModuleExecutionStatus.Executed),
            ExecutedModules: executedModules.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            SkippedModules: skippedModules.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            UnavailableModules: unavailableModules.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            ValidationDiagnostics: SortAndDistinctDiagnostics(diagnostics),
            Assumptions: assumptions.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            Warnings: warnings.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            ModuleSummaries: new EngineeringCalculationModuleSummariesDto(
                TopologySummary: topologySummary,
                VentilationSummary: ventilationSummary,
                GroundSummary: groundSummary,
                HeatingCoolingSummary: heatingCoolingSummaryText,
                DomesticHotWaterSummary: dhwSummary,
                SystemEnergySummary: systemEnergySummaryText),
            ModuleResults: moduleResults
                .OrderBy(item => item.ModuleKind, StringComparer.Ordinal)
                .ToArray(),
            Timings: timings
                .OrderByDescending(item => item.DurationMilliseconds)
                .ThenBy(item => item.ModuleKind, StringComparer.Ordinal)
                .ToArray(),
            CalculationTrace: calculationTrace,
            CalculationTraceSummary: traceSummary,
            EngineeringReport: reportDocument,
            ReportPreview: reportPreview,
            ReportJson: reportJson,
            ReportMarkdown: reportMarkdown,
            Metadata: new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["execution.mode"] = request.ExecutionMode.ToString(),
                ["scenario.kind"] = request.ScenarioKind.ToString(),
                ["module.executed.count"] = executedModules.Count.ToString(),
                ["module.skipped.count"] = skippedModules.Count.ToString(),
                ["module.unavailable.count"] = unavailableModules.Count.ToString()
            });
    }

    public string FindModuleSummary(
        IEnumerable<EngineeringCalculationModuleExecutionResultDto> moduleResults,
        string moduleKind)
    {
        var result = moduleResults.FirstOrDefault(item => item.ModuleKind.Equals(moduleKind, StringComparison.Ordinal));
        if (result is null)
            return "Not started.";

        return result.Status switch
        {
            EngineeringCalculationModuleExecutionStatus.Executed => "Executed.",
            EngineeringCalculationModuleExecutionStatus.Skipped => "Skipped.",
            EngineeringCalculationModuleExecutionStatus.NotSupported => "Not supported.",
            EngineeringCalculationModuleExecutionStatus.Failed => "Failed.",
            _ => "Not started."
        };
    }

    private static EngineeringCalculationExecutionStatus DetermineStatus(
        EngineeringCalculationExecutionMode mode,
        IReadOnlyCollection<EngineeringCalculationModuleExecutionResultDto> moduleResults,
        IReadOnlyCollection<EngineeringWorkflowDiagnosticDto> diagnostics,
        IReadOnlyCollection<string> warnings)
    {
        var hasErrors = diagnostics.Any(item => IsError(item.Severity));

        if (mode == EngineeringCalculationExecutionMode.PrepareOnly || mode == EngineeringCalculationExecutionMode.ValidateOnly || mode == EngineeringCalculationExecutionMode.DryRun)
        {
            return hasErrors
                ? EngineeringCalculationExecutionStatus.FailedValidation
                : EngineeringCalculationExecutionStatus.Prepared;
        }

        if (hasErrors && moduleResults.All(item => item.Status != EngineeringCalculationModuleExecutionStatus.Executed))
        {
            return EngineeringCalculationExecutionStatus.FailedValidation;
        }

        if (moduleResults.Any(item => item.Status == EngineeringCalculationModuleExecutionStatus.Failed))
        {
            return EngineeringCalculationExecutionStatus.FailedExecution;
        }

        var executedCount = moduleResults.Count(item => item.Status == EngineeringCalculationModuleExecutionStatus.Executed);
        var skippedCount = moduleResults.Count(item => item.Status == EngineeringCalculationModuleExecutionStatus.Skipped);

        if (executedCount == 0 && skippedCount > 0)
        {
            return EngineeringCalculationExecutionStatus.PartiallyExecuted;
        }

        if (skippedCount > 0)
        {
            return EngineeringCalculationExecutionStatus.PartiallyExecuted;
        }

        if (warnings.Count > 0 || diagnostics.Any(item => item.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase)))
        {
            return EngineeringCalculationExecutionStatus.CompletedWithWarnings;
        }

        return EngineeringCalculationExecutionStatus.Completed;
    }

    private static CalculationTraceModuleKind ParseTraceModuleKind(string moduleKind)
    {
        return moduleKind switch
        {
            "ThermalTopology" => CalculationTraceModuleKind.ThermalTopology,
            "WeatherSolar" => CalculationTraceModuleKind.Weather,
            "Ventilation" => CalculationTraceModuleKind.Ventilation,
            "Ground" => CalculationTraceModuleKind.Ground,
            "HeatingCooling" => CalculationTraceModuleKind.Iso52016,
            "DomesticHotWater" => CalculationTraceModuleKind.DomesticHotWater,
            "SystemEnergy" => CalculationTraceModuleKind.SystemEnergy,
            _ => CalculationTraceModuleKind.Generic
        };
    }

    private static CalculationTraceModuleKind ParseTraceModuleKindFromStep(string sourceStep)
    {
        return sourceStep switch
        {
            "Zones" or "Envelope" => CalculationTraceModuleKind.ThermalTopology,
            "WeatherSolar" => CalculationTraceModuleKind.Weather,
            "Ventilation" => CalculationTraceModuleKind.Ventilation,
            "Ground" => CalculationTraceModuleKind.Ground,
            "DomesticHotWater" => CalculationTraceModuleKind.DomesticHotWater,
            "SystemEnergy" => CalculationTraceModuleKind.SystemEnergy,
            "Reports" => CalculationTraceModuleKind.Reporting,
            "Validation" => CalculationTraceModuleKind.Validation,
            _ => CalculationTraceModuleKind.Generic
        };
    }

    private static CalculationTraceSeverity ParseTraceSeverity(string severity)
    {
        if (severity.Equals("error", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceSeverity.Error;
        if (severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceSeverity.Warning;
        if (severity.Equals("assumption", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceSeverity.Assumption;
        if (severity.Equals("debug", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceSeverity.Debug;

        return CalculationTraceSeverity.Info;
    }

    private static CalculationDiagnosticSeverity ParseCalculationSeverity(string severity)
    {
        if (severity.Equals("error", StringComparison.OrdinalIgnoreCase))
            return CalculationDiagnosticSeverity.Error;
        if (severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
            return CalculationDiagnosticSeverity.Warning;

        return CalculationDiagnosticSeverity.Info;
    }

    private static CalculationTraceDetailLevel ParseTraceDetailLevel(string? detailLevel)
    {
        if (!string.IsNullOrWhiteSpace(detailLevel) &&
            Enum.TryParse<CalculationTraceDetailLevel>(detailLevel, true, out var parsed))
        {
            return parsed;
        }

        return CalculationTraceDetailLevel.Standard;
    }

    private static EngineeringReportDetailLevel ParseReportDetailLevel(string? detailLevel)
    {
        if (!string.IsNullOrWhiteSpace(detailLevel) &&
            Enum.TryParse<EngineeringReportDetailLevel>(detailLevel, true, out var parsed))
        {
            return parsed;
        }

        return EngineeringReportDetailLevel.Standard;
    }

    private static IReadOnlyList<EngineeringWorkflowDiagnosticDto> SortAndDistinctDiagnostics(
        IEnumerable<EngineeringWorkflowDiagnosticDto> diagnostics)
    {
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

    private static bool IsError(string severity) =>
        severity.Equals("error", StringComparison.OrdinalIgnoreCase);
}