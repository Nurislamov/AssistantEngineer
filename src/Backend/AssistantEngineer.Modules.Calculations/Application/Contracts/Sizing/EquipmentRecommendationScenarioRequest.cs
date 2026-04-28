namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class EquipmentRecommendationScenarioRequest
{
    public string ScenarioName { get; set; } = string.Empty;
    public EquipmentRecommendationRequest Request { get; set; } = new();
}