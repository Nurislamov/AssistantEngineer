using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;

public sealed class Iso52016MatrixReducedRoomModelBuilder : IIso52016MatrixReducedRoomModelBuilder
{
    public Result<Iso52016MatrixHourlySolverRequest> Build(
        Iso52016MatrixReducedRoomModelRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016MatrixHourlySolverRequest>.Failure(validation);

        var hourlyInputProfile = request.HourlyInputProfile;
        var heatBalanceOptions = request.HeatBalanceOptions ?? new Iso52016RoomHeatBalanceOptions();
        var modelOptions = request.ModelOptions ?? new Iso52016MatrixReducedRoomModelOptions();

        var solverOptions = new Iso52016MatrixHourlySolverOptions(
            TimeStepSeconds: heatBalanceOptions.TimeStepSeconds,
            AirNodeId: modelOptions.AirNodeId,
            DefaultHeatingSetpointC: hourlyInputProfile.HeatingSetpointC,
            DefaultCoolingSetpointC: hourlyInputProfile.CoolingSetpointC);

        var nodes = new[]
        {
            new Iso52016MatrixNodeDefinition(
                NodeId: modelOptions.AirNodeId,
                HeatCapacityJPerK: hourlyInputProfile.ThermalCapacityJPerK,
                InitialTemperatureC: heatBalanceOptions.InitialIndoorTemperatureC,
                IsAirNode: true)
        };

        var boundaryConductances = new[]
        {
            new Iso52016MatrixBoundaryConductance(
                NodeId: modelOptions.AirNodeId,
                BoundaryId: modelOptions.OutdoorBoundaryId,
                ConductanceWPerK: hourlyInputProfile.TotalHeatTransferCoefficientWPerK)
        };

        var hours = hourlyInputProfile.Hours
            .Select(hour => new Iso52016MatrixHourlyInputRecord(
                HourOfYear: hour.HourOfYear,
                Month: hour.Month,
                Day: hour.Day,
                Hour: hour.Hour,
                BoundaryTemperaturesC: new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    [modelOptions.OutdoorBoundaryId] = hour.OutdoorTemperatureC
                },
                NodeHeatGainsW: new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    [modelOptions.AirNodeId] = hour.TotalGainsW
                },
                HeatingSetpointC: hour.HeatingSetpointC,
                CoolingSetpointC: hour.CoolingSetpointC))
            .ToArray();

        return Result<Iso52016MatrixHourlySolverRequest>.Success(
            new Iso52016MatrixHourlySolverRequest(
                ZoneCode: hourlyInputProfile.RoomCode.Trim(),
                Nodes: nodes,
                InternalConductances: Array.Empty<Iso52016MatrixConductanceLink>(),
                BoundaryConductances: boundaryConductances,
                Hours: hours,
                Options: solverOptions));
    }

    private static Result Validate(
        Iso52016MatrixReducedRoomModelRequest request)
    {
        if (request is null)
            return Result.Validation("ISO 52016 Matrix reduced room model request is required.");

        if (request.HourlyInputProfile is null)
            return Result.Validation("ISO 52016 room hourly input profile is required for V2 model mapping.");

        var hourlyInputProfile = request.HourlyInputProfile;

        if (string.IsNullOrWhiteSpace(hourlyInputProfile.RoomCode))
            return Result.Validation("ISO 52016 room code is required for V2 model mapping.");

        if (hourlyInputProfile.HourCount == 0)
            return Result.Validation("ISO 52016 room hourly input profile must contain hourly records for V2 model mapping.");

        if (hourlyInputProfile.TotalHeatTransferCoefficientWPerK <= 0)
            return Result.Validation("ISO 52016 Matrix reduced room model requires positive total heat transfer coefficient.");

        if (hourlyInputProfile.ThermalCapacityJPerK <= 0)
            return Result.Validation("ISO 52016 Matrix reduced room model requires positive thermal capacity.");

        if (hourlyInputProfile.CoolingSetpointC <= hourlyInputProfile.HeatingSetpointC)
            return Result.Validation("ISO 52016 Matrix reduced room model cooling setpoint must be greater than heating setpoint.");

        var heatBalanceOptions = request.HeatBalanceOptions ?? new Iso52016RoomHeatBalanceOptions();

        if (heatBalanceOptions.TimeStepSeconds <= 0)
            return Result.Validation("ISO 52016 Matrix reduced room model time step must be greater than zero.");

        var modelOptions = request.ModelOptions ?? new Iso52016MatrixReducedRoomModelOptions();

        if (string.IsNullOrWhiteSpace(modelOptions.AirNodeId))
            return Result.Validation("ISO 52016 Matrix reduced room model air node id is required.");

        if (string.IsNullOrWhiteSpace(modelOptions.OutdoorBoundaryId))
            return Result.Validation("ISO 52016 Matrix reduced room model outdoor boundary id is required.");

        foreach (var hour in hourlyInputProfile.Hours)
        {
            if (hour.TotalHeatTransferCoefficientWPerK <= 0)
                return Result.Validation($"ISO 52016 Matrix reduced room model requires positive total heat transfer coefficient at hour {hour.HourOfYear}.");

            if (hour.ThermalCapacityJPerK <= 0)
                return Result.Validation($"ISO 52016 Matrix reduced room model requires positive thermal capacity at hour {hour.HourOfYear}.");

            if (hour.CoolingSetpointC <= hour.HeatingSetpointC)
                return Result.Validation($"ISO 52016 Matrix reduced room model cooling setpoint must be greater than heating setpoint at hour {hour.HourOfYear}.");
        }

        return Result.Success();
    }
}