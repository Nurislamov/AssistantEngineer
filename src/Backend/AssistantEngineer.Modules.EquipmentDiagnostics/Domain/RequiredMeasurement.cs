namespace AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

public sealed record RequiredMeasurement(
    string Name,
    string Unit,
    string Description,
    bool RequiredBeforeConclusion);
