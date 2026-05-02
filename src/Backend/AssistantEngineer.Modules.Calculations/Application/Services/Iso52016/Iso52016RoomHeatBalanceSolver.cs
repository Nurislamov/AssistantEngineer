using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016RoomHeatBalanceSolver : IIso52016RoomHeatBalanceSolver
{
    private const double MinimumHeatTransferCoefficientWPerK = 0.000001;

    public Result<Iso52016RoomHeatBalanceProfile> Solve(
        Iso52016RoomHeatBalanceRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016RoomHeatBalanceProfile>.Failure(validation);

        var options = request.Options ?? new Iso52016RoomHeatBalanceOptions();

        var hourlyResults = SolveHours(
            request.InputProfile,
            options);

        var monthlySummaries = BuildMonthlySummaries(
            hourlyResults);

        return Result<Iso52016RoomHeatBalanceProfile>.Success(
            new Iso52016RoomHeatBalanceProfile(
                RoomCode: request.InputProfile.RoomCode,
                HeatingSetpointC: request.InputProfile.HeatingSetpointC,
                CoolingSetpointC: request.InputProfile.CoolingSetpointC,
                Options: options,
                Hours: hourlyResults,
                MonthlySummaries: monthlySummaries));
    }

    private static IReadOnlyList<Iso52016HourlyRoomHeatBalanceResult> SolveHours(
        Iso52016RoomHourlyInputProfile inputProfile,
        Iso52016RoomHeatBalanceOptions options)
    {
        var results = new List<Iso52016HourlyRoomHeatBalanceResult>(
            inputProfile.HourCount);

        var previousIndoorTemperature = options.InitialIndoorTemperatureC;

        foreach (var input in inputProfile.Hours)
        {
            var result = SolveHour(
                input,
                previousIndoorTemperature,
                options.TimeStepSeconds);

            results.Add(result);

            previousIndoorTemperature = result.IndoorTemperatureAfterHvacC;
        }

        return results;
    }

    private static Iso52016HourlyRoomHeatBalanceResult SolveHour(
        Iso52016RoomHourlyInputRecord input,
        double previousIndoorTemperatureC,
        double timeStepSeconds)
    {
        var freeFloatingTemperature = CalculateFreeFloatingTemperature(
            previousIndoorTemperatureC,
            input.OutdoorTemperatureC,
            input.TotalGainsW,
            input.TotalHeatTransferCoefficientWPerK,
            input.ThermalCapacityJPerK,
            timeStepSeconds);

        if (freeFloatingTemperature < input.HeatingSetpointC)
        {
            var heatingLoad = CalculateAverageHvacLoadToReachTarget(
                targetIndoorTemperatureC: input.HeatingSetpointC,
                previousIndoorTemperatureC: previousIndoorTemperatureC,
                outdoorTemperatureC: input.OutdoorTemperatureC,
                gainsW: input.TotalGainsW,
                heatTransferCoefficientWPerK: input.TotalHeatTransferCoefficientWPerK,
                thermalCapacityJPerK: input.ThermalCapacityJPerK,
                timeStepSeconds: timeStepSeconds);

            heatingLoad = Math.Max(
                0.0,
                heatingLoad);

            return CreateResult(
                input,
                indoorTemperatureBeforeHvacC: freeFloatingTemperature,
                indoorTemperatureAfterHvacC: input.HeatingSetpointC,
                heatingLoadW: heatingLoad,
                coolingLoadW: 0.0,
                timeStepSeconds: timeStepSeconds);
        }

        if (freeFloatingTemperature > input.CoolingSetpointC)
        {
            var hvacLoad = CalculateAverageHvacLoadToReachTarget(
                targetIndoorTemperatureC: input.CoolingSetpointC,
                previousIndoorTemperatureC: previousIndoorTemperatureC,
                outdoorTemperatureC: input.OutdoorTemperatureC,
                gainsW: input.TotalGainsW,
                heatTransferCoefficientWPerK: input.TotalHeatTransferCoefficientWPerK,
                thermalCapacityJPerK: input.ThermalCapacityJPerK,
                timeStepSeconds: timeStepSeconds);

            var coolingLoad = Math.Max(
                0.0,
                -hvacLoad);

            return CreateResult(
                input,
                indoorTemperatureBeforeHvacC: freeFloatingTemperature,
                indoorTemperatureAfterHvacC: input.CoolingSetpointC,
                heatingLoadW: 0.0,
                coolingLoadW: coolingLoad,
                timeStepSeconds: timeStepSeconds);
        }

        return CreateResult(
            input,
            indoorTemperatureBeforeHvacC: freeFloatingTemperature,
            indoorTemperatureAfterHvacC: freeFloatingTemperature,
            heatingLoadW: 0.0,
            coolingLoadW: 0.0,
            timeStepSeconds: timeStepSeconds);
    }

    private static double CalculateFreeFloatingTemperature(
        double previousIndoorTemperatureC,
        double outdoorTemperatureC,
        double gainsW,
        double heatTransferCoefficientWPerK,
        double thermalCapacityJPerK,
        double timeStepSeconds)
    {
        var alpha = CalculateThermalDecayFactor(
            heatTransferCoefficientWPerK,
            thermalCapacityJPerK,
            timeStepSeconds);

        var equilibriumTemperature =
            outdoorTemperatureC +
            gainsW / heatTransferCoefficientWPerK;

        return
            equilibriumTemperature +
            (previousIndoorTemperatureC - equilibriumTemperature) *
            alpha;
    }

    private static double CalculateAverageHvacLoadToReachTarget(
        double targetIndoorTemperatureC,
        double previousIndoorTemperatureC,
        double outdoorTemperatureC,
        double gainsW,
        double heatTransferCoefficientWPerK,
        double thermalCapacityJPerK,
        double timeStepSeconds)
    {
        var alpha = CalculateThermalDecayFactor(
            heatTransferCoefficientWPerK,
            thermalCapacityJPerK,
            timeStepSeconds);

        var denominator = 1.0 - alpha;

        if (denominator <= 0)
            return 0.0;

        return
            heatTransferCoefficientWPerK *
            (
                (targetIndoorTemperatureC - alpha * previousIndoorTemperatureC) /
                denominator -
                outdoorTemperatureC
            ) -
            gainsW;
    }

    private static double CalculateThermalDecayFactor(
        double heatTransferCoefficientWPerK,
        double thermalCapacityJPerK,
        double timeStepSeconds) =>
        Math.Exp(
            -heatTransferCoefficientWPerK *
            timeStepSeconds /
            thermalCapacityJPerK);

    private static Iso52016HourlyRoomHeatBalanceResult CreateResult(
        Iso52016RoomHourlyInputRecord input,
        double indoorTemperatureBeforeHvacC,
        double indoorTemperatureAfterHvacC,
        double heatingLoadW,
        double coolingLoadW,
        double timeStepSeconds)
    {
        var timeStepHours =
            timeStepSeconds / 3600.0;

        return new Iso52016HourlyRoomHeatBalanceResult(
            HourOfYear: input.HourOfYear,
            Month: input.Month,
            Day: input.Day,
            Hour: input.Hour,
            OutdoorTemperatureC: input.OutdoorTemperatureC,
            IndoorTemperatureBeforeHvacC: indoorTemperatureBeforeHvacC,
            IndoorTemperatureAfterHvacC: indoorTemperatureAfterHvacC,
            HeatingSetpointC: input.HeatingSetpointC,
            CoolingSetpointC: input.CoolingSetpointC,
            SolarGainsW: input.SolarGainsW,
            InternalGainsW: input.InternalGainsW,
            TotalGainsW: input.TotalGainsW,
            TransmissionHeatTransferCoefficientWPerK: input.TransmissionHeatTransferCoefficientWPerK,
            VentilationHeatTransferCoefficientWPerK: input.VentilationHeatTransferCoefficientWPerK,
            TotalHeatTransferCoefficientWPerK: input.TotalHeatTransferCoefficientWPerK,
            ThermalCapacityJPerK: input.ThermalCapacityJPerK,
            HeatingLoadW: heatingLoadW,
            CoolingLoadW: coolingLoadW,
            HeatingEnergyKWh: heatingLoadW * timeStepHours / 1000.0,
            CoolingEnergyKWh: coolingLoadW * timeStepHours / 1000.0);
    }

    private static IReadOnlyList<Iso52016MonthlyRoomHeatBalanceSummary> BuildMonthlySummaries(
        IReadOnlyList<Iso52016HourlyRoomHeatBalanceResult> hours) =>
        hours
            .GroupBy(hour => hour.Month)
            .OrderBy(group => group.Key)
            .Select(group => new Iso52016MonthlyRoomHeatBalanceSummary(
                Month: group.Key,
                HeatingEnergyKWh: group.Sum(hour => hour.HeatingEnergyKWh),
                CoolingEnergyKWh: group.Sum(hour => hour.CoolingEnergyKWh),
                SolarGainsKWh: group.Sum(hour => hour.SolarGainsW) / 1000.0,
                InternalGainsKWh: group.Sum(hour => hour.InternalGainsW) / 1000.0,
                TotalGainsKWh: group.Sum(hour => hour.TotalGainsW) / 1000.0,
                PeakHeatingLoadW: group.Max(hour => hour.HeatingLoadW),
                PeakCoolingLoadW: group.Max(hour => hour.CoolingLoadW),
                AverageIndoorTemperatureC: group.Average(hour => hour.IndoorTemperatureAfterHvacC),
                AverageOutdoorTemperatureC: group.Average(hour => hour.OutdoorTemperatureC)))
            .ToArray();

    private static Result Validate(
        Iso52016RoomHeatBalanceRequest request)
    {
        if (request.InputProfile is null)
            return Result.Validation("Room hourly input profile is required.");

        if (request.InputProfile.HourCount == 0)
            return Result.Validation("Room hourly input profile must contain hourly records.");

        foreach (var hour in request.InputProfile.Hours)
        {
            if (hour.TotalHeatTransferCoefficientWPerK <= MinimumHeatTransferCoefficientWPerK)
            {
                return Result.Validation(
                    $"Total heat transfer coefficient at hour {hour.HourOfYear} must be greater than zero.");
            }

            if (hour.ThermalCapacityJPerK <= 0)
            {
                return Result.Validation(
                    $"Thermal capacity at hour {hour.HourOfYear} must be greater than zero.");
            }

            if (hour.CoolingSetpointC <= hour.HeatingSetpointC)
            {
                return Result.Validation(
                    $"Cooling setpoint at hour {hour.HourOfYear} must be greater than heating setpoint.");
            }
        }

        var options = request.Options ?? new Iso52016RoomHeatBalanceOptions();

        if (options.TimeStepSeconds <= 0)
            return Result.Validation("Heat balance time step must be greater than zero.");

        return Result.Success();
    }
}