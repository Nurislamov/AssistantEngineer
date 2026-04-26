using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;

namespace AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Heating;

public class BuildingHeatingReport
{
    public string ProjectName { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;
    public string CalculationMethod { get; set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; set; }

    public double OutdoorDesignTemperatureC { get; set; }
    public double IndoorDesignTemperatureC { get; set; }
    public int RoomsCount { get; set; }

    public double TotalTransmissionLossW { get; set; }
    public double TotalVentilationLossW { get; set; }
    public double TotalDesignHeatingLoadW { get; set; }
    public double TotalDesignHeatingLoadKw { get; set; }

    public List<RoomHeatingLoadResult> Rooms { get; set; } = [];
}