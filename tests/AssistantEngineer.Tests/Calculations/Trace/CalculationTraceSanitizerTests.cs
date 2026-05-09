using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Calculations.Application.Services.Trace;

namespace AssistantEngineer.Tests.Calculations.Trace;

public sealed class CalculationTraceSanitizerTests
{
    private readonly CalculationTraceSanitizer _sanitizer = new();

    [Fact]
    public void RemovesNullAndEmptyNoise()
    {
        var document = BuildDocumentWithNoise();
        var sanitized = _sanitizer.Sanitize(document, CalculationTraceDetailLevel.Standard);

        Assert.DoesNotContain(sanitized.Assumptions, item => string.IsNullOrWhiteSpace(item));
        Assert.DoesNotContain(sanitized.Warnings, item => string.IsNullOrWhiteSpace(item));
        Assert.DoesNotContain(sanitized.Steps[0].OutputValues, value => string.IsNullOrWhiteSpace(value.Key));
    }

    [Fact]
    public void DiagnosticsAreSortedAndDeduplicated()
    {
        var diagnostics = new[]
        {
            new CalculationTraceDiagnostic(CalculationTraceSeverity.Info, "B", "info", CalculationTraceModuleKind.Generic),
            new CalculationTraceDiagnostic(CalculationTraceSeverity.Error, "A", "error", CalculationTraceModuleKind.Generic),
            new CalculationTraceDiagnostic(CalculationTraceSeverity.Warning, "C", "warn", CalculationTraceModuleKind.Generic),
            new CalculationTraceDiagnostic(CalculationTraceSeverity.Warning, "C", "warn", CalculationTraceModuleKind.Generic)
        };

        var document = BuildBaseDocument(
            [
                new CalculationTraceStep(
                    StepId: "step-1",
                    ModuleKind: CalculationTraceModuleKind.Generic,
                    StepName: "Step",
                    Sequence: 1,
                    InputValues: [],
                    IntermediateValues: [],
                    OutputValues: [],
                    FormulaOrConventionLabel: null,
                    Assumptions: [],
                    Warnings: [],
                    Diagnostics: diagnostics,
                    ChildSteps: [])
            ]);

        var sanitized = _sanitizer.Sanitize(document);
        Assert.Equal(3, sanitized.Steps[0].Diagnostics.Count);
        Assert.Equal(CalculationTraceSeverity.Error, sanitized.Steps[0].Diagnostics[0].Severity);
    }

    [Fact]
    public void NumericPrecisionIsStabilized()
    {
        var value = new CalculationTraceValue(
            Key: "pi",
            Label: "pi",
            Value: 3.1415926535d,
            Unit: null,
            ValueKind: CalculationTraceValueKind.Output);

        var document = BuildBaseDocument(
            [
                new CalculationTraceStep(
                    StepId: "step-1",
                    ModuleKind: CalculationTraceModuleKind.Generic,
                    StepName: "Step",
                    Sequence: 1,
                    InputValues: [],
                    IntermediateValues: [],
                    OutputValues: [value],
                    FormulaOrConventionLabel: null,
                    Assumptions: [],
                    Warnings: [],
                    Diagnostics: [],
                    ChildSteps: [])
            ]);

        var sanitized = _sanitizer.Sanitize(document);
        Assert.Equal(3.141593d, sanitized.Steps[0].OutputValues[0].Value);
    }

    [Fact]
    public void CompactModeSummarizesLargeProfiles()
    {
        var fixture = CalculationTraceFixtureLoader.Load("sanitized-compact-trace-fixture.json");
        var profile = Enumerable.Range(0, 24).Select(index => (double)index).ToArray();

        var step = new CalculationTraceStep(
            StepId: "step-1",
            ModuleKind: CalculationTraceModuleKind.Generic,
            StepName: fixture.ExpectedStepNames[0],
            Sequence: 1,
            InputValues: [],
            IntermediateValues: [],
            OutputValues:
            [
                new CalculationTraceValue(
                    Key: fixture.ExpectedValueKeys[0],
                    Label: "Hourly profile",
                    Value: profile,
                    Unit: new CalculationTraceUnit("kWh"),
                    ValueKind: CalculationTraceValueKind.Output)
            ],
            FormulaOrConventionLabel: null,
            Assumptions: [],
            Warnings: [],
            Diagnostics: [],
            ChildSteps: []);

        var document = BuildBaseDocument([step]);
        var sanitized = _sanitizer.Sanitize(
            document,
            CalculationTraceDetailLevel.Summary,
            fixture.MaxCollectionItems ?? 4);

        Assert.IsType<CalculationTraceArraySummary>(sanitized.Steps[0].OutputValues[0].Value);
    }

    [Fact]
    public void PathLikeValuesAreRedacted()
    {
        var value = new CalculationTraceValue(
            Key: "path",
            Label: "Path",
            Value: @"C:\Users\user\secret.txt",
            Unit: null,
            ValueKind: CalculationTraceValueKind.Output);

        var document = BuildBaseDocument(
            [
                new CalculationTraceStep(
                    StepId: "step-1",
                    ModuleKind: CalculationTraceModuleKind.Generic,
                    StepName: "Step",
                    Sequence: 1,
                    InputValues: [],
                    IntermediateValues: [],
                    OutputValues: [value],
                    FormulaOrConventionLabel: null,
                    Assumptions: [],
                    Warnings: [],
                    Diagnostics: [],
                    ChildSteps: [])
            ]);

        var sanitized = _sanitizer.Sanitize(document, CalculationTraceDetailLevel.Standard);
        Assert.Equal("[redacted-path]", sanitized.Steps[0].OutputValues[0].Value);
    }

    private static CalculationTraceDocument BuildDocumentWithNoise()
    {
        var step = new CalculationTraceStep(
            StepId: "step-1",
            ModuleKind: CalculationTraceModuleKind.Generic,
            StepName: " Step ",
            Sequence: 1,
            InputValues: [],
            IntermediateValues: [],
            OutputValues:
            [
                new CalculationTraceValue(" ", " ", null, null, CalculationTraceValueKind.Output),
                new CalculationTraceValue("ok", "ok", 1d, null, CalculationTraceValueKind.Output)
            ],
            FormulaOrConventionLabel: null,
            Assumptions: ["", " a "],
            Warnings: [" ", " w "],
            Diagnostics: [],
            ChildSteps: []);

        return BuildBaseDocument([step]) with
        {
            Assumptions = ["", " root "],
            Warnings = [" ", " root-warning "]
        };
    }

    private static CalculationTraceDocument BuildBaseDocument(
        IReadOnlyList<CalculationTraceStep> steps) =>
        new(
            TraceId: "trace",
            CalculationId: "calc",
            CalculationType: "type",
            CreatedTimestampUtc: DateTimeOffset.UnixEpoch,
            RootModule: CalculationTraceModuleKind.Generic,
            Steps: steps,
            Summary: new CalculationTraceSummary(steps.Count, 0, 0, 0, [CalculationTraceModuleKind.Generic]),
            Assumptions: [],
            Warnings: [],
            Diagnostics: [],
            Metadata: new Dictionary<string, string>(),
            SchemaVersion: "1.0");
}
