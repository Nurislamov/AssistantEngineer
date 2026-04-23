namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class EquipmentRecommendationComparisonScopeResponse
{
    public int? ScopeId { get; set; }
    public string ScopeName { get; set; } = string.Empty;
    public string? ParentScopeName { get; set; }

    public string WinningScenarioName { get; set; } = string.Empty;
    public double WinningCompositeScore { get; set; }

    public List<EquipmentRecommendationComparisonScopeScenarioResponse> Scenarios { get; set; } = new();
}