namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

public sealed class BuildingAutocorrectionPreview
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public int ProposedActionsCount { get; set; }
    public List<BuildingAutocorrectionAction> Actions { get; set; } = new();
    public BuildingValidationReport ValidationReport { get; set; } = new();
}