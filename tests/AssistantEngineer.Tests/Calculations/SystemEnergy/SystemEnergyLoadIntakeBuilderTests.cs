using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class SystemEnergyLoadIntakeBuilderTests
{
    private readonly SystemEnergyLoadIntakeBuilder _builder = new();

    [Fact]
    public void Intake_MapsHeatingCoolingAndDhwSeparately()
    {
        var result = _builder.Build(new SystemEnergyLoadIntakeRequest(
            CalculationId: "INTAKE-1",
            HeatingUsefulProfileKWh: Enumerable.Repeat(1.0, 8760).ToArray(),
            CoolingUsefulProfileKWh: Enumerable.Repeat(2.0, 8760).ToArray(),
            DhwHandoff: SystemEnergyTestData.CreateDhwHandoff(),
            AuxiliaryElectricityProfileKWh: Enumerable.Repeat(0.1, 8760).ToArray(),
            TimeStepHours: 1.0,
            NormalizeSignedLoads: true));

        Assert.Contains(result.UsefulLoadSet.UsefulLoads, load => load.EndUse == SystemEnergyEndUse.SpaceHeating);
        Assert.Contains(result.UsefulLoadSet.UsefulLoads, load => load.EndUse == SystemEnergyEndUse.SpaceCooling);
        Assert.Contains(result.UsefulLoadSet.UsefulLoads, load => load.EndUse == SystemEnergyEndUse.DomesticHotWater);
        Assert.Single(result.UsefulLoadSet.AuxiliaryLoads);
    }

    [Fact]
    public void Intake_NormalizesSignedLoadsWhenEnabled()
    {
        var result = _builder.Build(new SystemEnergyLoadIntakeRequest(
            CalculationId: "INTAKE-2",
            HeatingUsefulProfileKWh: Enumerable.Repeat(-1.0, 8760).ToArray(),
            CoolingUsefulProfileKWh: null,
            DhwHandoff: null,
            AuxiliaryElectricityProfileKWh: null,
            TimeStepHours: 1.0,
            NormalizeSignedLoads: true));

        var heating = Assert.Single(result.UsefulLoadSet.UsefulLoads);
        Assert.Equal(1.0, heating.HourlyUsefulEnergyKWh8760[0], 6);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-INTAKE-SIGNED-NORMALIZED");
    }

    [Fact]
    public void Intake_RejectsNegativeWhenNormalizationDisabled()
    {
        var result = _builder.Build(new SystemEnergyLoadIntakeRequest(
            CalculationId: "INTAKE-3",
            HeatingUsefulProfileKWh: Enumerable.Repeat(-1.0, 8760).ToArray(),
            CoolingUsefulProfileKWh: null,
            DhwHandoff: null,
            AuxiliaryElectricityProfileKWh: null,
            TimeStepHours: 1.0,
            NormalizeSignedLoads: false));

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-INTAKE-NEGATIVE-ENERGY");
    }
}
