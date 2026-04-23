namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;

public sealed class CatalogAutosizingScopeResponse
{
    public int? ScopeId { get; set; }
    public string ScopeName { get; set; } = string.Empty;
    public string? ParentScopeName { get; set; }

    public double RequiredCapacityKw { get; set; }

    public List<CatalogAutosizingOptionResponse> Recommendations { get; set; } = new();
}