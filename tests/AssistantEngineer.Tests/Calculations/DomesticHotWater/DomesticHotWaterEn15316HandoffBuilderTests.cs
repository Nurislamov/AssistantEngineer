using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater;

public sealed class DomesticHotWaterEn15316HandoffBuilderTests
{
    private readonly DomesticHotWaterEn15316HandoffBuilder _builder = new();

    [Fact]
    public void HandoffPreservesHourlyProfiles()
    {
        var result = CreateSystemResult();

        var handoff = _builder.Build(result);

        Assert.Equal(8760, handoff.HourlyUsefulDhwEnergyKWh8760.Count);
        Assert.Equal(8760, handoff.HourlyDhwSystemHeatRequirementKWh8760.Count);
        Assert.Equal(8760, handoff.HourlyDhwAuxiliaryElectricityKWh8760.Count);
    }

    [Fact]
    public void HandoffIsClearlyMarkedAsHandoffOnly()
    {
        var result = CreateSystemResult();

        var handoff = _builder.Build(result);

        Assert.Contains(handoff.Diagnostics, diagnostic => diagnostic.Code == "AE-DHW-EN15316-HANDOFF-ONLY");
    }

    private static DomesticHotWaterSystemLoadResult CreateSystemResult()
    {
        var useful = DomesticHotWaterSystemLossTestData.CreateUsefulDemand();
        var zeros = new double[8760];

        return new DomesticHotWaterSystemLoadResult(
            CalculationId: "DHW-SYS-1",
            BuildingId: useful.BuildingId,
            ZoneId: useful.ZoneId,
            RoomId: useful.RoomId,
            UsefulDemand: useful,
            LossComponents: [],
            AnnualUsefulEnergyKWh: useful.AnnualUsefulEnergyKWh,
            AnnualStorageLossKWh: 0.0,
            AnnualDistributionLossKWh: 0.0,
            AnnualCirculationLossKWh: 0.0,
            AnnualAuxiliaryElectricityKWh: 0.0,
            AnnualRecoverableLossKWh: 0.0,
            AnnualNonRecoverableLossKWh: 0.0,
            AnnualSystemHeatRequirementKWh: useful.AnnualUsefulEnergyKWh,
            MonthlySystemHeatRequirementKWh: useful.MonthlyUsefulEnergyKWh,
            HourlySystemHeatRequirementKWh8760: useful.HourlyUsefulEnergyKWh8760,
            HourlyRecoverableLossKWh8760: zeros,
            HourlyNonRecoverableLossKWh8760: zeros,
            HourlyAuxiliaryElectricityKWh8760: zeros,
            En15316Handoff: new DomesticHotWaterEn15316Handoff(
                CalculationId: "placeholder",
                EndUse: "DomesticHotWater",
                UsefulEnergySource: "placeholder",
                AnnualUsefulDhwEnergyKWh: 0.0,
                AnnualDhwSystemHeatRequirementKWh: 0.0,
                AnnualDhwAuxiliaryElectricityKWh: 0.0,
                HourlyUsefulDhwEnergyKWh8760: zeros,
                HourlyDhwSystemHeatRequirementKWh8760: zeros,
                HourlyDhwAuxiliaryElectricityKWh8760: zeros,
                HourlyRecoverableLossKWh8760: zeros,
                HourlyNonRecoverableLossKWh8760: zeros,
                Diagnostics: []),
            Disclosure: new StandardCalculationDisclosureFactory().CreateDomesticHotWaterIso12831Disclosure(),
            Diagnostics: []);
    }
}
