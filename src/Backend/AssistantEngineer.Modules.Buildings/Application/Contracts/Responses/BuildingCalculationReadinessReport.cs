namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

public sealed class BuildingCalculationReadinessReport
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public bool IsReady { get; set; }
    public List<BuildingCalculationReadinessIssue> Issues { get; set; } = new();
}