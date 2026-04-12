namespace AssistantEngineer.Models;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public double AreaM2 { get; set; }
    public double HeightM { get; set; }
    public double VolumeM3 { get; set; }

    public double IndoorTemperatureC { get; set; }
    public double OutdoorTemperatureC { get; set; }

    public int PeopleCount { get; set; }
    public double EquipmentLoadW { get; set; }
    public double LightingLoadW { get; set; }

    public double ReserveFactor { get; set; }
    public double DesignCapacityW { get; set; }
    public double DesignCapacityKw { get; set; }

    public List<Window> Windows { get; set; } = [];
    public List<Wall> Walls { get; set; } = [];
    
    public int FloorId { get; set; }
    public Floor Floor { get; set; } = null!;
}
