using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Api.Services.Calculations;

namespace AssistantEngineer.Tests.Calculations;

public class EngineeringCalculationJobPayloadCodecTests
{
    [Fact]
    public void DeserializeJobRequest_ReturnsNullForInvalidPayload()
    {
        var codec = new EngineeringCalculationJobPayloadCodec();

        var result = codec.DeserializeJobRequest("{invalid");

        Assert.Null(result);
    }

    [Fact]
    public void DeserializeDiagnostics_ReturnsEmptyForInvalidPayload()
    {
        var codec = new EngineeringCalculationJobPayloadCodec();

        var result = codec.DeserializeDiagnostics("{invalid");

        Assert.Empty(result);
    }

    [Fact]
    public void SortAndDistinctDiagnostics_DeduplicatesAndSortsBySeverity()
    {
        var codec = new EngineeringCalculationJobPayloadCodec();

        var diagnostics = new[]
        {
            new EngineeringWorkflowDiagnosticDto("warning", "W1", "warning message", "Validation"),
            new EngineeringWorkflowDiagnosticDto("error", "E1", "error message", "Validation"),
            new EngineeringWorkflowDiagnosticDto("warning", "W1", "warning message", "Validation"),
            new EngineeringWorkflowDiagnosticDto("info", "I1", "info message", "Project")
        };

        var result = codec.SortAndDistinctDiagnostics(diagnostics);

        Assert.Equal(3, result.Count);
        Assert.Equal("E1", result[0].Code);
        Assert.Equal("W1", result[1].Code);
        Assert.Equal("I1", result[2].Code);
    }

    [Fact]
    public void SerializeAndDeserializeScenarioResult_RoundTripsCoreFields()
    {
        var codec = new EngineeringCalculationJobPayloadCodec();
        var source = CreateScenarioResult();

        var payload = codec.Serialize(source);
        var roundtrip = codec.DeserializeScenarioResult(payload);

        Assert.NotNull(roundtrip);
        Assert.Equal(source.ScenarioId, roundtrip!.ScenarioId);
        Assert.Equal(source.Status, roundtrip.Status);
        Assert.Equal(source.ModuleResults.Count, roundtrip.ModuleResults.Count);
        Assert.Equal(source.ValidationDiagnostics.Count, roundtrip.ValidationDiagnostics.Count);
    }

    private static EngineeringCalculationScenarioResultDto CreateScenarioResult()
    {
        return new EngineeringCalculationScenarioResultDto(
            ScenarioId: "scenario-codec",
            Status: EngineeringCalculationExecutionStatus.CompletedWithWarnings,
            Executed: true,
            ExecutedModules: ["Ventilation"],
            SkippedModules: [],
            UnavailableModules: [],
            ValidationDiagnostics:
            [
                new EngineeringWorkflowDiagnosticDto("warning", "W1", "warning message", "Validation")
            ],
            Assumptions: ["assumption"],
            Warnings: ["warning"],
            ModuleSummaries: new EngineeringCalculationModuleSummariesDto(TopologySummary: "ok"),
            ModuleResults:
            [
                new EngineeringCalculationModuleExecutionResultDto(
                    ModuleKind: "Ventilation",
                    Status: EngineeringCalculationModuleExecutionStatus.Executed,
                    SummaryValues:
                    [
                        new EngineeringCalculationModuleValueDto("k", "label", 1.0, "unit")
                    ],
                    Diagnostics: [],
                    Assumptions: [],
                    Warnings: [],
                    DurationMilliseconds: 1.0,
                    SourceServiceName: "test")
            ],
            Timings:
            [
                new EngineeringCalculationModuleTimingDto("Ventilation", 1.0)
            ],
            CalculationTrace: null,
            CalculationTraceSummary: null,
            EngineeringReport: null,
            ReportPreview: null,
            ReportJson: null,
            ReportMarkdown: null,
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["mode"] = "test"
            });
    }
}
