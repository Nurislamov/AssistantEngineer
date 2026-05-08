using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class DomesticHotWaterSystemEnergyHandoffAdapterTests
{
    private readonly DomesticHotWaterSystemEnergyHandoffAdapter _adapter = new();

    [Fact]
    public void AdaptsDhwHandoffToUsefulLoadSet()
    {
        var handoff = SystemEnergyTestData.CreateDhwHandoff(systemLoadHourly: 1.25);

        var result = _adapter.BuildUsefulLoadSet(handoff);

        var usefulLoad = Assert.Single(result.UsefulLoads);
        Assert.Equal(SystemEnergyEndUse.DomesticHotWater, usefulLoad.EndUse);
        Assert.Equal(1.25, usefulLoad.HourlyUsefulEnergyKWh8760[0], 6);
    }

    [Fact]
    public void AdaptsDhwAuxiliaryElectricity()
    {
        var handoff = SystemEnergyTestData.CreateDhwHandoff(auxiliaryHourly: 0.05);

        var result = _adapter.BuildUsefulLoadSet(handoff);

        var auxiliaryLoad = Assert.Single(result.AuxiliaryLoads);
        Assert.Equal(SystemEnergyCarrier.Electricity, auxiliaryLoad.Carrier);
        Assert.Equal(0.05, auxiliaryLoad.HourlyAuxiliaryEnergyKWh8760[0], 6);
    }

    [Fact]
    public void AddsDhwHandoffDiagnostics()
    {
        var handoff = SystemEnergyTestData.CreateDhwHandoff();

        var result = _adapter.BuildUsefulLoadSet(handoff);

        Assert.Contains(
            result.UsefulLoads.SelectMany(load => load.Diagnostics),
            diagnostic => diagnostic.Code == "AE-SYS-DHW-HANDOFF-ADAPTED");
    }
}
