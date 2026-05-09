using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Reporting.Application.Services;

namespace AssistantEngineer.Tests.Reporting;

public sealed class EngineeringReportDiagnosticAggregatorTests
{
    private readonly EngineeringReportDiagnosticAggregator _aggregator = new();

    [Fact]
    public void MapsAndAggregatesDiagnosticsDeterministically()
    {
        var diagnostics = new[]
        {
            _aggregator.FromCalculationDiagnostic(new CalculationDiagnostic(CalculationDiagnosticSeverity.Warning, "AE-B", "Warning B")),
            _aggregator.FromCalculationDiagnostic(new CalculationDiagnostic(CalculationDiagnosticSeverity.Error, "AE-A", "Error A")),
            _aggregator.FromTraceDiagnostic(new CalculationTraceDiagnostic(CalculationTraceSeverity.Warning, "AE-B", "Warning B", CalculationTraceModuleKind.Validation))
        };

        var merged = _aggregator.Aggregate(diagnostics);
        Assert.Equal(2, merged.Count);
        Assert.Equal("AE-A", merged[0].Code);
        Assert.Equal("AE-B", merged[1].Code);
    }

    [Fact]
    public void IncludesSuggestedCorrectionWhenPatternMatches()
    {
        var diagnostic = _aggregator.FromStandardDiagnostic(
            new StandardCalculationDiagnostic(CalculationDiagnosticSeverity.Warning, "AE-MISSING", "Missing required factor"));

        Assert.NotNull(diagnostic.SuggestedCorrection);
        Assert.Contains("Provide missing required inputs", diagnostic.SuggestedCorrection, StringComparison.OrdinalIgnoreCase);
    }
}

