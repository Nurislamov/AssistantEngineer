using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;

namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

public class RoomResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double AreaM2 { get; set; }
    public double HeightM { get; set; }
    public double VolumeM3 { get; set; }
    public double IndoorTemperatureC { get; set; }
    public double? OutdoorTemperatureOverrideC { get; set; }
    public int PeopleCount { get; set; }
    public double EquipmentLoadW { get; set; }
    public double LightingLoadW { get; set; }
    public RoomTypeDto Type { get; set; }
    public int FloorId { get; set; }
}
