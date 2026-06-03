namespace AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

public sealed record DiagnosticStep(
    int Order,
    string Title,
    string Instruction,
    string ExpectedResult,
    string IfFailedAction);
