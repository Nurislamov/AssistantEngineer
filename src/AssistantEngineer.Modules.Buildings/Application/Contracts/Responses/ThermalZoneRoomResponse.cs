namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

public sealed class ThermalZoneRoomResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int FloorId { get; set; }
}