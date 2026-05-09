using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Calculations.Application.Services.Trace;

namespace AssistantEngineer.Tests.Calculations.Trace;

public sealed class CalculationTraceBuilderTests
{
    [Fact]
    public void CreatesDeterministicTraceDocument()
    {
        var left = BuildSimpleDocument(CalculationTraceDetailLevel.Standard);
        var right = BuildSimpleDocument(CalculationTraceDetailLevel.Standard);

        var exporter = new CalculationTraceJsonExporter();
        Assert.Equal(exporter.Export(left), exporter.Export(right));
    }

    [Fact]
    public void StepsAreOrderedDeterministically()
    {
        var builder = CreateBuilder(CalculationTraceDetailLevel.Standard);
        var second = builder.AddStep(CalculationTraceModuleKind.Generic, "Second");
        var first = builder.AddStep(CalculationTraceModuleKind.Generic, "First");

        builder.AddOutputValue(second, Value("second", 2d));
        builder.AddOutputValue(first, Value("first", 1d));

        var document = builder.Build();
        Assert.Equal(["Second", "First"], document.Steps.Select(step => step.StepName).ToArray());
        Assert.Equal([1, 2], document.Steps.Select(step => step.Sequence).ToArray());
    }

    [Fact]
    public void NestedStepsArePreserved()
    {
        var builder = CreateBuilder(CalculationTraceDetailLevel.Standard);
        var parent = builder.AddStep(CalculationTraceModuleKind.Generic, "Parent");
        var child = builder.AddStep(CalculationTraceModuleKind.Generic, "Child", parentStepId: parent);
        builder.AddOutputValue(child, Value("value", 5d));

        var document = builder.Build();
        Assert.Single(document.Steps);
        Assert.Single(document.Steps[0].ChildSteps);
        Assert.Equal("Child", document.Steps[0].ChildSteps[0].StepName);
    }

    [Fact]
    public void DuplicateDiagnosticsAreRemoved()
    {
        var builder = CreateBuilder(CalculationTraceDetailLevel.Standard);
        var step = builder.AddStep(CalculationTraceModuleKind.Generic, "Diagnostics step");
        var diagnostic = new CalculationTraceDiagnostic(
            CalculationTraceSeverity.Warning,
            "AE-DUP",
            "Duplicate warning",
            CalculationTraceModuleKind.Generic);

        builder.AddDiagnostic(step, diagnostic);
        builder.AddDiagnostic(step, diagnostic);
        builder.AddDocumentDiagnostic(diagnostic);
        builder.AddDocumentDiagnostic(diagnostic);

        var document = builder.Build();
        Assert.Single(document.Steps[0].Diagnostics);
        Assert.Single(document.Diagnostics);
    }

    [Fact]
    public void AssumptionsAndWarningsAreAdded()
    {
        var builder = CreateBuilder(CalculationTraceDetailLevel.Standard);
        var step = builder.AddStep(CalculationTraceModuleKind.Generic, "Assumptions step");
        builder.AddAssumption(step, " assumption ");
        builder.AddWarning(step, " warning ");
        builder.AddDocumentAssumption(" doc assumption ");
        builder.AddDocumentWarning(" doc warning ");

        var document = builder.Build();
        Assert.Contains("assumption", document.Steps[0].Assumptions);
        Assert.Contains("warning", document.Steps[0].Warnings);
        Assert.Contains("doc assumption", document.Assumptions);
        Assert.Contains("doc warning", document.Warnings);
    }

    [Fact]
    public void DetailLevelNoneDisablesTraceCollection()
    {
        var builder = CreateBuilder(CalculationTraceDetailLevel.None);
        var step = builder.AddStep(CalculationTraceModuleKind.Generic, "Disabled");
        builder.AddOutputValue(step, Value("value", 1d));

        var document = builder.Build();
        Assert.Empty(document.Steps);
        Assert.Equal(0, document.Summary.StepCount);
    }

    [Fact]
    public void DetailedLevelIncludesIntermediateValues()
    {
        var builder = CreateBuilder(CalculationTraceDetailLevel.Detailed);
        var step = builder.AddStep(CalculationTraceModuleKind.Generic, "Detailed step");
        builder.AddIntermediateValue(step, Value("intermediate", new[] { 1d, 2d }, CalculationTraceValueKind.Intermediate));

        var document = builder.Build();
        Assert.Single(document.Steps[0].IntermediateValues);
    }

    [Fact]
    public void SummaryLevelExcludesDetailedIntermediateArrays()
    {
        var builder = CreateBuilder(CalculationTraceDetailLevel.Summary);
        var step = builder.AddStep(CalculationTraceModuleKind.Generic, "Summary step");
        builder.AddIntermediateValue(step, Value("intermediate", Enumerable.Range(0, 12).Select(i => (double)i).ToArray(), CalculationTraceValueKind.Intermediate));
        builder.AddOutputValue(step, Value("summary", 12d));

        var document = builder.Build();
        Assert.Empty(document.Steps[0].IntermediateValues);
        Assert.Single(document.Steps[0].OutputValues);
    }

    private static CalculationTraceDocument BuildSimpleDocument(
        CalculationTraceDetailLevel detailLevel)
    {
        var builder = CreateBuilder(detailLevel);
        var step = builder.AddStep(CalculationTraceModuleKind.Generic, "Simple");
        builder.AddInputValue(step, Value("input", 10d, CalculationTraceValueKind.Input));
        builder.AddOutputValue(step, Value("output", 20d));
        builder.AddAssumption(step, "Deterministic assumption");
        builder.AddWarning(step, "Deterministic warning");
        builder.AddDiagnostic(
            step,
            new CalculationTraceDiagnostic(
                CalculationTraceSeverity.Info,
                "AE-TRACE-INFO",
                "Deterministic diagnostic",
                CalculationTraceModuleKind.Generic));

        return builder.Build();
    }

    private static CalculationTraceBuilder CreateBuilder(
        CalculationTraceDetailLevel detailLevel)
    {
        var builder = new CalculationTraceBuilder();
        builder.SetDetailLevel(detailLevel);
        builder.Initialize(
            traceId: "trace-builder-tests",
            calculationType: "BuilderTests",
            rootModule: CalculationTraceModuleKind.Generic,
            createdTimestampUtc: DateTimeOffset.UnixEpoch);
        return builder;
    }

    private static CalculationTraceValue Value(
        string key,
        object? value,
        CalculationTraceValueKind kind = CalculationTraceValueKind.Output) =>
        new(
            Key: key,
            Label: key,
            Value: value,
            Unit: null,
            ValueKind: kind);
}
