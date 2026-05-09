using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy.En15316;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy.En15316;

public sealed class En15316HeatingSystemCircuitFoundationTests
{
    private readonly En15316HeatingSystemInputValidator _validator = new();
    private readonly En15316SystemEnergyReferenceDataProvider _referenceDataProvider = new();

    [Fact]
    public void ValidSimplifiedCircuit_PassesValidationAndCalculates()
    {
        var input = BuildInput(
            emissionModel: En15316EmissionModelKind.Simplified,
            generationEfficiency: 0.9);

        var validation = _validator.Validate(input);
        Assert.True(validation.IsSuccess, validation.Error);

        var calculator = new En15316HeatingSystemCircuitCalculator(_validator, _referenceDataProvider);
        var result = calculator.Calculate(input);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Single(result.Value.TimeSteps);
        Assert.True(result.Value.AnnualFinalEnergyKWh > 0);
        Assert.True(result.Value.AnnualPrimaryEnergyKWh > 0);
    }

    [Fact]
    public void ZeroUsefulDemand_GivesZeroFinalEnergy()
    {
        var input = BuildInput([BuildCircuit(En15316EmissionModelKind.Simplified, 0.9)], usefulHeating: 0, usefulDhw: 0);
        var calculator = new En15316HeatingSystemCircuitCalculator(_validator, _referenceDataProvider);

        var result = calculator.Calculate(input);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(0, result.Value.AnnualFinalEnergyKWh, 6);
        Assert.Equal(0, result.Value.AnnualPrimaryEnergyKWh, 6);
    }

    [Fact]
    public void BoilerEfficiencyCalculation_IsApplied()
    {
        var circuit = BuildCircuit(En15316EmissionModelKind.Simplified, 0.8);
        var input = BuildInput([circuit], usefulHeating: 80, usefulDhw: 0);
        var calculator = new En15316HeatingSystemCircuitCalculator(_validator, _referenceDataProvider);

        var result = calculator.Calculate(input);

        Assert.True(result.IsSuccess, result.Error);
        Assert.True(result.Value.AnnualFinalEnergyKWh > result.Value.AnnualUsefulEnergyKWh);
    }

    [Fact]
    public void HeatPumpCopCalculation_IsApplied()
    {
        var circuit = BuildCircuit(En15316EmissionModelKind.Simplified, generationEfficiency: 0.95) with
        {
            Generation = new GenerationSystemModel(
                Technology: En15316GenerationTechnology.HeatPump,
                Carrier: En15316EnergyCarrier.Electricity,
                Cop: 3.0,
                PrimaryEnergyFactor: 2.0)
        };
        var input = BuildInput([circuit], usefulHeating: 120, usefulDhw: 0);
        var calculator = new En15316HeatingSystemCircuitCalculator(_validator, _referenceDataProvider);

        var result = calculator.Calculate(input);

        Assert.True(result.IsSuccess, result.Error);
        Assert.True(result.Value.AnnualFinalEnergyKWh < result.Value.AnnualUsefulEnergyKWh);
    }

    [Fact]
    public void DistributionLoss_IncreasesFinalEnergy()
    {
        var baseCircuit = BuildCircuit(En15316EmissionModelKind.Simplified, 0.9) with
        {
            Distribution = new DistributionCircuitModel(
                Efficiency: 1.0,
                AuxiliaryEnergyFraction: 0.0)
        };
        var lossyCircuit = baseCircuit with
        {
            Distribution = new DistributionCircuitModel(
                Efficiency: 0.85,
                AuxiliaryEnergyFraction: 0.0)
        };

        var calculator = new En15316HeatingSystemCircuitCalculator(_validator, _referenceDataProvider);
        var baseResult = calculator.Calculate(BuildInput([baseCircuit], usefulHeating: 100, usefulDhw: 0));
        var lossyResult = calculator.Calculate(BuildInput([lossyCircuit], usefulHeating: 100, usefulDhw: 0));

        Assert.True(baseResult.IsSuccess, baseResult.Error);
        Assert.True(lossyResult.IsSuccess, lossyResult.Error);
        Assert.True(lossyResult.Value.AnnualFinalEnergyKWh > baseResult.Value.AnnualFinalEnergyKWh);
        Assert.True(lossyResult.Value.AnnualDistributionLossEnergyKWh > baseResult.Value.AnnualDistributionLossEnergyKWh);
    }

