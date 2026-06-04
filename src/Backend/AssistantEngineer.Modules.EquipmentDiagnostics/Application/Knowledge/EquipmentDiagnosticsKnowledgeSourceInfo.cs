namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge;

public sealed record EquipmentDiagnosticsKnowledgeSourceInfo(
    string SourceType,
    string EvidenceLevel,
    string? ManualTitle,
    string? ManualVersion,
    string? ManualDocumentCode,
    string? Page,
    string? Section,
    string? Quote,
    string? Notes,
    IReadOnlyList<string> Limitations,
    IReadOnlyList<string> ApplicableModels,
    IReadOnlyList<string> ApplicableSeries);
