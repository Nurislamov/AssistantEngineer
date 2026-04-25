namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

public sealed class BuildingValidationReport
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public int ErrorsCount { get; set; }
    public int WarningsCount { get; set; }
    public int AutoFixableIssuesCount { get; set; }
    public List<BuildingValidationIssue> Issues { get; set; } = new();
}