using AssistantEngineer.Modules.Calculations.Application.Abstractions.Trace;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Trace;

public sealed class CalculationTraceDiagnosticMapper : ICalculationTraceDiagnosticMapper
{
    public CalculationTraceDiagnostic Map(
        CalculationDiagnostic diagnostic,
        CalculationTraceModuleKind moduleKind)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        return new CalculationTraceDiagnostic(
            MapSeverity(diagnostic.Severity),
            diagnostic.Code,
            diagnostic.Message,
            moduleKind,
            diagnostic.Context,
            null);
    }

    public CalculationTraceDiagnostic Map(
        StandardCalculationDiagnostic diagnostic,
        CalculationTraceModuleKind fallbackModuleKind)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        return new CalculationTraceDiagnostic(
            MapSeverity(diagnostic.Severity),
            diagnostic.Code,
            diagnostic.Message,
            MapModuleKind(diagnostic.Source, fallbackModuleKind),
            diagnostic.Context,
            diagnostic.Source);
    }

    public CalculationTraceDiagnostic Map(
        string code,
        string message,
        CalculationTraceSeverity severity,
        CalculationTraceModuleKind moduleKind,
        string? context = null,
        string? source = null) =>
        new(
            severity,
            string.IsNullOrWhiteSpace(code) ? "AE-TRACE-DIAGNOSTIC" : code,
            message,
            moduleKind,
            context,
            source);

    public CalculationTraceDiagnostic MapWarning(
        string warning,
        CalculationTraceModuleKind moduleKind,
        string? code = null) =>
        new(
            CalculationTraceSeverity.Warning,
            string.IsNullOrWhiteSpace(code) ? "AE-TRACE-WARNING" : code,
            warning,
            moduleKind,
            null,
            null);

    private static CalculationTraceSeverity MapSeverity(
        CalculationDiagnosticSeverity severity) =>
        severity switch
        {
            CalculationDiagnosticSeverity.Error => CalculationTraceSeverity.Error,
            CalculationDiagnosticSeverity.Warning => CalculationTraceSeverity.Warning,
            _ => CalculationTraceSeverity.Info
        };

    private static CalculationTraceModuleKind MapModuleKind(
        string? source,
        CalculationTraceModuleKind fallback)
    {
        if (string.IsNullOrWhiteSpace(source))
            return fallback;

        if (source.Contains("Weather", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceModuleKind.Weather;

        if (source.Contains("Solar", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceModuleKind.Solar;

        if (source.Contains("Topology", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceModuleKind.ThermalTopology;

        if (source.Contains("Iso52016", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceModuleKind.Iso52016;

        if (source.Contains("MultiZone", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceModuleKind.MultiZone;

        if (source.Contains("Ventilation", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceModuleKind.Ventilation;

        if (source.Contains("Ground", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceModuleKind.Ground;

        if (source.Contains("DomesticHotWater", StringComparison.OrdinalIgnoreCase) ||
            source.Contains("Dhw", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceModuleKind.DomesticHotWater;

        if (source.Contains("SystemEnergy", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceModuleKind.SystemEnergy;

        if (source.Contains("Validation", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceModuleKind.Validation;

        if (source.Contains("Report", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceModuleKind.Reporting;

        return fallback;
    }
}
