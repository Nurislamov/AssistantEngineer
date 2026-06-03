namespace AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

public sealed record DiagnosticCase(
    EquipmentErrorCode ErrorCode,
    IReadOnlyList<string> LikelyCauses,
    IReadOnlyList<DiagnosticStep> DiagnosticSteps,
    IReadOnlyList<RequiredMeasurement> RequiredMeasurements,
    IReadOnlyList<string> SafetyNotes,
    IReadOnlyList<ManualReference> ManualReferences,
    DiagnosticConfidence Confidence);
