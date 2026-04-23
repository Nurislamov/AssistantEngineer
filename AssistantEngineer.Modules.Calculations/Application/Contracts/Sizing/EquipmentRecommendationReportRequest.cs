namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class EquipmentRecommendationReportRequest
{
    public string ScenarioName { get; set; } = "Base scenario";
    public EquipmentRecommendationRequest Request { get; set; } = new();
}