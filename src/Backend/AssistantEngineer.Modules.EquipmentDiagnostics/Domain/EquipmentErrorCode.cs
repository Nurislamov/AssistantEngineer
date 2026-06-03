namespace AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

public sealed record EquipmentErrorCode(
    EquipmentManufacturer Manufacturer,
    string? SeriesName,
    string? ModelCode,
    string Code,
    string NormalizedCode,
    string Title,
    string Meaning,
    string Severity,
    DiagnosticConfidence Confidence,
    ManualReference SourceManual);
