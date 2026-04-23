namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

public sealed class ThermalZoneResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BuildingId { get; set; }
    public List<ThermalZoneRoomResponse> Rooms { get; set; } = new();
}