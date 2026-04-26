namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;

public class CreateBuildingRequest
{
    public string Name { get; set; } = string.Empty;
    public int? ClimateZoneId { get; set; }
}