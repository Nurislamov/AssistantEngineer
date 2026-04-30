using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;

public class RoomHeatingLoadResult
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string CalculationMethod { get; set; } = string.Empty;
    public string RequestedMethod { get; set; } = string.Empty;
    public string ActualMethod { get; set; } = string.Empty;
    public string CalculationMethodLabel { get; set; } = string.Empty;

    public double IndoorDesignTemperatureC { get; set; }
    public double OutdoorDesignTemperatureC { get; set; }
    public double DeltaTemperatureC { get; set; }
    public double VolumeM3 { get; set; }
    public double AirChangesPerHour { get; set; }
    public double EffectiveAirChangesPerHour { get; set; }
    public double EffectiveMechanicalAirflowM3PerHour { get; set; }
    public double EffectiveInfiltrationAirChangesPerHour { get; set; }
    public double EffectiveInfiltrationAirflowM3PerHour { get; set; }
    public string VentilationAssumptionSource { get; set; } = string.Empty;

    public double TransmissionHeatLossW { get; set; }
    public double VentilationHeatLossW { get; set; }
    public double MechanicalVentilationHeatLossW { get; set; }
    public double InfiltrationHeatLossW { get; set; }
    public double NaturalVentilationHeatLossW { get; set; }
    public double TotalDesignHeatingLoadW { get; set; }
    public double TotalDesignHeatingLoadKw { get; set; }
    public double HeatingLoadW { get; set; }
    public double HeatingLoadWPerM2 { get; set; }
    public RoomHeatingLoadBreakdown? Breakdown { get; set; }
    public List<CalculationDiagnostic> Diagnostics { get; set; } = new();
    public List<string> Assumptions { get; set; } = new();
}
