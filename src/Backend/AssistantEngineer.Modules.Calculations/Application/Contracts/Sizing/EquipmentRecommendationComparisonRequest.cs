namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class EquipmentRecommendationComparisonRequest
{
    public List<EquipmentRecommendationScenarioRequest> Scenarios { get; set; } = new();
}