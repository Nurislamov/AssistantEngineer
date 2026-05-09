using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Trace;

public interface ICalculationTraceBuilder
{
    CalculationTraceDetailLevel DetailLevel { get; }

    void SetDetailLevel(CalculationTraceDetailLevel detailLevel);

    void Initialize(
        string traceId,
        string calculationType,
        CalculationTraceModuleKind rootModule,
        string? calculationId = null,
        DateTimeOffset? createdTimestampUtc = null,
        IReadOnlyDictionary<string, string>? metadata = null);

    string AddStep(
        CalculationTraceModuleKind moduleKind,
        string stepName,
        string? formulaOrConventionLabel = null,
        string? parentStepId = null,
        string? stepId = null,
        double? durationMilliseconds = null);

    void AddInputValue(string stepId, CalculationTraceValue value);

    void AddIntermediateValue(string stepId, CalculationTraceValue value);

    void AddOutputValue(string stepId, CalculationTraceValue value);

    void AddAssumption(string stepId, string assumption);

    void AddWarning(string stepId, string warning);

    void AddDiagnostic(string stepId, CalculationTraceDiagnostic diagnostic);

    void AddDocumentAssumption(string assumption);

    void AddDocumentWarning(string warning);

    void AddDocumentDiagnostic(CalculationTraceDiagnostic diagnostic);

    void Merge(CalculationTraceDocument document, string? parentStepId = null);

    CalculationTraceDocument Build();
}
