using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge;

public sealed record EquipmentDiagnosticsKnowledgeEntry(
    string Manufacturer,
    string? SeriesName,
    string? ModelCode,
    EquipmentCategory Category,
    string Code,
    string Title,
    string Meaning,
    string Severity,
    DiagnosticConfidence Confidence,
    IReadOnlyList<string> LikelyCauses,
    IReadOnlyList<DiagnosticStep> DiagnosticSteps,
    IReadOnlyList<RequiredMeasurement> RequiredMeasurements,
    IReadOnlyList<string> SafetyNotes,
    IReadOnlyList<ManualReference> ManualReferences,
    IReadOnlyList<string>? Tags = null);
