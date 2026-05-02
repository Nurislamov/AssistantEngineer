using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016RoomEnergySimulationService : IIso52016RoomEnergySimulationService
{
    private readonly IIso52016RoomSolarGainProfileBuilder _solarGainProfileBuilder;
    private readonly IIso52016RoomInternalGainProfileBuilder _internalGainProfileBuilder;
    private readonly IIso52016RoomHourlyInputProfileBuilder _hourlyInputProfileBuilder;
    private readonly IIso52016RoomHeatBalanceSolver _heatBalanceSolver;

    public Iso52016RoomEnergySimulationService(
        IIso52016RoomSolarGainProfileBuilder solarGainProfileBuilder,
        IIso52016RoomInternalGainProfileBuilder internalGainProfileBuilder,
        IIso52016RoomHourlyInputProfileBuilder hourlyInputProfileBuilder,
        IIso52016RoomHeatBalanceSolver heatBalanceSolver)
    {
        _solarGainProfileBuilder = solarGainProfileBuilder;
        _internalGainProfileBuilder = internalGainProfileBuilder;
        _hourlyInputProfileBuilder = hourlyInputProfileBuilder;
        _heatBalanceSolver = heatBalanceSolver;
    }

    public Result<Iso52016RoomEnergySimulationResult> Simulate(
        Iso52016RoomEnergySimulationRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016RoomEnergySimulationResult>.Failure(validation);

        var solarGainResult = _solarGainProfileBuilder.Build(
            new Iso52016RoomSolarGainProfileRequest(
                RoomCode: request.RoomCode,
                WeatherSolarContext: request.WeatherSolarContext,
                Windows: request.Windows));

        if (solarGainResult.IsFailure)
            return Result<Iso52016RoomEnergySimulationResult>.Failure(solarGainResult);

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
            return Result<Iso52016RoomEnergySimulationResult>.Failure(internalGainResult);

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
            return Result<Iso52016RoomEnergySimulationResult>.Failure(hourlyInputResult);

        var heatBalanceResult = _heatBalanceSolver.Solve(
            new Iso52016RoomHeatBalanceRequest(
                InputProfile: hourlyInputResult.Value,
                Options: request.HeatBalanceOptions));

        if (heatBalanceResult.IsFailure)
            return Result<Iso52016RoomEnergySimulationResult>.Failure(heatBalanceResult);

        return Result<Iso52016RoomEnergySimulationResult>.Success(
            new Iso52016RoomEnergySimulationResult(
                RoomCode: request.RoomCode.Trim(),
                SolarGainProfile: solarGainResult.Value,
                InternalGainProfile: internalGainResult.Value,
                HourlyInputProfile: hourlyInputResult.Value,
                HeatBalanceProfile: heatBalanceResult.Value));
    }

    private static Result Validate(
        Iso52016RoomEnergySimulationRequest request)
    {
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