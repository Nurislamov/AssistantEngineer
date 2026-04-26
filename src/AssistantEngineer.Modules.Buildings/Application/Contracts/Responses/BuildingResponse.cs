namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

public class BuildingResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public int? ClimateZoneId { get; set; }
    public string? ClimateZoneName { get; set; }
}