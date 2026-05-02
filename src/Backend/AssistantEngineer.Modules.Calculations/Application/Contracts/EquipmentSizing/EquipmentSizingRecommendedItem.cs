namespace AssistantEngineer.Modules.Calculations.Application.Contracts.EquipmentSizing;

public sealed record EquipmentSizingRecommendedItem(
    int EquipmentId,
    string Name,
    string Model,
    double? HeatingCapacityW,
    double? CoolingCapacityW,
    double HeatingMarginW,
    double CoolingMarginW,
    double? HeatingMarginPercent,
    double? CoolingMarginPercent,
    double Score,
    IReadOnlyList<string> Notes);
