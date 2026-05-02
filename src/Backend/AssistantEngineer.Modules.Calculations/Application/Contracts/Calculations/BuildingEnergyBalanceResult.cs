using System.Text.Json.Serialization;
using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;

public class BuildingEnergyBalanceResult
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public string CoolingCalculationMethod { get; set; } = string.Empty;
    public string HeatingCalculationMethod { get; set; } = string.Empty;
    public string RequestedCoolingMethod { get; set; } = string.Empty;
    public string RequestedHeatingMethod { get; set; } = string.Empty;
    public string ActualMethod { get; set; } = string.Empty;
    public string CalculationMethodLabel { get; set; } = string.Empty;
    public string EnergyDataSource { get; set; } = string.Empty;
    public bool IsTrueHourly8760 { get; set; }
    public int HourlyRecordCount { get; set; }
    public double AnnualCoolingDemandKWh { get; set; }
    public double AnnualHeatingDemandKWh { get; set; }
    public double AnnualTotalDemandKWh { get; set; }
    public double EnergyUseIntensityKWhPerM2Year { get; set; }
    public double PeakHeatingW { get; set; }
    public double PeakCoolingW { get; set; }
    public int? PeakHeatingHour { get; set; }
    public int? PeakCoolingHour { get; set; }
    public AnnualEnergyComponentBreakdown? ComponentBreakdown { get; set; }
    public List<MonthlyEnergyBalance> MonthlyBalances { get; set; } = new();
    public List<CalculationDiagnostic> Diagnostics { get; set; } = new();
    public List<string> Assumptions { get; set; } = new();

    [JsonIgnore]
    public List<AnnualEnergyBalanceHourInput> HourlyBalanceRecords { get; set; } = new();
}
