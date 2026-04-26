namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;

public sealed class UpdateThermalZoneRequest
{
    public string Name { get; set; } = string.Empty;
    public List<int> RoomIds { get; set; } = new();
}