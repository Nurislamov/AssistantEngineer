using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Diagnostics;

public enum DiagnosticCoreStatus
{
    Answer,
    ClarificationRequired,
    NotFound,
    ReferenceOnly,
    Unsupported,
    UnsafeOrOutOfScope
}

public enum DiagnosticAudience
{
    Consumer,
    Installer,
    Engineer
}

public enum DiagnosticEquipmentSide
{
    Indoor,
    Outdoor,
    Chiller,
    Controller,
    CommissioningTool,
    Unknown
}

public enum DiagnosticDisplayContext
{
    WiredController,
    OduMainBoardLed,
    IduDisplay,
    CentralizedController,
    PortableCommissioningTool,
    MobileAppOrGateway,
    Unknown
}

public sealed record DiagnosticCoreRequest(
    string? Manufacturer,
    string? Code,
    string? FreeText = null,
    string? Series = null,
    string? ModelCode = null,
    EquipmentCategory? Category = null,
    DiagnosticEquipmentSide? EquipmentSide = null,
    DiagnosticDisplayContext? DisplayContext = null,
    string? PreferredLanguage = null,
    IReadOnlyDictionary<string, string>? OperatorProvidedMeasurements = null,
    string? SiteContext = null);

public sealed record DiagnosticMatchIdentity(
    string Manufacturer,
    string? Series,
    string? ModelCode,
    EquipmentCategory Category,
    DiagnosticEquipmentSide EquipmentSide,
    DiagnosticDisplayContext DisplayContext);

public sealed record DiagnosticObservedCode(
    string ObservedCode,
    string CanonicalCode,
    string? FreeText);

public sealed record DiagnosticAmbiguityCandidate(
    string Label,
    string Manufacturer,
    string? Series,
    EquipmentCategory Category,
    DiagnosticEquipmentSide EquipmentSide,
    DiagnosticDisplayContext DisplayContext,
    string Code,
    string Explanation,
    string FollowUpPrompt);

public sealed record DiagnosticAmbiguity(
    string Prompt,
    IReadOnlyList<DiagnosticAmbiguityCandidate> Candidates);

public sealed record DiagnosticAnswer(
    string Title,
    string Summary,
    string VerificationBanner,
    IReadOnlyList<string> LikelyCauses,
    IReadOnlyList<DiagnosticStepDto> DiagnosticSteps,
    IReadOnlyList<RequiredMeasurementDto> RequiredMeasurements,
    IReadOnlyList<string> RecommendedChecks,
    IReadOnlyList<string> OperatorNotes);

public sealed record DiagnosticSource(
    string SourceType,
    string EvidenceLevel,
    string Summary,
    string? ManualTitle,
    string? ManualVersion,
    string? ManualDocumentCode,
    string? Page,
    string? Section,
    IReadOnlyList<string> Limitations);

public sealed record DiagnosticSourceReference(
    string SourceName,
    string? DocumentCode,
    string SourceReference,
    string SourceType,
    string SourceLanguage,
    string VerificationStatus,
    string Confidence,
    string? ManualId,
    string? PackageId,
    string? Notes);

public sealed record DiagnosticLocalizedGuidance(
    string Locale,
    DiagnosticAudience Audience,
    string Title,
    string? Meaning,
    string Summary,
    string SafetyNote,
    IReadOnlyList<string> Causes,
    IReadOnlyList<string> Checks,
    IReadOnlyList<string> DoNotAdvise,
    string RecommendedAction);

public sealed record DiagnosticSafety(
    string Boundary,
    IReadOnlyList<string> Notes);

public sealed record DiagnosticCoreResult(
    DiagnosticCoreStatus Status,
    string Title,
    string Message,
    string NormalizedManufacturer,
    string CanonicalCode,
    DiagnosticMatchIdentity? Match,
    DiagnosticObservedCode ObservedCode,
    DiagnosticAnswer? Answer,
    DiagnosticAmbiguity? Ambiguity,
    DiagnosticSource? Source,
    DiagnosticSafety Safety,
    bool VerificationRequired,
    DiagnosticConfidence Confidence,
    bool IsManualVerified,
    bool IsSeedKnowledge,
    IReadOnlyList<string> NextSteps,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string>? InternalDecisionTrace)
{
    public ErrorKnowledgeSignalType? SignalType { get; init; }
    public ErrorKnowledgeSeverity? Severity { get; init; }
    public IReadOnlyList<string> ApplicableContexts { get; init; } = [];
    public IReadOnlyList<DiagnosticLocalizedGuidance> LocalizedGuidance { get; init; } = [];
    public IReadOnlyList<DiagnosticSourceReference> SourceReferences { get; init; } = [];
}
