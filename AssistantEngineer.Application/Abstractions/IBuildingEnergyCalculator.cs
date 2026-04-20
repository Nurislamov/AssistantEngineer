using AssistantEngineer.Domain.Models;

namespace AssistantEngineer.Application.Abstractions;

public interface IBuildingEnergyCalculator
{
    Task<BuildingEnergyBalanceResult> CalculateAsync(
        Building building,
        CoolingLoadCalculationMethod coolingMethod,
        HeatingLoadCalculationMethod heatingMethod,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default);
}

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

public class MonthlyEnergyBalance
{
    public int Month { get; set; }
    public double CoolingDemandKWh { get; set; }
    public double HeatingDemandKWh { get; set; }
}