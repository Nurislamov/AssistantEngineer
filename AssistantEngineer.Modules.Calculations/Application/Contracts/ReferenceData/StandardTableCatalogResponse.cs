namespace AssistantEngineer.Modules.Calculations.Application.Contracts.ReferenceData;

public sealed class StandardTableCatalogResponse
{
    public DateTimeOffset GeneratedAtUtc { get; set; }
    public List<StandardTableCatalogItemResponse> Items { get; set; } = new();
}

public sealed class StandardTableCatalogItemResponse
{
    public string TableKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}