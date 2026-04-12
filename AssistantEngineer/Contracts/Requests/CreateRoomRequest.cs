using System.ComponentModel.DataAnnotations;

namespace AssistantEngineer.Contracts.Requests;

public class CreateRoomRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 10000)]
    public double AreaM2 { get; set; }

    [Range(1, 20)]
    public double HeightM { get; set; }

    [Range(-50, 100)]
    public double IndoorTemperatureC { get; set; }

    [Range(-60, 100)]
    public double OutdoorTemperatureC { get; set; }

    [Range(0, 1000)]
    public int PeopleCount { get; set; }

    [Range(0, 1_000_000)]
    public double EquipmentLoadW { get; set; }

    [Range(0, 1_000_000)]
    public double LightingLoadW { get; set; }
    
    [Range(1, int.MaxValue)]
    public int FloorId { get; set; }
}