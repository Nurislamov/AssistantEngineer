namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.ManualCodeBook;

public enum EquipmentDiagnosticsManualCodeKind
{
    Fault,
    Protection,
    Status,
    Debugging,
    Query,
    Setting,
    DisplayPattern,
    NonFault,
    Parameter,
    ToolFunction,
    Unknown
}

public enum EquipmentDiagnosticsManualCodeEquipmentSide
{
    Indoor,
    Outdoor,
    Controller,
    CommissioningTool,
    System,
    OwnerManual,
    TechnicalGuide
}

public enum EquipmentDiagnosticsManualCodeDisplayContext
{
    WiredController,
    OduMainBoardLed,
    IduDisplay,
    PortableCommissioningTool,
    CentralizedController,
    RemoteController,
    TechnicalDocument,
    Unknown
}

public enum EquipmentDiagnosticsManualCodePromotionReadiness
{
    ReferenceOnly,
    NeedsTroubleshootingEvidence,
    ReadyForStagingCandidate,
    ReadyForEngineeringReview,
    Blocked
}

public enum EquipmentDiagnosticsManualCodeEvidenceLevel
{
    ErrorIndicationTable,
    TroubleshootingSection,
    DebuggingProcedure,
    ControllerOperationSection,
    TechnicalGuideApplicability,
    OwnerManualCommonMalfunction,
    Unknown
}

public sealed record EquipmentDiagnosticsManualCodeOccurrence(
    string ManualId,
    string SourceFileName,
    string SourceTitle,
    string? SourceDocumentCode,
    string? SourceVersion,
    string Page,
    string Section,
    string Code,
    string NormalizedCode,
    EquipmentDiagnosticsManualCodeKind CodeKind,
    EquipmentDiagnosticsManualCodeEquipmentSide EquipmentSide,
    EquipmentDiagnosticsManualCodeDisplayContext DisplayContext,
    string Series,
    string Meaning,
    string? JudgmentConditionSummary,
    string? PossibleCauseSummary,
    string? TroubleshootingSummary,
    string? NonFaultExplanation,
    IReadOnlyList<string> RequiredMeasurements,
    IReadOnlyList<string> SafetyNotes,
    IReadOnlyList<string> Limitations,
    bool CanBecomeDiagnosticCase,
    EquipmentDiagnosticsManualCodePromotionReadiness PromotionReadiness,
    EquipmentDiagnosticsManualCodeEvidenceLevel EvidenceLevel,
    string? ShortQuote,
    string? Notes);

public sealed record EquipmentDiagnosticsManualCodeBook(
    IReadOnlyList<EquipmentDiagnosticsManualCodeOccurrence> Occurrences);
