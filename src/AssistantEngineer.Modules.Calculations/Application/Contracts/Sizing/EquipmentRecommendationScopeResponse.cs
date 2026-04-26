namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class EquipmentRecommendationScopeResponse
{
    public int? ScopeId { get; set; }
    public string ScopeName { get; set; } = string.Empty;
    public string? ParentScopeName { get; set; }

    public double RequiredCapacityKw { get; set; }

    public List<EquipmentRecommendationOptionResponse> Recommendations { get; set; } = new();
}