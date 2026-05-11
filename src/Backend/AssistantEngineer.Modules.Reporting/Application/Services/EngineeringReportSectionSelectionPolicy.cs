using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal static class EngineeringReportSectionSelectionPolicy
{
    public static bool HasTraceModule(
        CalculationTraceDocument trace,
        params CalculationTraceModuleKind[] modules)
    {
        var set = modules.ToHashSet();
        return FlattenTraceSteps(trace.Steps).Any(step => set.Contains(step.ModuleKind));
    }

    public static int CountTraceSteps(
        CalculationTraceDocument? trace,
        CalculationTraceModuleKind module)
    {
        if (trace is null)
            return 0;

        return FlattenTraceSteps(trace.Steps).Count(step => step.ModuleKind == module);
    }

    public static IReadOnlyList<CalculationTraceStep> LimitTraceSteps(
        IReadOnlyList<CalculationTraceStep> steps,
        int maxCount)
    {
        return FlattenTraceSteps(steps)
            .OrderBy(item => item.Sequence)
            .Take(Math.Max(1, maxCount))
            .ToArray();
    }

    public static bool ShouldIncludeSection(
        EngineeringReportKind kind,
        bool hasData,
        ICollection<EngineeringReportDiagnostic> diagnostics,
        string sectionId,
        CalculationTraceModuleKind module,
        EngineeringReportDetailLevel detailLevel)
    {
        var sectionIsRequested = kind is EngineeringReportKind.FullEngineeringCore or EngineeringReportKind.Generic
            || (kind == EngineeringReportKind.HeatingCoolingLoad && sectionId == "heating-cooling")
            || (kind == EngineeringReportKind.DomesticHotWater && sectionId == "domestic-hot-water")
            || (kind == EngineeringReportKind.SystemEnergy && (sectionId == "system-energy" || sectionId == "final-energy" || sectionId == "primary-energy-carbon"))
            || (kind == EngineeringReportKind.CalculationSummary && sectionId is "heating-cooling" or "natural-ventilation" or "ground-boundaries" or "domestic-hot-water" or "system-energy")
            || (kind == EngineeringReportKind.Validation && sectionId == "validation");

        if (!sectionIsRequested)
            return false;

        if (hasData)
            return true;

        diagnostics.Add(new EngineeringReportDiagnostic(
            EngineeringReportDiagnosticSeverity.Info,
            "AE-REPORT-SECTION-DATA-MISSING",
            $"Section '{sectionId}' was requested by report kind '{kind}' but source summary data was not provided.",
            module,
            $"Detail={detailLevel}",
            "EngineeringReportBuilder",
            "Provide corresponding module summary input for this report kind."));
        return false;
    }

    private static IEnumerable<CalculationTraceStep> FlattenTraceSteps(
        IEnumerable<CalculationTraceStep> steps)
    {
        foreach (var step in steps)
        {
            yield return step;
            foreach (var child in FlattenTraceSteps(step.ChildSteps))
                yield return child;
        }
    }
}
