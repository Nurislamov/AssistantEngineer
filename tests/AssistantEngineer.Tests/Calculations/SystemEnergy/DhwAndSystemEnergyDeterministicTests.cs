using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public class DhwAndSystemEnergyDeterministicTests
{
    [Fact]
    public void DomesticHotWater_ResidentialSimpleUsesDeterministicFormula()
    {
        var service = new DomesticHotWaterDemandService();

        var result = service.Calculate(new DomesticHotWaterDemandRequest
        {
            PeopleCount = 4,
            LitersPerPersonDay = 50,
            ColdWaterTemperatureC = 10,
            HotWaterTemperatureC = 55,
            DistributionLossFactor = 0
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.Value.DailyVolumeLiters, precision: 6);
        Assert.Equal(10.465, result.Value.DailyEnergyKWh, precision: 3);
        Assert.Equal(
            result.Value.AnnualEnergyKWh,
            result.Value.MonthlyDemand.Sum(month => month.EnergyKWh),
            precision: 3);
        Assert.NotEmpty(result.Value.AssumptionsUsed);
    }

    [Fact]
    public void DomesticHotWater_ZeroOccupancyReturnsZeroWithDiagnostic()
    {
        var service = new DomesticHotWaterDemandService();

        var result = service.Calculate(new DomesticHotWaterDemandRequest
        {
            PeopleCount = 0,
            LitersPerPersonDay = 50,
            ColdWaterTemperatureC = 10,
            HotWaterTemperatureC = 55
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.DailyVolumeLiters, precision: 6);
        Assert.Equal(0, result.Value.AnnualEnergyKWh, precision: 6);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Contains("zero", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DomesticHotWater_InvalidWaterTemperaturesFailValidation()
    {
        var service = new DomesticHotWaterDemandService();

        var result = service.Calculate(new DomesticHotWaterDemandRequest
        {
            PeopleCount = 1,
            ColdWaterTemperatureC = 55,
            HotWaterTemperatureC = 10
        });

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void SystemEnergy_HeatingEfficiencyConvertsUsefulToFinal()
    {
        var engine = new SystemEnergyEngine();

        var result = engine.Calculate(new SystemEnergyInput(
            UsefulHeatingEnergyKWh: 10000,
            HeatingEfficiency: 0.9));

        Assert.True(result.IsSuccess);
        Assert.Equal(11111.111111, result.Value.FinalHeatingEnergyKWh, precision: 6);
    }

    [Fact]
    public void SystemEnergy_CoolingCopConvertsUsefulToFinal()
    {
        var engine = new SystemEnergyEngine();

        var result = engine.Calculate(new SystemEnergyInput(
            UsefulCoolingEnergyKWh: 6000,
            CoolingCop: 3.0));

        Assert.True(result.IsSuccess);
        Assert.Equal(2000, result.Value.FinalCoolingEnergyKWh, precision: 6);
    }

    [Fact]
    public void SystemEnergy_TotalFinalEnergyIncludesFan()
    {
        var engine = new SystemEnergyEngine();

        var result = engine.Calculate(new SystemEnergyInput(
            UsefulHeatingEnergyKWh: 10000,
            UsefulCoolingEnergyKWh: 6000,
            UsefulDhwEnergyKWh: 1000,
            HeatingEfficiency: 1.0,
            CoolingCop: 3.0,
            DhwEfficiency: 1.0,
            FanEnergyKWh: 500));

        Assert.True(result.IsSuccess);
        Assert.Equal(13500, result.Value.TotalFinalEnergyKWh, precision: 6);
    }

    [Fact]
    public void SystemEnergy_PrimaryEnergyFactorIsAppliedWhenAvailable()
    {
        var engine = new SystemEnergyEngine();

        var result = engine.Calculate(new SystemEnergyInput(
            UsefulHeatingEnergyKWh: 1000,
            HeatingEfficiency: 1.0,
            PrimaryEnergyFactor: 2.5));

        Assert.True(result.IsSuccess);
        Assert.Equal(2500, result.Value.PrimaryEnergyKWh!.Value, precision: 6);
    }

    [Fact]
    public void SystemEnergy_MissingAssumptionsReturnUsefulEnergyWithDiagnostics()
    {
        var engine = new SystemEnergyEngine();

        var result = engine.Calculate(new SystemEnergyInput(
            UsefulHeatingEnergyKWh: 1000));

        Assert.True(result.IsSuccess);
        Assert.Equal(1000, result.Value.FinalHeatingEnergyKWh, precision: 6);
        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Warning &&
            diagnostic.Code == "SystemEnergy.HeatingAssumptionMissing");
    }
}
