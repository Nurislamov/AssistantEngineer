using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;

public enum EquipmentDiagnosticBotResponseStatus
{
    Answer,
    ClarificationRequired,
    NotFound,
    ReferenceOnly,
    Unsupported,
    UnsafeOrOutOfScope
}

public enum EquipmentDiagnosticBotEquipmentSide
{
    Indoor,
    Outdoor,
    Chiller,
    Controller,
    CommissioningTool,
    Unknown
}

public enum EquipmentDiagnosticBotDisplayContext
{
    WiredController,
    OduMainBoardLed,
    IduDisplay,
    CentralizedController,
    PortableCommissioningTool,
    MobileAppOrGateway,
    Unknown
}

public sealed record EquipmentDiagnosticBotRequest(
    string? Manufacturer,
    string? Code,
    string? FreeText = null,
    string? Series = null,
    string? ModelCode = null,
    EquipmentCategory? Category = null,
    EquipmentDiagnosticBotEquipmentSide? EquipmentSide = null,
    EquipmentDiagnosticBotDisplayContext? DisplayContext = null,
    string? PreferredLanguage = null,
    IReadOnlyDictionary<string, string>? OperatorProvidedMeasurements = null,
    string? SiteContext = null);

public sealed record EquipmentDiagnosticBotEquipmentContext(
    string Manufacturer,
    string? Series,
    string? ModelCode,
    EquipmentCategory Category,
    EquipmentDiagnosticBotEquipmentSide EquipmentSide,
    EquipmentDiagnosticBotDisplayContext DisplayContext);

public sealed record EquipmentDiagnosticBotObservedCodeContext(
    string Code,
    string NormalizedCode,
    string? FreeText);

public sealed record EquipmentDiagnosticBotClarificationOption(
    string Label,
    string Manufacturer,
    string? Series,
    EquipmentCategory Category,
    EquipmentDiagnosticBotEquipmentSide EquipmentSide,
    EquipmentDiagnosticBotDisplayContext DisplayContext,
    string Code,
    string Explanation,
    string FollowUpPrompt);

public sealed record EquipmentDiagnosticBotClarificationQuestion(
    string Prompt,
    IReadOnlyList<EquipmentDiagnosticBotClarificationOption> Options);

public sealed record EquipmentDiagnosticBotAnswerCard(
    string Title,
    string Summary,
    string VerificationBanner,
    IReadOnlyList<string> LikelyCauses,
    IReadOnlyList<DiagnosticStepDto> DiagnosticSteps,
    IReadOnlyList<RequiredMeasurementDto> RequiredMeasurements,
    IReadOnlyList<string> RecommendedChecks,
    IReadOnlyList<string> OperatorNotes);

public sealed record EquipmentDiagnosticBotSourceCard(
    string SourceType,
    string EvidenceLevel,
    string Summary,
    string? ManualTitle,
    string? ManualVersion,
    string? ManualDocumentCode,
    string? Page,
    string? Section,
    IReadOnlyList<string> Limitations);

public sealed record EquipmentDiagnosticBotSafetyCard(
    string Boundary,
    IReadOnlyList<string> Notes);

public sealed record EquipmentDiagnosticBotResponse(
    EquipmentDiagnosticBotResponseStatus Status,
    string Title,
    string Message,
    string NormalizedManufacturer,
    string NormalizedCode,
    EquipmentDiagnosticBotEquipmentContext? EquipmentContext,
    EquipmentDiagnosticBotObservedCodeContext ObservedCode,
    EquipmentDiagnosticBotAnswerCard? AnswerCard,
    EquipmentDiagnosticBotClarificationQuestion? ClarificationQuestion,
    EquipmentDiagnosticBotSourceCard? SourceCard,
    EquipmentDiagnosticBotSafetyCard SafetyCard,
    bool VerificationRequired,
    DiagnosticConfidence Confidence,
    bool IsManualVerified,
    bool IsSeedKnowledge,
    IReadOnlyList<string> OperatorNextSteps,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string>? InternalDecisionTrace)
{
    public IReadOnlyList<string> ApplicableContexts { get; init; } = [];
}
