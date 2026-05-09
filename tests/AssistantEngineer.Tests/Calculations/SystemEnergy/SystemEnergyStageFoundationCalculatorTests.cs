using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class SystemEnergyStageFoundationCalculatorTests
{
    [Fact]
    public void EmissionEfficiencyStage_IsDeterministic()
    {
        var calculator = new SystemEnergyEmissionCalculator();
        var stage = CreateStage(SystemEnergySubsystemKind.Emission, efficiency: 0.8);
        var result = calculator.Calculate(new SystemEnergyStageCalculationRequest(
            CalculationId: "STAGE-EM-1",
            UseKind: SystemEnergyUseKind.SpaceHeating,
            InputProfileKWh: Enumerable.Repeat(8.0, 8760).ToArray(),
            StageDefinition: stage,
            TimeStepHours: 1.0,
            OwnershipPolicy: SystemEnergyLossOwnershipPolicy.SystemEnergyOwnsLosses));

        Assert.Equal(10.0, result.OutputProfileKWh[0], 6);
        Assert.Equal(2.0, result.LossesProfileKWh[0], 6);
    }

    [Fact]
    public void DistributionLossFractionStage_IsDeterministic()
    {
        var calculator = new SystemEnergyDistributionCalculator();
        var stage = CreateStage(SystemEnergySubsystemKind.Distribution, lossFraction: 0.1);
        var result = calculator.Calculate(new SystemEnergyStageCalculationRequest(
            CalculationId: "STAGE-DIST-1",
            UseKind: SystemEnergyUseKind.SpaceHeating,
            InputProfileKWh: Enumerable.Repeat(10.0, 8760).ToArray(),
            StageDefinition: stage,
            TimeStepHours: 1.0,
            OwnershipPolicy: SystemEnergyLossOwnershipPolicy.SystemEnergyOwnsLosses));

        Assert.Equal(11.0, result.OutputProfileKWh[0], 6);
        Assert.Equal(1.0, result.LossesProfileKWh[0], 6);
    }

    [Fact]
    public void StorageStage_SkipsDhwWhenUpstreamOwnsLosses()
    {
        var calculator = new SystemEnergyStorageCalculator();
        var stage = CreateStage(SystemEnergySubsystemKind.Storage, lossFraction: 0.2);
        var result = calculator.Calculate(new SystemEnergyStageCalculationRequest(
            CalculationId: "STAGE-STOR-1",
            UseKind: SystemEnergyUseKind.DomesticHotWater,
            InputProfileKWh: Enumerable.Repeat(5.0, 8760).ToArray(),
            StageDefinition: stage,
            TimeStepHours: 1.0,
            OwnershipPolicy: SystemEnergyLossOwnershipPolicy.UpstreamOwnsLosses));

        Assert.Equal(5.0, result.OutputProfileKWh[0], 6);
        Assert.Equal(0.0, result.LossesProfileKWh[0], 6);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-STORAGE-SKIPPED-UPSTREAM-OWNERSHIP");
    }

    [Fact]
    public void DistributionStage_SkipsDhwWhenNoDoubleCountingIsEnabled()
    {
        var calculator = new SystemEnergyDistributionCalculator();
        var stage = CreateStage(SystemEnergySubsystemKind.Distribution, lossFraction: 0.2);
        var result = calculator.Calculate(new SystemEnergyStageCalculationRequest(
            CalculationId: "STAGE-DIST-2",
            UseKind: SystemEnergyUseKind.DomesticHotWater,
            InputProfileKWh: Enumerable.Repeat(5.0, 8760).ToArray(),
            StageDefinition: stage,
            TimeStepHours: 1.0,
            OwnershipPolicy: SystemEnergyLossOwnershipPolicy.NoDoubleCounting));

        Assert.Equal(5.0, result.OutputProfileKWh[0], 6);
        Assert.Equal(0.0, result.LossesProfileKWh[0], 6);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-DISTRIBUTION-SKIPPED-UPSTREAM-OWNERSHIP");
    }

    [Fact]
    public void GenerationCalculator_UsesCopForHeatPump()
    {
        var calculator = new SystemEnergyGenerationCalculator();
        var result = calculator.Calculate(new SystemEnergyGenerationStageRequest(
            CalculationId: "GEN-1",
            UseKind: SystemEnergyUseKind.SpaceHeating,
            LoadToGenerationProfileKWh: Enumerable.Repeat(9.0, 8760).ToArray(),
            Generators:
            [
                new SystemEnergyGeneratorDefinition(
                    GeneratorId: "HP-1",
                    UseKind: SystemEnergyUseKind.SpaceHeating,
                    GeneratorKind: SystemEnergyGeneratorKind.HeatPump,
                    CarrierKind: SystemEnergyCarrierKind.Electricity,
                    Efficiency: null,
                    Cop: 3.0,
                    SeasonalPerformanceFactor: null,
                    RenewableContributionFraction: null,
                    AuxiliaryEnergyProfile: null)
            ],
            TimeStepHours: 1.0,
            OwnershipPolicy: SystemEnergyLossOwnershipPolicy.SystemEnergyOwnsLosses));

        Assert.Equal(9.0, result.DeliveredLoadProfileKWh[0], 6);
        Assert.Equal(3.0, result.FinalEnergyByCarrierKWh[SystemEnergyCarrierKind.Electricity][0], 6);
    }

    private static SystemEnergyStageDefinition CreateStage(
        SystemEnergySubsystemKind subsystemKind,
        double? efficiency = null,
        double? lossFraction = null) =>
        new(
            StageId: $"{subsystemKind}-S1",
            SubsystemKind: subsystemKind,
            AppliesToUse: SystemEnergyUseKind.SpaceHeating,
            Efficiency: efficiency,
            LossFraction: lossFraction,
            FixedLossProfile: null,
            AuxiliaryEnergyProfile: null,
            RecoveredLossFraction: 0.0,
            TargetCarrier: SystemEnergyCarrierKind.Unknown,
            CalculationMode: efficiency.HasValue
                ? SystemEnergyModuleCalculationMode.FixedEfficiency
                : SystemEnergyModuleCalculationMode.LossFraction,
            VerboseDiagnostics: true);
}
