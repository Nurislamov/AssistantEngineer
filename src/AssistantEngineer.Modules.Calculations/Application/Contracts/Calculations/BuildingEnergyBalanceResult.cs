namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;

public class BuildingEnergyBalanceResult
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public string CoolingCalculationMethod { get; set; } = string.Empty;
    public string HeatingCalculationMethod { get; set; } = string.Empty;
    public double AnnualCoolingDemandKWh { get; set; }
    public double AnnualHeatingDemandKWh { get; set; }
    public double AnnualTotalDemandKWh { get; set; }
    public List<MonthlyEnergyBalance> MonthlyBalances { get; set; } = new();
}