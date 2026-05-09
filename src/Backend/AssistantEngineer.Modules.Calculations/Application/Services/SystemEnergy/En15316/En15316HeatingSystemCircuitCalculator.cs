using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy.En15316;

public sealed class En15316HeatingSystemCircuitCalculator
{
    private readonly En15316HeatingSystemInputValidator _validator;
    private readonly En15316SystemEnergyReferenceDataProvider _referenceDataProvider;

    public En15316HeatingSystemCircuitCalculator(
        En15316HeatingSystemInputValidator validator,
        En15316SystemEnergyReferenceDataProvider referenceDataProvider)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _referenceDataProvider = referenceDataProvider ?? throw new ArgumentNullException(nameof(referenceDataProvider));
    }

    public Result<En15316HeatingSystemResult> Calculate(En15316HeatingSystemInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var validation = _validator.Validate(input);
        if (validation.IsFailure)
            return Result<En15316HeatingSystemResult>.Failure(validation);

        var assumptions = new List<string>
        {
            "EN15316-style system energy calculation foundation for heating circuits.",
            "Internal deterministic engineering anchors only.",
            "Not full validation and no full EN15316 compliance claim.",
            "Not full validation and no external validation claim.",
            "Simplified and C3 emission-model branches are explicitly implemented; other branches use deterministic fallback."
        };

        var globalDiagnostics = new List<En15316SystemEnergyDiagnostics>();
        var orderedCircuits = input.Circuits.OrderBy(circuit => circuit.CircuitId, StringComparer.Ordinal).ToArray();
        var stepResults = new List<HeatingSystemTimeStepResult>(input.TimeSteps.Count);

        foreach (var step in input.TimeSteps.OrderBy(step => step.TimeStepIndex))
        {
            var stepDiagnostics = new List<En15316SystemEnergyDiagnostics>();
            var circuitBreakdowns = new Dictionary<string, HeatingCircuitTimeStepEnergyBreakdown>(StringComparer.Ordinal);
            var finalByCircuit = new Dictionary<string, double>(StringComparer.Ordinal);
            var primaryByCircuit = new Dictionary<string, double>(StringComparer.Ordinal);
            var emissionLosses = 0.0;
            var distributionLosses = 0.0;
            var storageLosses = 0.0;
            var generatorLosses = 0.0;

            var loadFractions = ResolveLoadFractions(orderedCircuits, step);
            var usefulHeatingTotal = Math.Max(0, step.UsefulHeatingLoadKWh);
            var usefulDhwTotal = Math.Max(0, step.UsefulDhwLoadKWh);
            var usefulTotal = usefulHeatingTotal + usefulDhwTotal;
            var operatingCondition = ResolveOperatingCondition(input, step);

            foreach (var circuit in orderedCircuits)
            {
                var defaults = _referenceDataProvider.ResolveHeatingCircuitDefaults(
                    circuit.CircuitType,
                    circuit.Emission.ModelKind);

                var circuitFraction = loadFractions[circuit.CircuitId];
                var usefulHeating = usefulHeatingTotal * circuitFraction;
                var usefulDhw = usefulDhwTotal * circuitFraction;
                var useful = usefulHeating + usefulDhw;

                var emissionOutput = useful;
                var emissionInput = ApplyEmissionModel(emissionOutput, circuit, defaults, operatingCondition, stepDiagnostics);
                var emissionLoss = Math.Max(0, emissionInput - emissionOutput);

                var distributionOutput = emissionInput;
                var distributionInput = ApplyModule(
                    distributionOutput,
                    circuit.Distribution.Efficiency ?? defaults.Distribution.Efficiency,
                    circuit.Distribution.LossFactor ?? defaults.Distribution.LossFactor);
                var distributionLoss = Math.Max(0, distributionInput - distributionOutput);

                var storageOutput = distributionInput;
                var storageInput = ApplyStorage(
                    storageOutput,
                    circuit.Storage.Efficiency ?? defaults.Storage.Efficiency,
                    circuit.Storage.LossFactor ?? defaults.Storage.LossFactor);
                var storageLoss = Math.Max(0, storageInput - storageOutput);

                var generatorOutput = storageInput;
                var generationFinalEnergy = ApplyGeneration(generatorOutput, circuit.Generation);
                var generatorLoss = Math.Max(0, generationFinalEnergy - generatorOutput);
                var auxiliaryFraction = circuit.Distribution.AuxiliaryEnergyFraction > 0
                    ? circuit.Distribution.AuxiliaryEnergyFraction
                    : defaults.Distribution.AuxiliaryEnergyFraction;
                var auxiliary = generationFinalEnergy * auxiliaryFraction;
                var final = generationFinalEnergy + auxiliary;
                var primary = final * circuit.Generation.PrimaryEnergyFactor;
                var overallEfficiency = final > 0 ? useful / final : 0.0;

                emissionLosses += emissionLoss;
                distributionLosses += distributionLoss;
                storageLosses += storageLoss;
                generatorLosses += generatorLoss;

                circuitBreakdowns[circuit.CircuitId] = new HeatingCircuitTimeStepEnergyBreakdown(
                    CircuitId: circuit.CircuitId,
                    UsefulHeatingEnergyKWh: Round6(usefulHeating),
                    UsefulDhwEnergyKWh: Round6(usefulDhw),
                    UsefulTotalEnergyKWh: Round6(useful),
                    EmissionInputEnergyKWh: Round6(emissionInput),
                    EmissionOutputEnergyKWh: Round6(emissionOutput),
                    EmissionLossEnergyKWh: Round6(emissionLoss),
                    DistributionInputEnergyKWh: Round6(distributionInput),
                    DistributionOutputEnergyKWh: Round6(distributionOutput),
                    DistributionLossEnergyKWh: Round6(distributionLoss),
                    StorageInputEnergyKWh: Round6(storageInput),
                    StorageOutputEnergyKWh: Round6(storageOutput),
                    StorageLossEnergyKWh: Round6(storageLoss),
                    GeneratorInputEnergyKWh: Round6(generatorOutput),
                    GeneratorOutputEnergyKWh: Round6(generationFinalEnergy),
                    GeneratorLossEnergyKWh: Round6(generatorLoss),
                    FinalEnergyKWh: Round6(final),
                    PrimaryEnergyKWh: Round6(primary),
                    OverallUsefulToFinalEfficiency: Round6(overallEfficiency));

                finalByCircuit[circuit.CircuitId] = Round6(final);
                primaryByCircuit[circuit.CircuitId] = Round6(primary);
            }

            var totalFinal = Round6(finalByCircuit.Values.Sum());
            var totalPrimary = Round6(primaryByCircuit.Values.Sum());
            var overallStepEfficiency = totalFinal > 0 ? usefulTotal / totalFinal : 0.0;

            var stepResult = new HeatingSystemTimeStepResult(
                TimeStepIndex: step.TimeStepIndex,
                Month: step.Month,
                UsefulHeatingLoadKWh: Round6(usefulHeatingTotal),
                UsefulDhwLoadKWh: Round6(usefulDhwTotal),
                UsefulTotalLoadKWh: Round6(usefulTotal),
                CircuitBreakdowns: circuitBreakdowns,
                EmissionLossEnergyKWh: Round6(emissionLosses),
                DistributionLossEnergyKWh: Round6(distributionLosses),
                StorageLossEnergyKWh: Round6(storageLosses),
                GeneratorLossEnergyKWh: Round6(generatorLosses),
                FinalEnergyByCircuitKWh: finalByCircuit,
                PrimaryEnergyByCircuitKWh: primaryByCircuit,
                TotalFinalEnergyKWh: totalFinal,
                TotalPrimaryEnergyKWh: totalPrimary,
                OverallUsefulToFinalEfficiency: Round6(overallStepEfficiency),
                Diagnostics: stepDiagnostics);

            stepResults.Add(stepResult);
            globalDiagnostics.AddRange(stepDiagnostics);
        }

        var monthlyUsefulHeating = AggregateMonthly(stepResults, step => step.UsefulHeatingLoadKWh);
        var monthlyUsefulDhw = AggregateMonthly(stepResults, step => step.UsefulDhwLoadKWh);
        var monthlyUsefulTotal = AggregateMonthly(stepResults, step => step.UsefulTotalLoadKWh);
        var monthlyEmissionLoss = AggregateMonthly(stepResults, step => step.EmissionLossEnergyKWh);
        var monthlyDistributionLoss = AggregateMonthly(stepResults, step => step.DistributionLossEnergyKWh);
        var monthlyStorageLoss = AggregateMonthly(stepResults, step => step.StorageLossEnergyKWh);
        var monthlyGeneratorLoss = AggregateMonthly(stepResults, step => step.GeneratorLossEnergyKWh);
        var monthlyFinal = AggregateMonthly(stepResults, step => step.TotalFinalEnergyKWh);
        var monthlyPrimary = AggregateMonthly(stepResults, step => step.TotalPrimaryEnergyKWh);

        var annualUsefulHeating = Round6(stepResults.Sum(step => step.UsefulHeatingLoadKWh));
        var annualUsefulDhw = Round6(stepResults.Sum(step => step.UsefulDhwLoadKWh));
        var annualUsefulTotal = Round6(stepResults.Sum(step => step.UsefulTotalLoadKWh));
        var annualEmissionLoss = Round6(stepResults.Sum(step => step.EmissionLossEnergyKWh));
        var annualDistributionLoss = Round6(stepResults.Sum(step => step.DistributionLossEnergyKWh));
        var annualStorageLoss = Round6(stepResults.Sum(step => step.StorageLossEnergyKWh));
        var annualGeneratorLoss = Round6(stepResults.Sum(step => step.GeneratorLossEnergyKWh));
        var annualFinal = Round6(stepResults.Sum(step => step.TotalFinalEnergyKWh));
        var annualPrimary = Round6(stepResults.Sum(step => step.TotalPrimaryEnergyKWh));
        var annualOverallEfficiency = annualFinal > 0 ? annualUsefulTotal / annualFinal : 0.0;

        return Result<En15316HeatingSystemResult>.Success(
            new En15316HeatingSystemResult(
                CalculationId: input.CalculationId,
                TimeSteps: stepResults,
                MonthlyUsefulHeatingEnergyKWh: monthlyUsefulHeating,
                MonthlyUsefulDhwEnergyKWh: monthlyUsefulDhw,
                MonthlyUsefulTotalEnergyKWh: monthlyUsefulTotal,
                MonthlyEmissionLossEnergyKWh: monthlyEmissionLoss,
                MonthlyDistributionLossEnergyKWh: monthlyDistributionLoss,
                MonthlyStorageLossEnergyKWh: monthlyStorageLoss,
                MonthlyGeneratorLossEnergyKWh: monthlyGeneratorLoss,
                MonthlyFinalEnergyKWh: monthlyFinal,
                MonthlyPrimaryEnergyKWh: monthlyPrimary,
                AnnualUsefulHeatingEnergyKWh: annualUsefulHeating,
                AnnualUsefulDhwEnergyKWh: annualUsefulDhw,
                AnnualUsefulEnergyKWh: annualUsefulTotal,
                AnnualEmissionLossEnergyKWh: annualEmissionLoss,
                AnnualDistributionLossEnergyKWh: annualDistributionLoss,
                AnnualStorageLossEnergyKWh: annualStorageLoss,
                AnnualGeneratorLossEnergyKWh: annualGeneratorLoss,
                AnnualFinalEnergyKWh: annualFinal,
                AnnualPrimaryEnergyKWh: annualPrimary,
                AnnualOverallUsefulToFinalEfficiency: Round6(annualOverallEfficiency),
                Diagnostics: globalDiagnostics,
                AssumptionsUsed: assumptions));
    }

    private static HeatingOperatingCondition ResolveOperatingCondition(
        En15316HeatingSystemInput input,
        HeatingSystemTimeStepInput step)
    {
        if (!string.IsNullOrWhiteSpace(step.OperatingConditionId))
        {
            var direct = input.OperatingConditions.FirstOrDefault(
                condition => string.Equals(condition.ConditionId, step.OperatingConditionId, StringComparison.Ordinal));
            if (direct is not null)
            {
                return step.OutdoorTemperatureC.HasValue
                    ? direct with { OutdoorTemperatureC = step.OutdoorTemperatureC }
                    : direct;
            }
        }

        if (input.OperatingConditions.Count > 0)
        {
            var first = input.OperatingConditions[0];
            return step.OutdoorTemperatureC.HasValue
                ? first with { OutdoorTemperatureC = step.OutdoorTemperatureC }
                : first;
        }

        return new HeatingOperatingCondition(
            ConditionId: "default",
            FlowReturnTemperatureC: new FlowReturnTemperaturePair(55, 45),
            OutdoorTemperatureC: step.OutdoorTemperatureC);
    }

    private static IReadOnlyDictionary<string, double> ResolveLoadFractions(
        IReadOnlyList<HeatingCircuit> circuits,
        HeatingSystemTimeStepInput step)
    {
        if (step.CircuitLoadFractions is null || step.CircuitLoadFractions.Count == 0)
        {
            var evenFraction = 1.0 / circuits.Count;
            return circuits.ToDictionary(circuit => circuit.CircuitId, _ => evenFraction, StringComparer.Ordinal);
        }

        var fractions = circuits.ToDictionary(
            circuit => circuit.CircuitId,
            circuit => step.CircuitLoadFractions.TryGetValue(circuit.CircuitId, out var value) ? Math.Max(0, value) : 0.0,
            StringComparer.Ordinal);

        var sum = fractions.Values.Sum();
        if (sum <= 0)
        {
            var evenFraction = 1.0 / circuits.Count;
            return circuits.ToDictionary(circuit => circuit.CircuitId, _ => evenFraction, StringComparer.Ordinal);
        }

        return fractions.ToDictionary(item => item.Key, item => item.Value / sum, StringComparer.Ordinal);
    }

    private static double ApplyEmissionModel(
        double downstream,
        HeatingCircuit circuit,
        En15316HeatingCircuitDefaults defaults,
        HeatingOperatingCondition operatingCondition,
        ICollection<En15316SystemEnergyDiagnostics> diagnostics)
    {
        var efficiency = circuit.Emission.Efficiency ?? defaults.Emission.Efficiency;
        var lossFactor = circuit.Emission.LossFactor ?? defaults.Emission.LossFactor;
        var effectiveFlowReturn = ResolveEffectiveFlowReturn(circuit, operatingCondition);
        var temperatureFactor = ResolveTemperatureFactor(circuit.DesignFlowReturnTemperatureC, effectiveFlowReturn);
        var adjustedEfficiency = temperatureFactor > 0 ? efficiency * temperatureFactor : efficiency;

        return circuit.Emission.ModelKind switch
        {
            En15316EmissionModelKind.Simplified => ApplyModule(downstream, adjustedEfficiency, lossFactor),
            En15316EmissionModelKind.C3 => ApplyModule(downstream, adjustedEfficiency * 0.99, lossFactor),
            _ => ApplyWithFallback(downstream, circuit, adjustedEfficiency, lossFactor, diagnostics)
        };
    }

    private static double ApplyWithFallback(
        double downstream,
        HeatingCircuit circuit,
        double efficiency,
        double? lossFactor,
        ICollection<En15316SystemEnergyDiagnostics> diagnostics)
    {
        diagnostics.Add(new En15316SystemEnergyDiagnostics(
            "En15316.HeatingCircuit.EmissionFallback",
            $"Circuit '{circuit.CircuitId}' used deterministic fallback for emission model '{circuit.Emission.ModelKind}'."));
        return ApplyModule(downstream, efficiency, lossFactor);
    }

    private static double ApplyGeneration(
        double upstream,
        GenerationSystemModel generation)
    {
        switch (generation.Technology)
        {
            case En15316GenerationTechnology.HeatPump:
                if (generation.Cop is > 0)
                    return upstream / generation.Cop.Value;
                break;
            case En15316GenerationTechnology.ElectricResistance:
            case En15316GenerationTechnology.DirectElectric:
                if (generation.Efficiency is > 0)
                    return upstream / generation.Efficiency.Value;
                return upstream;
            default:
                if (generation.Efficiency is > 0)
                    return upstream / generation.Efficiency.Value;

                if (generation.Cop is > 0)
                    return upstream / generation.Cop.Value;
                break;
        }

        return upstream;
    }

    private static double ApplyStorage(
        double downstream,
        double efficiency,
        double? lossFactor)
    {
        if (efficiency > 0)
            return downstream / efficiency;

        if (lossFactor is >= 0)
            return downstream * (1.0 + lossFactor.Value);

        return downstream;
    }

    private static double ApplyModule(
        double downstream,
        double efficiency,
        double? lossFactor)
    {
        if (efficiency > 0)
            return downstream / efficiency;

        if (lossFactor is >= 0)
            return downstream * (1.0 + lossFactor.Value);

        return downstream;
    }

    private static FlowReturnTemperaturePair ResolveEffectiveFlowReturn(
        HeatingCircuit circuit,
        HeatingOperatingCondition operatingCondition)
    {
        var flow = operatingCondition.FlowReturnTemperatureC.FlowTemperatureC;
        var ret = operatingCondition.FlowReturnTemperatureC.ReturnTemperatureC;

        if (operatingCondition.OutdoorResetSlopePerK.HasValue &&
            operatingCondition.OutdoorResetReferenceTemperatureC.HasValue &&
            operatingCondition.OutdoorTemperatureC.HasValue)
        {
            var deltaOutdoor = operatingCondition.OutdoorResetReferenceTemperatureC.Value - operatingCondition.OutdoorTemperatureC.Value;
            flow += operatingCondition.OutdoorResetSlopePerK.Value * deltaOutdoor;

            if (flow <= ret)
                flow = ret + 1.0;
        }

        if (flow <= ret)
            flow = circuit.DesignFlowReturnTemperatureC.FlowTemperatureC;

        return new FlowReturnTemperaturePair(flow, ret);
    }

    private static double ResolveTemperatureFactor(
        FlowReturnTemperaturePair design,
        FlowReturnTemperaturePair effective)
    {
        var designDelta = Math.Max(1.0, design.FlowTemperatureC - design.ReturnTemperatureC);
        var effectiveDelta = Math.Max(1.0, effective.FlowTemperatureC - effective.ReturnTemperatureC);
        var ratio = effectiveDelta / designDelta;
        return Math.Clamp(ratio, 0.85, 1.15);
    }

    private static IReadOnlyDictionary<int, double> AggregateMonthly(
        IEnumerable<HeatingSystemTimeStepResult> steps,
        Func<HeatingSystemTimeStepResult, double> selector)
    {
        return steps
            .GroupBy(step => step.Month)
            .OrderBy(group => group.Key)
            .ToDictionary(group => group.Key, group => Round6(group.Sum(selector)));
    }

    private static double Round6(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);
}
