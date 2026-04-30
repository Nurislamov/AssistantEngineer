namespace AssistantEngineer.Modules.Calculations.Application.Contracts.EquipmentSizing;

public sealed record EquipmentSizingRejectedItem(
    int EquipmentId,
    string Name,
    string Model,
    IReadOnlyList<string> Reasons);
