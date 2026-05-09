using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Calculations.Application.Services.Trace;

namespace AssistantEngineer.Tests.Calculations.Trace;

public sealed class CalculationTraceDiagnosticMapperTests
{
    private readonly CalculationTraceDiagnosticMapper _mapper = new();

    [Fact]
    public void ExistingWarningsMapIntoTraceWarnings()
    {
        var mapped = _mapper.MapWarning(
            "Fallback used",
            CalculationTraceModuleKind.SystemEnergy,
            "AE-WARN");

        Assert.Equal(CalculationTraceSeverity.Warning, mapped.Severity);
        Assert.Equal("AE-WARN", mapped.Code);
        Assert.Equal(CalculationTraceModuleKind.SystemEnergy, mapped.ModuleKind);
    }

    [Fact]
    public void ValidationDiagnosticsMapIntoTraceDiagnostics()
    {
        var diagnostic = new StandardCalculationDiagnostic(
            CalculationDiagnosticSeverity.Error,
            "AE-VAL-ERR",
            "Validation failed",
            Context: "zone-1",
            Source: "Validation");

        var mapped = _mapper.Map(diagnostic, CalculationTraceModuleKind.Generic);

        Assert.Equal(CalculationTraceSeverity.Error, mapped.Severity);
        Assert.Equal(CalculationTraceModuleKind.Validation, mapped.ModuleKind);
        Assert.Equal("zone-1", mapped.Context);
    }

    [Fact]
    public void ModuleAssumptionsAreRepresentedWithAssumptionSeverity()
    {
        var mapped = _mapper.Map(
            "AE-ASSUME",
            "Assumed default setpoint",
            CalculationTraceSeverity.Assumption,
            CalculationTraceModuleKind.Iso52016);

        Assert.Equal(CalculationTraceSeverity.Assumption, mapped.Severity);
        Assert.Equal("AE-ASSUME", mapped.Code);
        Assert.Equal(CalculationTraceModuleKind.Iso52016, mapped.ModuleKind);
    }

    [Fact]
    public void CalculationDiagnosticMapsWithFallbackModule()
    {
        var diagnostic = new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Warning,
            "AE-CALC-WARN",
            "Warning text",
            Context: "ctx");

        var mapped = _mapper.Map(diagnostic, CalculationTraceModuleKind.Weather);
        Assert.Equal(CalculationTraceSeverity.Warning, mapped.Severity);
        Assert.Equal(CalculationTraceModuleKind.Weather, mapped.ModuleKind);
    }
}
