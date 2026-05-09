using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class SystemEnergyFoundationCalculatorTests
{
    private readonly SystemEnergyFoundationCalculator _calculator = new(
        new SystemEnergyEmissionCalculator(),
        new SystemEnergyDistributionCalculator(),
        new SystemEnergyStorageCalculator(),
        new SystemEnergyGenerationCalculator());

    [Fact]
    public void MixedLoads_ProduceDeterministicFinalPrimaryAndCo2()
    {
        var loadInputs = new[]
        {
            CreateLoad("L-H", SystemEnergyEndUse.SpaceHeating, 6.0),
            CreateLoad("L-C", SystemEnergyEndUse.SpaceCooling, 3.0),
            CreateLoad("L-D", SystemEnergyEndUse.DomesticHotWater, 2.0)
        };

        var request = new SystemEnergyCalculationRequest(
            CalculationId: "FND-1",
            LoadInputs: loadInputs,
            StageDefinitions:
            [
                CreateStage(SystemEnergySubsystemKind.Emission, SystemEnergyUseKind.Generic, 1.0),
                CreateStage(SystemEnergySubsystemKind.Distribution, SystemEnergyUseKind.Generic, 1.0),
                CreateStage(SystemEnergySubsystemKind.Storage, SystemEnergyUseKind.Generic, 1.0)
            ],
            GeneratorDefinitions:
            [
                new SystemEnergyGeneratorDefinition("G-H", SystemEnergyUseKind.SpaceHeating, SystemEnergyGeneratorKind.GasBoiler, SystemEnergyCarrierKind.NaturalGas, 0.9, null, null, null, null, 0),
                new SystemEnergyGeneratorDefinition("G-C", SystemEnergyUseKind.SpaceCooling, SystemEnergyGeneratorKind.Chiller, SystemEnergyCarrierKind.Electricity, null, 3.0, null, null, null, 0),
                new SystemEnergyGeneratorDefinition("G-D", SystemEnergyUseKind.DomesticHotWater, SystemEnergyGeneratorKind.ElectricResistance, SystemEnergyCarrierKind.Electricity, 1.0, null, null, null, null, 0)
            ],
            FactorCatalog: CreateFactorCatalog(),
            TimeStepHours: 1.0,
            OutputResolution: SystemEnergyProfileShape.Hourly8760,
            OwnershipPolicy: SystemEnergyLossOwnershipPolicy.NoDoubleCounting,
            StrictFactorMode: true);

        var result = _calculator.Calculate(request);

        Assert.True(result.AnnualSummary.FinalEnergyKWh > 0);
        Assert.True(result.AnnualSummary.PrimaryEnergyKWh > 0);
        Assert.True(result.AnnualSummary.Co2Kg > 0);
        Assert.Equal(
            result.AnnualSummary.FinalEnergyKWh,
            result.FinalEnergyByCarrierKWh.Values.Sum(profile => profile.Sum()),
            6);
    }

    [Fact]
    public void MissingFactor_StrictModeEmitsErrorDiagnostic()
    {
        var request = new SystemEnergyCalculationRequest(
            CalculationId: "FND-2",
            LoadInputs: [CreateLoad("L-H", SystemEnergyEndUse.SpaceHeating, 1.0)],
            StageDefinitions:
            [
                CreateStage(SystemEnergySubsystemKind.Emission, SystemEnergyUseKind.Generic, 1.0),
                CreateStage(SystemEnergySubsystemKind.Distribution, SystemEnergyUseKind.Generic, 1.0),
                CreateStage(SystemEnergySubsystemKind.Storage, SystemEnergyUseKind.Generic, 1.0)
            ],
            GeneratorDefinitions:
            [
                new SystemEnergyGeneratorDefinition("G-H", SystemEnergyUseKind.SpaceHeating, SystemEnergyGeneratorKind.GasBoiler, SystemEnergyCarrierKind.NaturalGas, 1.0, null, null, null, null, 0)
            ],
            FactorCatalog: new EnergyFactorCatalog("CAT-EMPTY", "v1", []),
            TimeStepHours: 1.0,
            OutputResolution: SystemEnergyProfileShape.Hourly8760,
            OwnershipPolicy: SystemEnergyLossOwnershipPolicy.NoDoubleCounting,
            StrictFactorMode: true);

        var result = _calculator.Calculate(request);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-FACTOR-MISSING");
    }

    [Fact]
    public void NoDoubleCountingPolicy_UsesDhwUpstreamSystemLoadAndSkipsDhwDistributionStorageLosses()
    {
        var request = new SystemEnergyCalculationRequest(
            CalculationId: "FND-3",
            LoadInputs:
            [
                new SystemEnergyUsefulLoadInput(
                    LoadId: "L-DHW",
                    BuildingId: "B1",
                    ZoneId: "Z1",
                    RoomId: "R1",
                    EndUse: SystemEnergyEndUse.DomesticHotWater,
                    HourlyUsefulEnergyKWh8760: Enumerable.Repeat(1.0, 8760).ToArray(),
                    MonthlyUsefulEnergyKWh: null,
                    AnnualUsefulEnergyKWh: null,
                    Source: "UnitTest",
                    Diagnostics: [],
                    HourlySystemLoadKWh8760: Enumerable.Repeat(2.0, 8760).ToArray())
            ],
            StageDefinitions:
            [
                CreateStage(SystemEnergySubsystemKind.Emission, SystemEnergyUseKind.DomesticHotWater, 1.0),
                new SystemEnergyStageDefinition("DIST-D", SystemEnergySubsystemKind.Distribution, SystemEnergyUseKind.DomesticHotWater, null, 0.5, null, null, 0.0, SystemEnergyCarrierKind.Unknown, SystemEnergyModuleCalculationMode.LossFraction, false),
                new SystemEnergyStageDefinition("STOR-D", SystemEnergySubsystemKind.Storage, SystemEnergyUseKind.DomesticHotWater, null, 0.5, null, null, 0.0, SystemEnergyCarrierKind.Unknown, SystemEnergyModuleCalculationMode.LossFraction, false)
            ],
            GeneratorDefinitions:
            [
                new SystemEnergyGeneratorDefinition("G-D", SystemEnergyUseKind.DomesticHotWater, SystemEnergyGeneratorKind.ElectricResistance, SystemEnergyCarrierKind.Electricity, 1.0, null, null, null, null, 0)
            ],
            FactorCatalog: CreateFactorCatalog(),
            TimeStepHours: 1.0,
            OutputResolution: SystemEnergyProfileShape.Hourly8760,
            OwnershipPolicy: SystemEnergyLossOwnershipPolicy.NoDoubleCounting,
            StrictFactorMode: true);

        var result = _calculator.Calculate(request);

        Assert.Equal(2.0 * 8760.0, result.AnnualSummary.FinalEnergyKWh, 6);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-OWNERSHIP-UPSTREAM-SYSTEM-LOAD-USED");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-DISTRIBUTION-SKIPPED-UPSTREAM-OWNERSHIP");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-STORAGE-SKIPPED-UPSTREAM-OWNERSHIP");
    }

    [Fact]
    public void SystemEnergyOwnsLosses_AddsDistributionAndStorageLossesOnce()
    {
        var request = new SystemEnergyCalculationRequest(
            CalculationId: "FND-4",
            LoadInputs: [CreateLoad("L-D", SystemEnergyEndUse.DomesticHotWater, 1.0)],
            StageDefinitions:
            [
                CreateStage(SystemEnergySubsystemKind.Emission, SystemEnergyUseKind.DomesticHotWater, 1.0),
                new SystemEnergyStageDefinition("DIST-D", SystemEnergySubsystemKind.Distribution, SystemEnergyUseKind.DomesticHotWater, null, 0.2, null, null, 0.0, SystemEnergyCarrierKind.Unknown, SystemEnergyModuleCalculationMode.LossFraction, false),
                new SystemEnergyStageDefinition("STOR-D", SystemEnergySubsystemKind.Storage, SystemEnergyUseKind.DomesticHotWater, null, 0.1, null, null, 0.0, SystemEnergyCarrierKind.Unknown, SystemEnergyModuleCalculationMode.LossFraction, false)
            ],
            GeneratorDefinitions:
            [
                new SystemEnergyGeneratorDefinition("G-D", SystemEnergyUseKind.DomesticHotWater, SystemEnergyGeneratorKind.ElectricResistance, SystemEnergyCarrierKind.Electricity, 1.0, null, null, null, null, 0)
            ],
            FactorCatalog: CreateFactorCatalog(),
            TimeStepHours: 1.0,
            OutputResolution: SystemEnergyProfileShape.Hourly8760,
            OwnershipPolicy: SystemEnergyLossOwnershipPolicy.SystemEnergyOwnsLosses,
            StrictFactorMode: true);

        var result = _calculator.Calculate(request);
        var expectedHourly = 1.32; // 1.0 -> +20% distribution -> +10% storage
        Assert.Equal(expectedHourly * 8760.0, result.AnnualSummary.FinalEnergyKWh, 6);
    }

    private static SystemEnergyUsefulLoadInput CreateLoad(
        string id,
        SystemEnergyEndUse endUse,
        double hourlyValue) =>
        new(
            LoadId: id,
            BuildingId: "B1",
            ZoneId: "Z1",
            RoomId: "R1",
            EndUse: endUse,
            HourlyUsefulEnergyKWh8760: Enumerable.Repeat(hourlyValue, 8760).ToArray(),
            MonthlyUsefulEnergyKWh: null,
            AnnualUsefulEnergyKWh: null,
            Source: "UnitTest",
            Diagnostics: []);

    private static SystemEnergyStageDefinition CreateStage(
        SystemEnergySubsystemKind subsystem,
        SystemEnergyUseKind use,
        double efficiency) =>
        new(
            StageId: $"{subsystem}-{use}",
            SubsystemKind: subsystem,
            AppliesToUse: use,
            Efficiency: efficiency,
            LossFraction: null,
            FixedLossProfile: null,
            AuxiliaryEnergyProfile: null,
            RecoveredLossFraction: 0.0,
            TargetCarrier: SystemEnergyCarrierKind.Unknown,
            CalculationMode: SystemEnergyModuleCalculationMode.FixedEfficiency,
            VerboseDiagnostics: false);

    private static EnergyFactorCatalog CreateFactorCatalog() =>
        new(
            CatalogId: "CAT-1",
            Version: "v1",
            Entries:
            [
                new EnergyFactorCatalogEntry(SystemEnergyCarrierKind.Electricity, 1.8, 0.4, 2.2, 0.35, "UnitTest"),
                new EnergyFactorCatalogEntry(SystemEnergyCarrierKind.NaturalGas, 1.1, 0.0, 1.1, 0.21, "UnitTest")
            ]);
}
