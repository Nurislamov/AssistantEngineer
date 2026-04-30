using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;

public class RoomCalculationResult
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string CalculationMethod { get; set; } = string.Empty;
    public string RequestedMethod { get; set; } = string.Empty;
    public string ActualMethod { get; set; } = string.Empty;
    public string CalculationMethodLabel { get; set; } = string.Empty;
    public int? PeakHour { get; set; }

    public double AreaM2 { get; set; }
    public double HeightM { get; set; }
    public double VolumeM3 { get; set; }
    public double IndoorTemperatureC { get; set; }
    public double OutdoorTemperatureC { get; set; }
    public int PeopleCount { get; set; }
    public double EquipmentLoadW { get; set; }
    public double LightingLoadW { get; set; }

    public double TotalWindowAreaM2 { get; set; }
    public double TotalWallAreaM2 { get; set; }
    public double ExternalWallAreaM2 { get; set; }

    public double BaseRoomLoadW { get; set; }
    public double WindowHeatGainW { get; set; }
    public double WallHeatGainW { get; set; }
    public double VentilationHeatGainW { get; set; }
    public double InfiltrationHeatGainW { get; set; }
    public double NaturalVentilationHeatGainW { get; set; }
    public double PeopleHeatGainW { get; set; }
    public double EquipmentHeatGainW { get; set; }
    public double LightingHeatGainW { get; set; }
    public double InternalHeatGainW { get; set; }

    public double TotalHeatLoadW { get; set; }
    public double TotalHeatLoadKw { get; set; }
    public double CoolingLoadW { get; set; }
    public double CoolingLoadWPerM2 { get; set; }

    public double DeltaTemperatureC { get; set; }
    public double HeightAdjustmentFactor { get; set; }
    public double TemperatureAdjustmentFactor { get; set; }

    public double DesignReserveFactor { get; set; }
    public double DesignCapacityW { get; set; }
    public double DesignCapacityKw { get; set; }

    public List<double> HourlyHeatLoadW { get; set; } = new();
    public RoomCoolingLoadBreakdown? Breakdown { get; set; }
    public List<CalculationDiagnostic> Diagnostics { get; set; } = new();
    public List<string> Assumptions { get; set; } = new();
}
