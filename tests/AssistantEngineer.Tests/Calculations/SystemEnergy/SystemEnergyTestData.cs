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
        double usefulHourly = 1.0,
        double systemLoadHourly = 1.2,
        double auxiliaryHourly = 0.05,
        DomesticHotWaterLossOwnershipPolicy ownershipPolicy = DomesticHotWaterLossOwnershipPolicy.DhwOwnLosses) =>
        new(
            CalculationId: "DHW-H1",
            EndUse: "DomesticHotWater",
            UsefulEnergySource: "test",
            AnnualUsefulDhwEnergyKWh: HourlyConstant(usefulHourly).Sum(),
            AnnualDhwSystemHeatRequirementKWh: HourlyConstant(systemLoadHourly).Sum(),
            AnnualDhwAuxiliaryElectricityKWh: HourlyConstant(auxiliaryHourly).Sum(),
            HourlyUsefulDhwEnergyKWh8760: HourlyConstant(usefulHourly),
            HourlyDhwSystemHeatRequirementKWh8760: HourlyConstant(systemLoadHourly),
            HourlyDhwAuxiliaryElectricityKWh8760: HourlyConstant(auxiliaryHourly),
            HourlyRecoverableLossKWh8760: HourlyConstant(0.2),
            HourlyNonRecoverableLossKWh8760: HourlyConstant(0.3),
            Diagnostics: [],
            LossOwnershipPolicy: ownershipPolicy);

    public static SystemEnergyGenerationHandoff CreateGenerationHandoff(
        double heatingHourlyLoad = 10.0,
        SystemEnergyEndUse endUse = SystemEnergyEndUse.SpaceHeating) =>
        new(
            CalculationId: "GEN-H1",
            HourlySystemLoadBeforeGenerationByEndUseKWh8760: new Dictionary<SystemEnergyEndUse, IReadOnlyList<double>>
            {
                [endUse] = HourlyConstant(heatingHourlyLoad)
            },
            AnnualSystemLoadBeforeGenerationByEndUseKWh: new Dictionary<SystemEnergyEndUse, double>
            {
                [endUse] = HourlyConstant(heatingHourlyLoad).Sum()
            },
            HourlyRecoverableLossByEndUseKWh8760: new Dictionary<SystemEnergyEndUse, IReadOnlyList<double>>
            {
                [endUse] = HourlyConstant(0.0)
            },
            HourlyNonRecoverableLossByEndUseKWh8760: new Dictionary<SystemEnergyEndUse, IReadOnlyList<double>>
            {
                [endUse] = HourlyConstant(0.0)
            },
            AuxiliaryLoads: [],
            Diagnostics: []);

    public static SystemEnergyGeneratorInput CreateGenerator(
        string generatorId = "G1",
        SystemEnergyGeneratorKind kind = SystemEnergyGeneratorKind.Boiler,
        SystemEnergyGeneratorCalculationMode mode = SystemEnergyGeneratorCalculationMode.FixedEfficiency,
        SystemEnergyCarrier carrier = SystemEnergyCarrier.NaturalGas,
        double? efficiency = 0.9,
        double? cop = null,
        double? eer = null,
        double? spf = null,
        int priority = 0,
        double? loadFraction = null,
        double? capacity = null,
        IReadOnlyList<SystemEnergyEndUse>? servedEndUses = null) =>
        new(
            GeneratorId: generatorId,
            Name: generatorId,
            GeneratorKind: kind,
            CalculationMode: mode,
            ServiceMode: SystemEnergyGeneratorServiceMode.Heating,
            FinalEnergyCarrier: carrier,
            ServedEndUses: servedEndUses ?? [SystemEnergyEndUse.SpaceHeating],
            Priority: priority,
            LoadFraction: loadFraction,
            NominalCapacityKWhPerHour: capacity,
            Efficiency: efficiency,
            Cop: cop,
            Eer: eer,
            SeasonalPerformanceFactor: spf,
            AuxiliaryElectricityFraction: null,
            AuxiliaryElectricityKWhPerKWhOutput: null,
            HourlyLoadFraction8760: null,
            HourlyFinalEnergyProfileKWh8760: null,
            Source: "test",
            Diagnostics: []);

    public static SystemEnergyGeneratorSet CreateGeneratorSet(
        IReadOnlyList<SystemEnergyGeneratorInput> generators,
        SystemEnergyLoadSplitMode splitMode = SystemEnergyLoadSplitMode.SingleGenerator) =>
        new(
            GeneratorSetId: "SET-1",
            LoadSplitMode: splitMode,
            Generators: generators,
            DisclosureOverride: null,
            Source: "test",
            Diagnostics: []);

    public static SystemEnergyGeneratorCalculationInput CreateGeneratorCalculationInput(
        SystemEnergyGenerationHandoff? handoff = null,
        SystemEnergyGeneratorSet? generatorSet = null) =>
        new(
            CalculationId: "GEN-CALC-1",
            GenerationHandoff: handoff ?? CreateGenerationHandoff(),
            GeneratorSet: generatorSet ?? CreateGeneratorSet([CreateGenerator()]),
            DisclosureOverride: null,
            Source: "test");
}
