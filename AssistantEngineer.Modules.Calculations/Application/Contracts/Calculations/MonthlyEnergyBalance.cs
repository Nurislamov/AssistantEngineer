namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;

public class MonthlyEnergyBalance
{
    public int Month { get; set; }
    public double CoolingDemandKWh { get; set; }
    public double HeatingDemandKWh { get; set; }
}