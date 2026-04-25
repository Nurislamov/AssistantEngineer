namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;

public sealed class CreateBuildingFromArchetypeRequest
{
    public string ArchetypeCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? ClimateZoneId { get; set; }
}