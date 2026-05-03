using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;

public sealed class Iso52016MatrixRoomEnergySimulationService : IIso52016MatrixRoomEnergySimulationService
{
    private readonly IIso52016RoomSolarGainProfileBuilder _solarGainProfileBuilder;
    private readonly IIso52016RoomInternalGainProfileBuilder _internalGainProfileBuilder;
    private readonly IIso52016RoomHourlyInputProfileBuilder _hourlyInputProfileBuilder;
    private readonly IIso52016MatrixReducedRoomModelBuilder _reducedRoomModelBuilder;
    private readonly IIso52016MatrixHourlySolver _hourlySolver;

    public Iso52016MatrixRoomEnergySimulationService(
        IIso52016RoomSolarGainProfileBuilder solarGainProfileBuilder,
        IIso52016RoomInternalGainProfileBuilder internalGainProfileBuilder,
        IIso52016RoomHourlyInputProfileBuilder hourlyInputProfileBuilder,
        IIso52016MatrixReducedRoomModelBuilder reducedRoomModelBuilder,
        IIso52016MatrixHourlySolver hourlySolver)
    {
        _solarGainProfileBuilder = solarGainProfileBuilder;
        _internalGainProfileBuilder = internalGainProfileBuilder;
        _hourlyInputProfileBuilder = hourlyInputProfileBuilder;
        _reducedRoomModelBuilder = reducedRoomModelBuilder;
        _hourlySolver = hourlySolver;
    }

    public Result<Iso52016MatrixRoomEnergySimulationResult> Simulate(
        Iso52016RoomEnergySimulationRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016MatrixRoomEnergySimulationResult>.Failure(validation);

        var solarGainResult = _solarGainProfileBuilder.Build(
            new Iso52016RoomSolarGainProfileRequest(
                RoomCode: request.RoomCode,
                WeatherSolarContext: request.WeatherSolarContext,
                Windows: request.Windows));

        if (solarGainResult.IsFailure)
            return Result<Iso52016MatrixRoomEnergySimulationResult>.Failure(solarGainResult);

        var internalGainResult = _internalGainProfileBuilder.Build(
            new Iso52016RoomInternalGainProfileRequest(
                RoomCode: request.RoomCode,
                HourCount: request.WeatherSolarContext.HourCount,
                PeopleCount: request.PeopleCount,
                SensibleHeatGainPerPersonW: request.SensibleHeatGainPerPersonW,
                EquipmentLoadW: request.EquipmentLoadW,
                LightingLoadW: request.LightingLoadW,
                OccupancyFactors: request.OccupancyFactors,
                EquipmentFactors: request.EquipmentFactors,
                LightingFactors: request.LightingFactors));

        if (internalGainResult.IsFailure)
            return Result<Iso52016MatrixRoomEnergySimulationResult>.Failure(internalGainResult);

        var hourlyInputResult = _hourlyInputProfileBuilder.Build(
            new Iso52016RoomHourlyInputProfileRequest(
                RoomCode: request.RoomCode,
                WeatherSolarContext: request.WeatherSolarContext,
                SolarGainProfile: solarGainResult.Value,
                InternalGainProfile: internalGainResult.Value,
                TransmissionHeatTransferCoefficientWPerK: request.TransmissionHeatTransferCoefficientWPerK,
                VentilationHeatTransferCoefficientWPerK: request.VentilationHeatTransferCoefficientWPerK,
                ThermalCapacityJPerK: request.ThermalCapacityJPerK,
                HeatingSetpointC: request.HeatingSetpointC,
                CoolingSetpointC: request.CoolingSetpointC));

        if (hourlyInputResult.IsFailure)
            return Result<Iso52016MatrixRoomEnergySimulationResult>.Failure(hourlyInputResult);

        var matrixRequestResult = _reducedRoomModelBuilder.Build(
            new Iso52016MatrixReducedRoomModelRequest(
                HourlyInputProfile: hourlyInputResult.Value,
                HeatBalanceOptions: request.HeatBalanceOptions));

        if (matrixRequestResult.IsFailure)
            return Result<Iso52016MatrixRoomEnergySimulationResult>.Failure(matrixRequestResult);

        var matrixResult = _hourlySolver.Solve(matrixRequestResult.Value);

        if (matrixResult.IsFailure)
            return Result<Iso52016MatrixRoomEnergySimulationResult>.Failure(matrixResult);

        return Result<Iso52016MatrixRoomEnergySimulationResult>.Success(
            new Iso52016MatrixRoomEnergySimulationResult(
                RoomCode: request.RoomCode.Trim(),
                SolarGainProfile: solarGainResult.Value,
                InternalGainProfile: internalGainResult.Value,
                HourlyInputProfile: hourlyInputResult.Value,
                MatrixSolverRequest: matrixRequestResult.Value,
                MatrixSolverProfile: matrixResult.Value));
    }

    private static Result Validate(
        Iso52016RoomEnergySimulationRequest request)
    {
        if (request is null)
            return Result.Validation("ISO 52016 Matrix room energy simulation request is required.");

        if (string.IsNullOrWhiteSpace(request.RoomCode))
            return Result.Validation("Room code is required.");

        if (request.WeatherSolarContext is null)
            return Result.Validation("ISO 52016 weather-solar context is required.");

        if (request.WeatherSolarContext.HourCount == 0)
            return Result.Validation("ISO 52016 weather-solar context must contain hourly records.");

        if (request.Windows is null)
            return Result.Validation("Room window list is required.");

        if (request.OccupancyFactors is null)
            return Result.Validation("Occupancy factors are required.");

        if (request.EquipmentFactors is null)
            return Result.Validation("Equipment factors are required.");

        if (request.LightingFactors is null)
            return Result.Validation("Lighting factors are required.");

        return Result.Success();
    }
}