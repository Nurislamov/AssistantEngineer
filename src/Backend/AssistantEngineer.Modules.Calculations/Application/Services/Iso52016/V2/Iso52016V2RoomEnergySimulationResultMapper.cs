using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.V2;

public sealed class Iso52016V2RoomEnergySimulationResultMapper : IIso52016V2RoomEnergySimulationResultMapper
{
    public Result<Iso52016RoomEnergySimulationResult> Map(
        Iso52016V2RoomEnergySimulationResult source)
    {
        var validation = Validate(source);

        if (validation.IsFailure)
            return Result<Iso52016RoomEnergySimulationResult>.Failure(validation);

        var hourlyResults = source.MatrixSolverProfile.Hours
            .OrderBy(hour => hour.HourOfYear)
            .Select(hour =>
            {
                var input = source.HourlyInputProfile.GetHour(hour.HourOfYear);

                return new Iso52016HourlyRoomHeatBalanceResult(
                    HourOfYear: hour.HourOfYear,
                    Month: hour.Month,
                    Day: hour.Day,
                    Hour: hour.Hour,
                    OutdoorTemperatureC: input.OutdoorTemperatureC,
                    IndoorTemperatureBeforeHvacC: hour.AirTemperatureBeforeHvacC,
                    IndoorTemperatureAfterHvacC: hour.AirTemperatureAfterHvacC,
                    HeatingSetpointC: hour.HeatingSetpointC,
                    CoolingSetpointC: hour.CoolingSetpointC,
                    SolarGainsW: input.SolarGainsW,
                    InternalGainsW: input.InternalGainsW,
                    TotalGainsW: input.TotalGainsW,
                    TransmissionHeatTransferCoefficientWPerK: input.TransmissionHeatTransferCoefficientWPerK,
                    VentilationHeatTransferCoefficientWPerK: input.VentilationHeatTransferCoefficientWPerK,
                    TotalHeatTransferCoefficientWPerK: input.TotalHeatTransferCoefficientWPerK,
                    ThermalCapacityJPerK: input.ThermalCapacityJPerK,
                    HeatingLoadW: hour.HeatingLoadW,
                    CoolingLoadW: hour.CoolingLoadW,
                    HeatingEnergyKWh: hour.HeatingEnergyKWh,
                    CoolingEnergyKWh: hour.CoolingEnergyKWh);
            })
            .ToArray();

        var heatBalanceOptions = BuildHeatBalanceOptions(source);

        var heatBalanceProfile = new Iso52016RoomHeatBalanceProfile(
            RoomCode: source.RoomCode,
            HeatingSetpointC: source.HourlyInputProfile.HeatingSetpointC,
            CoolingSetpointC: source.HourlyInputProfile.CoolingSetpointC,
            Options: heatBalanceOptions,
            Hours: hourlyResults,
            MonthlySummaries: BuildMonthlySummaries(hourlyResults));

        return Result<Iso52016RoomEnergySimulationResult>.Success(
            new Iso52016RoomEnergySimulationResult(
                RoomCode: source.RoomCode,
                SolarGainProfile: source.SolarGainProfile,
                InternalGainProfile: source.InternalGainProfile,
                HourlyInputProfile: source.HourlyInputProfile,
                HeatBalanceProfile: heatBalanceProfile));
    }

    private static Iso52016RoomHeatBalanceOptions BuildHeatBalanceOptions(
        Iso52016V2RoomEnergySimulationResult source)
    {
        var airNode = source.MatrixSolverRequest.Nodes.FirstOrDefault(node =>
            string.Equals(
                node.NodeId,
                source.MatrixSolverProfile.Options.AirNodeId,
                StringComparison.OrdinalIgnoreCase));

        return new Iso52016RoomHeatBalanceOptions(
            InitialIndoorTemperatureC: airNode?.InitialTemperatureC ?? 20.0,
            TimeStepSeconds: source.MatrixSolverProfile.Options.TimeStepSeconds);
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
        Iso52016V2RoomEnergySimulationResult source)
    {
        if (source is null)
            return Result.Validation("ISO 52016 V2 room energy simulation result is required for mapping.");

        if (string.IsNullOrWhiteSpace(source.RoomCode))
            return Result.Validation("ISO 52016 V2 room code is required for mapping.");

        if (source.SolarGainProfile is null)
            return Result.Validation("ISO 52016 V2 solar gain profile is required for mapping.");

        if (source.InternalGainProfile is null)
            return Result.Validation("ISO 52016 V2 internal gain profile is required for mapping.");

        if (source.HourlyInputProfile is null)
            return Result.Validation("ISO 52016 V2 hourly input profile is required for mapping.");

        if (source.MatrixSolverRequest is null)
            return Result.Validation("ISO 52016 V2 matrix solver request is required for mapping.");

        if (source.MatrixSolverProfile is null)
            return Result.Validation("ISO 52016 V2 matrix solver profile is required for mapping.");

        if (source.HourlyInputProfile.HourCount != source.MatrixSolverProfile.HourCount)
        {
            return Result.Validation(
                "ISO 52016 V2 hourly input profile and matrix solver profile must have the same hour count for mapping.");
        }

        foreach (var hour in source.MatrixSolverProfile.Hours)
        {
            if (hour.HourOfYear < 0 || hour.HourOfYear >= source.HourlyInputProfile.HourCount)
            {
                return Result.Validation(
                    $"ISO 52016 V2 matrix solver hour {hour.HourOfYear} is outside the hourly input profile range.");
            }
        }

        return Result.Success();
    }
}