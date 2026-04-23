using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Profiles;

public sealed class En16798RoomUsageProfileResponse
{
    public RoomTypeDto RoomType { get; set; }
    public En16798ProfileCategory Category { get; set; }

    public double HeatingSetpointOccupiedC { get; set; }
    public double CoolingSetpointOccupiedC { get; set; }
    public double OutdoorAirLPerPerson { get; set; }

    public List<double> OccupancyFactors { get; set; } = new();
    public List<double> EquipmentFactors { get; set; } = new();
    public List<double> LightingFactors { get; set; } = new();
    public List<double> VentilationFactors { get; set; } = new();
}