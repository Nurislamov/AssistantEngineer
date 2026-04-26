namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

public sealed class BuildingAutocorrectionResult
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public int AppliedActionsCount { get; set; }
    public List<BuildingAutocorrectionAction> AppliedActions { get; set; } = new();
    public BuildingValidationReport ValidationReport { get; set; } = new();
}