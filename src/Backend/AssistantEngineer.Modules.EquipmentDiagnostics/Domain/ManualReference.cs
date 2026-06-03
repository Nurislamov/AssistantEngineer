namespace AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

public sealed record ManualReference(
    string Manufacturer,
    string ManualTitle,
    string? ManualVersion,
    string? Page,
    string? Notes);
