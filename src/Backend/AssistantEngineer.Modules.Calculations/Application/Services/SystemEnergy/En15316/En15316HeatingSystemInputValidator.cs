using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy.En15316;

public sealed class En15316HeatingSystemInputValidator
{
    public Result Validate(En15316HeatingSystemInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(input.CalculationId))
            return Result.Validation("CalculationId is required.");

        if (input.Circuits is null || input.Circuits.Count == 0)
            return Result.Validation("At least one heating circuit is required.");

        var duplicateCircuitId = input.Circuits
            .GroupBy(circuit => circuit.CircuitId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)?
            .Key;
        if (!string.IsNullOrWhiteSpace(duplicateCircuitId))
            return Result.Validation($"Duplicate circuit id was found: '{duplicateCircuitId}'.");

        foreach (var circuit in input.Circuits)
        {
            if (string.IsNullOrWhiteSpace(circuit.CircuitId))
                return Result.Validation("Heating circuit id is required.");

            if (circuit.CircuitType == HeatingCircuitType.Unknown)
                return Result.Validation($"Heating circuit '{circuit.CircuitId}' has unknown circuit type.");

            if (circuit.DesignFlowReturnTemperatureC.FlowTemperatureC <= circuit.DesignFlowReturnTemperatureC.ReturnTemperatureC)
            {
                return Result.Validation(
                    $"Heating circuit '{circuit.CircuitId}' has invalid design flow/return temperatures.");
            }

            if (circuit.Distribution.AuxiliaryEnergyFraction is < 0 or > 1)
            {
                return Result.Validation(
                    $"Heating circuit '{circuit.CircuitId}' has invalid distribution auxiliary energy fraction.");
            }

            if (circuit.Generation.PrimaryEnergyFactor < 0 || !double.IsFinite(circuit.Generation.PrimaryEnergyFactor))
            {
                return Result.Validation(
                    $"Heating circuit '{circuit.CircuitId}' has invalid primary energy factor.");
            }

            if (circuit.Generation.Efficiency is null && circuit.Generation.Cop is null)
            {
                return Result.Validation(
                    $"Heating circuit '{circuit.CircuitId}' requires generation efficiency or COP.");
            }

            var emissionValidation = ValidateEfficiencyAndLoss(
                circuit.Emission.Efficiency,
                circuit.Emission.LossFactor,
                $"Heating circuit '{circuit.CircuitId}' emission");
            if (emissionValidation.IsFailure)
                return emissionValidation;

            var distributionValidation = ValidateEfficiencyAndLoss(
                circuit.Distribution.Efficiency,
                circuit.Distribution.LossFactor,
                $"Heating circuit '{circuit.CircuitId}' distribution");
            if (distributionValidation.IsFailure)
                return distributionValidation;

            var storageValidation = ValidateEfficiencyAndLoss(
                circuit.Storage.Efficiency,
                circuit.Storage.LossFactor,
                $"Heating circuit '{circuit.CircuitId}' storage");
            if (storageValidation.IsFailure)
                return storageValidation;

            if (circuit.Generation.Efficiency is <= 0 or > 1)
                return Result.Validation($"Heating circuit '{circuit.CircuitId}' generation efficiency must be within (0, 1].");

            if (circuit.Generation.Cop is <= 0)
                return Result.Validation($"Heating circuit '{circuit.CircuitId}' generation COP must be greater than zero.");
        }

        var validCircuitIds = input.Circuits.Select(circuit => circuit.CircuitId).ToHashSet(StringComparer.Ordinal);
        var validOperatingConditionIds = input.OperatingConditions
            .Select(condition => condition.ConditionId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var condition in input.OperatingConditions)
        {
            if (string.IsNullOrWhiteSpace(condition.ConditionId))
                return Result.Validation("Operating condition id is required.");

            if (condition.FlowReturnTemperatureC.FlowTemperatureC <= condition.FlowReturnTemperatureC.ReturnTemperatureC)
                return Result.Validation($"Operating condition '{condition.ConditionId}' has invalid flow/return temperatures.");

            if (condition.OutdoorResetSlopePerK.HasValue && !double.IsFinite(condition.OutdoorResetSlopePerK.Value))
                return Result.Validation($"Operating condition '{condition.ConditionId}' has invalid outdoor reset slope.");

            if (condition.OutdoorResetReferenceTemperatureC.HasValue && !double.IsFinite(condition.OutdoorResetReferenceTemperatureC.Value))
                return Result.Validation($"Operating condition '{condition.ConditionId}' has invalid outdoor reset reference temperature.");
        }

        foreach (var step in input.TimeSteps)
        {
            if (step.TimeStepIndex < 0)
                return Result.Validation("TimeStepIndex must be non-negative.");

            if (step.Month is < 1 or > 12)
                return Result.Validation($"TimeStep '{step.TimeStepIndex}' has invalid month value '{step.Month}'.");

            if (step.UsefulHeatingLoadKWh < 0 || !double.IsFinite(step.UsefulHeatingLoadKWh))
                return Result.Validation($"TimeStep '{step.TimeStepIndex}' useful heating load must be finite and non-negative.");

            if (step.UsefulDhwLoadKWh < 0 || !double.IsFinite(step.UsefulDhwLoadKWh))
                return Result.Validation($"TimeStep '{step.TimeStepIndex}' useful DHW load must be finite and non-negative.");

            if (step.OutdoorTemperatureC.HasValue && !double.IsFinite(step.OutdoorTemperatureC.Value))
                return Result.Validation($"TimeStep '{step.TimeStepIndex}' has invalid outdoor temperature.");

            if (!string.IsNullOrWhiteSpace(step.OperatingConditionId) &&
                !validOperatingConditionIds.Contains(step.OperatingConditionId))
            {
                return Result.Validation(
                    $"TimeStep '{step.TimeStepIndex}' references missing operating condition '{step.OperatingConditionId}'.");
            }

            if (step.CircuitLoadFractions is null)
                continue;

            foreach (var item in step.CircuitLoadFractions)
            {
                if (!validCircuitIds.Contains(item.Key))
                    return Result.Validation($"TimeStep '{step.TimeStepIndex}' references unknown circuit id '{item.Key}'.");

                if (!double.IsFinite(item.Value) || item.Value < 0)
                    return Result.Validation($"TimeStep '{step.TimeStepIndex}' has invalid circuit load fraction for '{item.Key}'.");
            }
        }

        return Result.Success();
    }

    private static Result ValidateEfficiencyAndLoss(
        double? efficiency,
        double? lossFactor,
        string name)
    {
        if (efficiency is <= 0 or > 1)
            return Result.Validation($"{name} efficiency must be within (0, 1].");

        if (lossFactor < 0 || (lossFactor.HasValue && !double.IsFinite(lossFactor.Value)))
            return Result.Validation($"{name} loss factor must be finite and non-negative.");

        return Result.Success();
    }
}
