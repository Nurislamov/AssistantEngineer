using System.Reflection;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity;

public class Iso52016EnergyCalculationContractTests
{
    [Fact]
    public void AnnualEnergyNeedResultContainsHourlyAndMonthlyResults()
    {
        var constructor = Assert.Single(
            typeof(Iso52016AnnualEnergyNeedResult).GetConstructors());

        var parameterNames = GetConstructorParameterNames(constructor);

        Assert.Contains("HourlyResults", parameterNames);
        Assert.Contains("MonthlyResults", parameterNames);
        Assert.Contains("AnnualHeatingDemandKWh", parameterNames);
        Assert.Contains("AnnualCoolingDemandKWh", parameterNames);
    }

    [Fact]
    public void HourlyEnergyNeedContainsCoreHourlyOutputs()
    {
        var constructor = Assert.Single(
            typeof(Iso52016HourlyEnergyNeed).GetConstructors());

        var parameterNames = GetConstructorParameterNames(constructor);

        Assert.Contains("HourOfYear", parameterNames);
        Assert.Contains("HeatingLoadW", parameterNames);
        Assert.Contains("CoolingLoadW", parameterNames);
        Assert.Contains("OperativeTemperatureC", parameterNames);
        Assert.Contains("OutdoorTemperatureC", parameterNames);
        Assert.Contains("InternalGainsW", parameterNames);
        Assert.Contains("SolarGainsW", parameterNames);
    }

    [Fact]
    public void MonthlyEnergyNeedContainsHeatingAndCoolingDemand()
    {
        var constructor = Assert.Single(
            typeof(Iso52016MonthlyEnergyNeed).GetConstructors());

        var parameterNames = GetConstructorParameterNames(constructor);

        Assert.Contains("Month", parameterNames);
        Assert.Contains("HeatingDemandKWh", parameterNames);
        Assert.Contains("CoolingDemandKWh", parameterNames);
    }

    [Fact]
    public void AnnualEnergyNeedResultSupportsZoneAndRoomHourlyResults()
    {
        var constructor = Assert.Single(
            typeof(Iso52016AnnualEnergyNeedResult).GetConstructors());

        var parameterNames = GetConstructorParameterNames(constructor);

        Assert.Contains("ZoneHourlyResults", parameterNames);
        Assert.Contains("RoomHourlyResults", parameterNames);
    }

    private static HashSet<string> GetConstructorParameterNames(
        ConstructorInfo constructor) =>
        constructor
            .GetParameters()
            .Select(parameter => parameter.Name)
            .Where(name => name is not null)
            .Select(name => name!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}