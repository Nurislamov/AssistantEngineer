using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal sealed class EngineeringReportDiagnosticAggregator : IEngineeringReportDiagnosticAggregator
{
    public EngineeringReportDiagnostic FromCalculationDiagnostic(
        CalculationDiagnostic diagnostic,
        CalculationTraceModuleKind module = CalculationTraceModuleKind.Validation,
        string? source = null)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        return new EngineeringReportDiagnostic(
            Severity: ToSeverity(diagnostic.Severity),
            Code: diagnostic.Code,
            Message: diagnostic.Message,
            Module: module,
            Context: diagnostic.Context,
            Source: source,
            SuggestedCorrection: SuggestCorrection(diagnostic.Code, diagnostic.Message));
    }

    public EngineeringReportDiagnostic FromStandardDiagnostic(
        StandardCalculationDiagnostic diagnostic,
        CalculationTraceModuleKind defaultModule = CalculationTraceModuleKind.Validation,
        string? source = null)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        return new EngineeringReportDiagnostic(
            Severity: ToSeverity(diagnostic.Severity),
            Code: diagnostic.Code,
            Message: diagnostic.Message,
            Module: ResolveModule(diagnostic, defaultModule),
            Context: diagnostic.Context,
            Source: source ?? diagnostic.Source,
            SuggestedCorrection: SuggestCorrection(diagnostic.Code, diagnostic.Message));
    }

    public EngineeringReportDiagnostic FromTraceDiagnostic(
        CalculationTraceDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        return new EngineeringReportDiagnostic(
            Severity: ToSeverity(diagnostic.Severity),
            Code: diagnostic.Code,
            Message: diagnostic.Message,
            Module: diagnostic.ModuleKind,
            Context: diagnostic.Context,
            Source: diagnostic.Source,
            SuggestedCorrection: SuggestCorrection(diagnostic.Code, diagnostic.Message));
    }

    public IReadOnlyList<EngineeringReportDiagnostic> Aggregate(
        IEnumerable<EngineeringReportDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        return diagnostics
            .Where(item =>
                item is not null &&
                !string.IsNullOrWhiteSpace(item.Code) &&
                !string.IsNullOrWhiteSpace(item.Message))
            .Distinct()
            .OrderByDescending(item => SeverityRank(item.Severity))
            .ThenBy(item => item.Module)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.Message, StringComparer.Ordinal)
            .ThenBy(item => item.Context, StringComparer.Ordinal)
            .ToArray();
    }

    private static CalculationTraceModuleKind ResolveModule(
        StandardCalculationDiagnostic diagnostic,
        CalculationTraceModuleKind defaultModule)
    {
        if (diagnostic.Source is null)
            return defaultModule;

        var source = diagnostic.Source;
        if (source.Contains("SystemEnergy", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceModuleKind.SystemEnergy;

        if (source.Contains("DomesticHotWater", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceModuleKind.DomesticHotWater;

        if (source.Contains("Ventilation", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceModuleKind.Ventilation;

        if (source.Contains("Ground", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceModuleKind.Ground;

        if (source.Contains("Iso52016", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceModuleKind.Iso52016;

        if (source.Contains("Solar", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceModuleKind.Solar;

        if (source.Contains("Weather", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceModuleKind.Weather;

        return defaultModule;
    }

    private static EngineeringReportDiagnosticSeverity ToSeverity(
        CalculationDiagnosticSeverity severity) =>
        severity switch
        {
            CalculationDiagnosticSeverity.Error => EngineeringReportDiagnosticSeverity.Error,
            CalculationDiagnosticSeverity.Warning => EngineeringReportDiagnosticSeverity.Warning,
            _ => EngineeringReportDiagnosticSeverity.Info
        };

    private static EngineeringReportDiagnosticSeverity ToSeverity(
        CalculationTraceSeverity severity) =>
        severity switch
        {
            CalculationTraceSeverity.Error => EngineeringReportDiagnosticSeverity.Error,
            CalculationTraceSeverity.Warning => EngineeringReportDiagnosticSeverity.Warning,
            CalculationTraceSeverity.Assumption => EngineeringReportDiagnosticSeverity.Assumption,
            CalculationTraceSeverity.Info => EngineeringReportDiagnosticSeverity.Info,
            _ => EngineeringReportDiagnosticSeverity.Debug
        };

    private static int SeverityRank(
        EngineeringReportDiagnosticSeverity severity) =>
        severity switch
        {
            EngineeringReportDiagnosticSeverity.Error => 4,
            EngineeringReportDiagnosticSeverity.Warning => 3,
            EngineeringReportDiagnosticSeverity.Assumption => 2,
            EngineeringReportDiagnosticSeverity.Info => 1,
            _ => 0
        };

    private static string? SuggestCorrection(
        string code,
        string message)
    {
        var value = $"{code} {message}";

        if (value.Contains("missing", StringComparison.OrdinalIgnoreCase))
            return "Provide missing required inputs and rerun the calculation/report workflow.";

        if (value.Contains("invalid", StringComparison.OrdinalIgnoreCase))
            return "Correct invalid input values and rerun the calculation/report workflow.";

        if (value.Contains("fallback", StringComparison.OrdinalIgnoreCase))
            return "Review fallback assumptions and provide explicit values where available.";

        if (value.Contains("out of scope", StringComparison.OrdinalIgnoreCase))
            return "Use a supported calculation scope for this report kind.";

        return null;
    }
}