    [Fact]
    public void PrimaryEnergyFactor_IsApplied()
    {
        var circuit = BuildCircuit(En15316EmissionModelKind.Simplified, 0.9) with
        {
            Generation = new GenerationSystemModel(
                Technology: En15316GenerationTechnology.Boiler,
                Carrier: En15316EnergyCarrier.NaturalGas,
                Efficiency: 0.9,
                PrimaryEnergyFactor: 1.4)
        };
        var input = BuildInput([circuit], usefulHeating: 120, usefulDhw: 0);
        var calculator = new En15316HeatingSystemCircuitCalculator(_validator, _referenceDataProvider);

        var result = calculator.Calculate(input);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(result.Value.AnnualFinalEnergyKWh * 1.4, result.Value.AnnualPrimaryEnergyKWh, 4);
    }

    [Fact]
    public void MonthlyAndAnnualSums_MatchTimestepSums()
    {
        var circuit = BuildCircuit(En15316EmissionModelKind.Simplified, 0.9);
        var input = new En15316HeatingSystemInput(
            CalculationId: "monthly-annual-consistency",
            Circuits: [circuit],
            OperatingConditions:
            [
                new HeatingOperatingCondition(
                    ConditionId: "winter",
                    FlowReturnTemperatureC: new FlowReturnTemperaturePair(55, 45),
                    OutdoorTemperatureC: -5,
                    OutdoorResetSlopePerK: 0.2,
                    OutdoorResetReferenceTemperatureC: 15)
            ],
            TimeSteps:
            [
                new HeatingSystemTimeStepInput(TimeStepIndex: 0, Month: 1, UsefulHeatingLoadKWh: 80, UsefulDhwLoadKWh: 20, OutdoorTemperatureC: -5, OperatingConditionId: "winter"),
                new HeatingSystemTimeStepInput(TimeStepIndex: 1, Month: 1, UsefulHeatingLoadKWh: 40, UsefulDhwLoadKWh: 10, OutdoorTemperatureC: 0, OperatingConditionId: "winter"),
                new HeatingSystemTimeStepInput(TimeStepIndex: 2, Month: 2, UsefulHeatingLoadKWh: 30, UsefulDhwLoadKWh: 15, OutdoorTemperatureC: 5, OperatingConditionId: "winter")
            ]);

        var calculator = new En15316HeatingSystemCircuitCalculator(_validator, _referenceDataProvider);
        var result = calculator.Calculate(input);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(result.Value.AnnualFinalEnergyKWh, result.Value.TimeSteps.Sum(step => step.TotalFinalEnergyKWh), 6);
        Assert.Equal(result.Value.AnnualPrimaryEnergyKWh, result.Value.TimeSteps.Sum(step => step.TotalPrimaryEnergyKWh), 6);
        Assert.Equal(result.Value.AnnualFinalEnergyKWh, result.Value.MonthlyFinalEnergyKWh.Sum(item => item.Value), 6);
        Assert.Equal(result.Value.AnnualPrimaryEnergyKWh, result.Value.MonthlyPrimaryEnergyKWh.Sum(item => item.Value), 6);
    }

