namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

public sealed class BuildingAutocorrectionAction
{
    public string Code { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public bool Applied { get; set; }
}