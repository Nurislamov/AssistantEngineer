namespace AssistantEngineer.Modules.Calculations.Application.Contracts.EquipmentSizing;

public sealed record EquipmentSizingCandidateInput(
    int EquipmentId,
    string Name,
    string Model,
    string EquipmentType,
    double? HeatingCapacityW,
    double? CoolingCapacityW,
    bool IsActive = true);