    [Fact]
    public void InvalidGenerationEfficiency_IsRejected()
    {
        var input = BuildInput(
            emissionModel: En15316EmissionModelKind.Simplified,
            generationEfficiency: 1.2);

        var validation = _validator.Validate(input);

        Assert.True(validation.IsFailure);
        Assert.Contains("generation efficiency", validation.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InvalidFlowReturnPair_IsRejected()
    {
        var circuit = BuildCircuit(
            emissionModel: En15316EmissionModelKind.Simplified,
            generationEfficiency: 0.9) with
        {
            DesignFlowReturnTemperatureC = new FlowReturnTemperaturePair(FlowTemperatureC: 45, ReturnTemperatureC: 50)
        };

        var input = BuildInput([circuit]);
        var validation = _validator.Validate(input);

        Assert.True(validation.IsFailure);
        Assert.Contains("flow/return", validation.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void C3EmissionModel_IsSupported()
    {
        var input = BuildInput(
            emissionModel: En15316EmissionModelKind.C3,
            generationEfficiency: 0.9);

        var calculator = new En15316HeatingSystemCircuitCalculator(_validator, _referenceDataProvider);
        var result = calculator.Calculate(input);

        Assert.True(result.IsSuccess, result.Error);
        Assert.DoesNotContain(
            result.Value.Diagnostics,
            diagnostic => diagnostic.Code.Contains("EmissionFallback", StringComparison.Ordinal));
    }

    [Fact]
    public void ExistingEn15316ChainBehavior_RemainsUnchanged()
    {
        var chain = new En15316SystemEnergyChainCalculator(_referenceDataProvider);
        var input = new En15316SystemEnergyInput(
            EndUses:
            [
                new En15316SystemEnergyEndUseInput(
                    EndUse: En15316EndUse.Heating,
                    EnergyCarrier: En15316EnergyCarrier.NaturalGas,
                    GenerationTechnology: En15316GenerationTechnology.Boiler,
                    UsefulEnergyKWh: 10000,
                    Emission: new En15316SystemEnergyModuleInput(Efficiency: 0.95),
                    Distribution: new En15316SystemEnergyModuleInput(Efficiency: 0.9),
                    Storage: new En15316SystemEnergyModuleInput(LossFactor: 0.1),
                    GenerationEfficiency: 0.88,
                    AuxiliaryEnergyKWh: 200,
                    PrimaryEnergyFactor: 1.1)
            ],
            DiagnosticsContext: "en15316-boiler");

        var result = chain.Calculate(input);
        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(14819.88304, result.Value.TotalFinalEnergyKWh, precision: 5);
        Assert.Equal(16301.871344, result.Value.TotalPrimaryEnergyKWh, precision: 5);
    }

    [Fact]
    public void ReferenceDefaults_AreDeterministicForCircuitFoundation()
    {
        var defaultsA = _referenceDataProvider.ResolveHeatingCircuitDefaults(
            HeatingCircuitType.Radiator,
            En15316EmissionModelKind.C3);
        var defaultsB = _referenceDataProvider.ResolveHeatingCircuitDefaults(
            HeatingCircuitType.Radiator,
            En15316EmissionModelKind.C3);

        Assert.Equal(defaultsA, defaultsB);
        Assert.True(defaultsA.Emission.Efficiency > 0);
        Assert.True(defaultsA.Distribution.Efficiency > 0);
        Assert.True(defaultsA.Storage.Efficiency > 0);
    }

    [Fact]
    public void InvalidCircuitIds_DuplicateIdsAreRejected()
    {
        var circuitA = BuildCircuit(En15316EmissionModelKind.Simplified, 0.9) with
        {
            CircuitId = "circuit-a"
        };
        var circuitB = BuildCircuit(En15316EmissionModelKind.C3, 0.9) with
        {
            CircuitId = "circuit-a"
        };

        var input = BuildInput([circuitA, circuitB]);
        var validation = _validator.Validate(input);

        Assert.True(validation.IsFailure);
        Assert.Contains("Duplicate circuit id", validation.Error, StringComparison.OrdinalIgnoreCase);
    }

    private static En15316HeatingSystemInput BuildInput(
        En15316EmissionModelKind emissionModel,
        double generationEfficiency) =>
        BuildInput([BuildCircuit(emissionModel, generationEfficiency)], usefulHeating: 100, usefulDhw: 0);

    private static En15316HeatingSystemInput BuildInput(
        IReadOnlyList<HeatingCircuit> circuits,
        double usefulHeating = 100,
        double usefulDhw = 0) =>
        new(
            CalculationId: "heating-circuit-foundation",
            Circuits: circuits,
            OperatingConditions:
            [
                new HeatingOperatingCondition(
                    ConditionId: "winter-design",
                    FlowReturnTemperatureC: new FlowReturnTemperaturePair(55, 45),
                    OutdoorTemperatureC: -5,
                    OutdoorResetSlopePerK: 0.2,
                    OutdoorResetReferenceTemperatureC: 15)
            ],
            TimeSteps:
            [
                new HeatingSystemTimeStepInput(
                    TimeStepIndex: 0,
                    Month: 1,
                    UsefulHeatingLoadKWh: usefulHeating,
                    UsefulDhwLoadKWh: usefulDhw,
                    OutdoorTemperatureC: -5,
                    OperatingConditionId: "winter-design")
            ],
            DiagnosticsContext: "en15316-circuit-foundation");

    private static HeatingCircuit BuildCircuit(
        En15316EmissionModelKind emissionModel,
        double generationEfficiency) =>
        new(
            CircuitId: "circuit-1",
            CircuitType: HeatingCircuitType.Radiator,
            Emission: new EmissionSystemModel(
                ModelKind: emissionModel,
                Efficiency: 0.96),
            Distribution: new DistributionCircuitModel(
                Efficiency: 0.93,
                AuxiliaryEnergyFraction: 0.02),
            Generation: new GenerationSystemModel(
                Technology: En15316GenerationTechnology.Boiler,
                Carrier: En15316EnergyCarrier.NaturalGas,
                Efficiency: generationEfficiency,
                PrimaryEnergyFactor: 1.1),
            Storage: new StorageSystemModel(
                Efficiency: 0.99),
            DesignFlowReturnTemperatureC: new FlowReturnTemperaturePair(55, 45));
}
