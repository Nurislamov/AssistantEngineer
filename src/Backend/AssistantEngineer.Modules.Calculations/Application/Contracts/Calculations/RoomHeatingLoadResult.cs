namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;

public class RoomHeatingLoadResult
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string CalculationMethod { get; set; } = string.Empty;

    public double IndoorDesignTemperatureC { get; set; }
    public double OutdoorDesignTemperatureC { get; set; }
    public double DeltaTemperatureC { get; set; }
    public double VolumeM3 { get; set; }
    public double AirChangesPerHour { get; set; }

    public double TransmissionHeatLossW { get; set; }
    public double VentilationHeatLossW { get; set; }
    public double MechanicalVentilationHeatLossW { get; set; }
    public double InfiltrationHeatLossW { get; set; }
    public double NaturalVentilationHeatLossW { get; set; }
    public double TotalDesignHeatingLoadW { get; set; }
    public double TotalDesignHeatingLoadKw { get; set; }
}
