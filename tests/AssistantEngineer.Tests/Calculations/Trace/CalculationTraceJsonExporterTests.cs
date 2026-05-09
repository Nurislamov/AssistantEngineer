using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Calculations.Application.Services.Trace;

namespace AssistantEngineer.Tests.Calculations.Trace;

public sealed class CalculationTraceJsonExporterTests
{
    private readonly CalculationTraceJsonExporter _exporter = new();

    [Fact]
    public void ExportsStableSchemaVersionAndRequiredTopLevelFields()
    {
        var trace = BuildSimpleTrace();
        var json = _exporter.Export(trace);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("1.0", root.GetProperty("SchemaVersion").GetString());
        Assert.Equal("trace-json-tests", root.GetProperty("TraceId").GetString());
        Assert.Equal("TraceExporterTests", root.GetProperty("CalculationType").GetString());
        Assert.True(root.TryGetProperty("Steps", out _));
        Assert.True(root.TryGetProperty("Summary", out _));
    }

    [Fact]
    public void OutputIsValidJsonAndDeterministic()
    {
        var trace = BuildSimpleTrace();
        var left = _exporter.Export(trace, indented: false);
        var right = _exporter.Export(trace, indented: false);

        using var _ = JsonDocument.Parse(left);
        Assert.Equal(left, right);
    }

    [Fact]
    public void DoesNotWriteGeneratedArtifacts()
    {
        var forbidden = Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "calculation-trace", "generated");
        Assert.False(Directory.Exists(forbidden), $"Generated trace artifact directory must not exist: {forbidden}");
    }

    private static CalculationTraceDocument BuildSimpleTrace()
    {
        var step = new CalculationTraceStep(
            StepId: "step-1",
            ModuleKind: CalculationTraceModuleKind.Generic,
            StepName: "Step",
            Sequence: 1,
            InputValues:
            [
                new CalculationTraceValue("input", "Input", 1d, new CalculationTraceUnit("kWh"), CalculationTraceValueKind.Input)
            ],
            IntermediateValues: [],
            OutputValues:
            [
                new CalculationTraceValue("output", "Output", 2d, new CalculationTraceUnit("kWh"), CalculationTraceValueKind.Output)
            ],
            FormulaOrConventionLabel: "Simple formula",
            Assumptions: ["assumption"],
            Warnings: [],
            Diagnostics: [],
            ChildSteps: []);

        return new CalculationTraceDocument(
            TraceId: "trace-json-tests",
            CalculationId: "calc-json-tests",
            CalculationType: "TraceExporterTests",
            CreatedTimestampUtc: DateTimeOffset.UnixEpoch,
            RootModule: CalculationTraceModuleKind.Generic,
            Steps: [step],
            Summary: new CalculationTraceSummary(1, 0, 0, 1, [CalculationTraceModuleKind.Generic]),
            Assumptions: [],
            Warnings: [],
            Diagnostics: [],
            Metadata: new Dictionary<string, string> { ["schema"] = "1.0" },
            SchemaVersion: "1.0");
    }
}
