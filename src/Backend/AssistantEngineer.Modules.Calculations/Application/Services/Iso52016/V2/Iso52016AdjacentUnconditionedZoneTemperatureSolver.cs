using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.V2;

public sealed class Iso52016AdjacentUnconditionedZoneTemperatureSolver : IIso52016AdjacentUnconditionedZoneTemperatureSolver
{
    public Result<Iso52016AdjacentUnconditionedZoneTemperatureResult> Solve(
        Iso52016AdjacentUnconditionedZoneTemperatureRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016AdjacentUnconditionedZoneTemperatureResult>.Failure(validation);

        var capacityTermWPerK = request.ThermalCapacityJPerK / request.TimeStepSeconds;
        var totalConductance =
            request.HeatTransferToOutdoorWPerK +
            request.HeatTransferToGroundWPerK +
            request.HeatTransferToConditionedZoneWPerK;

        var totalGainsW = request.InternalGainsW + request.SolarGainsW;

        var numerator =
            capacityTermWPerK * request.AdjacentZonePreviousTemperatureC +
            request.HeatTransferToOutdoorWPerK * request.OutdoorTemperatureC +
            request.HeatTransferToGroundWPerK * request.GroundTemperatureC +
            request.HeatTransferToConditionedZoneWPerK * request.ConditionedZoneTemperatureC +
            totalGainsW;

        var denominator = capacityTermWPerK + totalConductance;
        var temperatureC = numerator / denominator;
        var heatFlowToConditionedZoneW =
            request.HeatTransferToConditionedZoneWPerK *
            (temperatureC - request.ConditionedZoneTemperatureC);

        return Result<Iso52016AdjacentUnconditionedZoneTemperatureResult>.Success(
            new Iso52016AdjacentUnconditionedZoneTemperatureResult(
                TemperatureC: temperatureC,
                HeatFlowToConditionedZoneW: heatFlowToConditionedZoneW,
                TotalBoundaryConductanceWPerK: totalConductance,
                TotalGainsW: totalGainsW));
    }

    private static Result Validate(
        Iso52016AdjacentUnconditionedZoneTemperatureRequest request)
    {
        if (request.TimeStepSeconds <= 0)
            return Result.Validation("Adjacent unconditioned zone time step must be greater than zero.");

        if (request.ThermalCapacityJPerK <= 0)
            return Result.Validation("Adjacent unconditioned zone thermal capacity must be greater than zero.");

        if (request.HeatTransferToOutdoorWPerK < 0 ||
            request.HeatTransferToGroundWPerK < 0 ||
            request.HeatTransferToConditionedZoneWPerK < 0)
        {
            return Result.Validation("Adjacent unconditioned zone heat transfer coefficients cannot be negative.");
        }

        var totalConductance =
            request.HeatTransferToOutdoorWPerK +
            request.HeatTransferToGroundWPerK +
            request.HeatTransferToConditionedZoneWPerK;

        if (totalConductance <= 0)
            return Result.Validation("Adjacent unconditioned zone requires at least one heat transfer path.");

        return Result.Success();
    }
}