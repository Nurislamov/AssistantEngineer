using System.Reflection;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Calculations.Application.Models.Trace;
using AssistantEngineer.Modules.Calculations.Application.Services.Trace;

namespace AssistantEngineer.Tests.Calculations.Trace;

public sealed class EngineeringCalculationTraceBuilderTests
{
    [Fact]
    public void BuildRoomHeatingTrace_CreatesExpectedSectionsAndFinalLineValue()
    {
        var builder = new EngineeringCalculationTraceBuilder();
        var trace = builder.BuildRoomHeatingTrace(CreateBaseInput());

        Assert.Equal("RoomHeatingLoad", trace.CalculationType);
        Assert.Contains(trace.Sections, section => section.Title == "Transmission heat loss");
        Assert.Contains(trace.Sections, section => section.Title == "Ventilation heat loss");
        Assert.Contains(trace.Sections, section => section.Title == "Final heating load");

        var finalSection = trace.Sections.Single(section => section.SectionId == "section-final-heating-load");
        var finalLine = finalSection.Lines.Single(line => line.LineId == "line-final-heating-load");
        Assert.NotNull(finalLine.Value);
        Assert.Equal(832.5, finalLine.Value.Value, precision: 6);
        Assert.Equal("W", finalLine.Unit);
    }

    [Fact]
    public void BuildRoomHeatingTrace_IncludesAssumptions()
    {
        var builder = new EngineeringCalculationTraceBuilder();
        var input = CreateBaseInput() with
        {
            Assumptions =
            [
                new EngineeringCalculationTraceAssumption(
                    AssumptionId: "ASSUMP-VENT-SENSIBLE-COEFFICIENT-001",
                    Name: "Simplified sensible air heat coefficient",
                    Value: "0.33",
                    Unit: "Wh/(m³·K)",
                    Status: "ValidationOnly",
                    Source: "Manual fixture",
                    RegistryReference: "docs/engineering/engineering-assumptions-registry.md")
            ]
        };

        var trace = builder.BuildRoomHeatingTrace(input);

        Assert.Contains(trace.Assumptions, assumption =>
            assumption.AssumptionId == "ASSUMP-VENT-SENSIBLE-COEFFICIENT-001");

        var assumptionsSection = trace.Sections.Single(section => section.SectionId == "section-assumptions-exclusions");
        Assert.Contains(assumptionsSection.Lines, line => line.LineId.Contains("ASSUMP-VENT-SENSIBLE-COEFFICIENT-001", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildRoomHeatingTrace_IncludesExcludedEffects()
    {
        var builder = new EngineeringCalculationTraceBuilder();
        var input = CreateBaseInput() with
        {
            ExcludedEffects =
            [
                new EngineeringCalculationTraceExcludedEffect("Solar gains", "Solar gains excluded for this scenario.", "Fixture"),
                new EngineeringCalculationTraceExcludedEffect("Internal gains", "Internal gains excluded for this scenario.", "Fixture"),
                new EngineeringCalculationTraceExcludedEffect("Dynamic effects", "Dynamic thermal mass effects excluded.", "Fixture")
            ]
        };

        var trace = builder.BuildRoomHeatingTrace(input);
        var assumptionsSection = trace.Sections.Single(section => section.SectionId == "section-assumptions-exclusions");

        Assert.Contains(assumptionsSection.Lines, line => line.Label.Contains("Solar gains", StringComparison.Ordinal));
        Assert.Contains(assumptionsSection.Lines, line => line.Label.Contains("Internal gains", StringComparison.Ordinal));
        Assert.Contains(assumptionsSection.Lines, line => line.Label.Contains("Dynamic effects", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildRoomHeatingTrace_IncludesDiagnosticReferences()
    {
        var builder = new EngineeringCalculationTraceBuilder();
        var input = CreateBaseInput() with
        {
            DiagnosticReferences =
            [
                new EngineeringCalculationTraceDiagnosticReference(
                    Code: "IQ-ROOM-040",
                    Severity: "Warning",
                    Category: "Ventilation",
                    Message: "Room ventilation configuration is missing and defaults may be used.")
            ]
        };

        var trace = builder.BuildRoomHeatingTrace(input);
        var diagnosticsSection = trace.Sections.Single(section => section.SectionId == "section-diagnostics");

        Assert.Contains(trace.DiagnosticReferences, diagnostic => diagnostic.Code == "IQ-ROOM-040");
        Assert.Contains(diagnosticsSection.Lines, line => line.Label == "IQ-ROOM-040");
    }

    [Fact]
    public void BuildRoomHeatingTrace_ComponentMismatchAddsConsistencyWarningWithoutThrowing()
    {
        var builder = new EngineeringCalculationTraceBuilder();
        var input = CreateBaseInput() with
        {
            TotalHeatingLoadW = 832.5,
            TransmissionHeatLossW = 500.0,
            VentilationHeatLossW = 200.0,
            InfiltrationHeatLossW = 100.0,
            GroundHeatLossW = 0.0,
            SolarGainW = 0.0,
            InternalGainW = 0.0
        };

        var trace = builder.BuildRoomHeatingTrace(input);

        var finalSection = trace.Sections.Single(section => section.SectionId == "section-final-heating-load");
        Assert.Contains(finalSection.Lines, line => line.LineId == "line-consistency-warning");
        Assert.NotNull(trace.Metadata);
        Assert.Equal("MismatchWarning", trace.Metadata["consistencyStatus"]);
    }

    [Fact]
    public void BuilderHasNoCalculationPipelineDependency()
    {
        var constructor = typeof(EngineeringCalculationTraceBuilder).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
        Assert.Empty(constructor.GetParameters());
    }

    private static RoomHeatingLoadTraceInput CreateBaseInput()
    {
        return new RoomHeatingLoadTraceInput(
            RoomId: 101,
            RoomName: "Validation Room",
            TransmissionHeatLossW: 585.0,
            VentilationHeatLossW: 247.5,
            InfiltrationHeatLossW: 0.0,
            GroundHeatLossW: 0.0,
            SolarGainW: 0.0,
            InternalGainW: 0.0,
            TotalHeatingLoadW: 832.5,
            Assumptions: [],
            ExcludedEffects: [],
            DiagnosticReferences: []);
    }
}
