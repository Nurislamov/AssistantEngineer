namespace AssistantEngineer.Contracts.Requests;

public class CreateRoomRequest
{
    public string Name { get; set; } = string.Empty;

    public double AreaM2 { get; set; }

    public double HeightM { get; set; }

    public double IndoorTemperatureC { get; set; }

    public double OutdoorTemperatureC { get; set; }

    public int PeopleCount { get; set; }

    public double EquipmentLoadW { get; set; }

    public double LightingLoadW { get; set; }

    public int FloorId { get; set; }
}
