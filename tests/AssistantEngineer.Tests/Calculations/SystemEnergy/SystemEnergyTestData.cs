using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

internal static class SystemEnergyTestData
{
    public static IReadOnlyList<double> HourlyConstant(double value) =>
        Enumerable.Repeat(value, 8760).ToArray();

    public static SystemEnergyUsefulLoadInput CreateUsefulLoad(
        string loadId = "L1",
        SystemEnergyEndUse endUse = SystemEnergyEndUse.SpaceHeating,
        double hourlyValue = 1.0) =>
        new(
            LoadId: loadId,
            BuildingId: "B1",
            ZoneId: "Z1",
            RoomId: "R1",
            EndUse: endUse,
            HourlyUsefulEnergyKWh8760: HourlyConstant(hourlyValue),
            MonthlyUsefulEnergyKWh: null,
            AnnualUsefulEnergyKWh: null,
            Source: "test",
            Diagnostics: []);

    public static SystemEnergyUsefulLoadSet CreateUsefulLoadSet(
        IReadOnlyList<SystemEnergyUsefulLoadInput>? usefulLoads = null,
        IReadOnlyList<SystemEnergyAuxiliaryLoadInput>? auxiliaryLoads = null) =>
        new(
            CalculationId: "SYS-1",
            UsefulLoads: usefulLoads ?? [CreateUsefulLoad()],
            AuxiliaryLoads: auxiliaryLoads ?? [],
            DisclosureOverride: null,
            Source: "test");

    public static DomesticHotWaterEn15316Handoff CreateDhwHandoff(
        double systemLoadHourly = 1.2,
        double auxiliaryHourly = 0.05) =>
        new(
            CalculationId: "DHW-H1",
            EndUse: "DomesticHotWater",
            UsefulEnergySource: "test",
            AnnualUsefulDhwEnergyKWh: HourlyConstant(1.0).Sum(),
            AnnualDhwSystemHeatRequirementKWh: HourlyConstant(systemLoadHourly).Sum(),
            AnnualDhwAuxiliaryElectricityKWh: HourlyConstant(auxiliaryHourly).Sum(),
            HourlyUsefulDhwEnergyKWh8760: HourlyConstant(1.0),
            HourlyDhwSystemHeatRequirementKWh8760: HourlyConstant(systemLoadHourly),
            HourlyDhwAuxiliaryElectricityKWh8760: HourlyConstant(auxiliaryHourly),
            HourlyRecoverableLossKWh8760: HourlyConstant(0.2),
            HourlyNonRecoverableLossKWh8760: HourlyConstant(0.3),
            Diagnostics: []);
}
