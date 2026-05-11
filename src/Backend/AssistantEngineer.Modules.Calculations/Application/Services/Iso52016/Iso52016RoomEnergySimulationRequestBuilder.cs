using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016RoomEnergySimulationRequestBuilder : ISo52016RoomEnergySimulationRequestBuilder
{
    private readonly ISo52016RoomWindowSolarGainInputMapper _windowMapper;
    private readonly ISo52016RoomEnvelopeInputCalculator _envelopeInputCalculator;
    private readonly ISo52016ScheduleProfileExpander _scheduleProfileExpander;

    public Iso52016RoomEnergySimulationRequestBuilder(
        ISo52016RoomWindowSolarGainInputMapper windowMapper,
        ISo52016RoomEnvelopeInputCalculator envelopeInputCalculator,
        ISo52016ScheduleProfileExpander scheduleProfileExpander)
    {
        _windowMapper = windowMapper;
        _envelopeInputCalculator = envelopeInputCalculator;
        _scheduleProfileExpander = scheduleProfileExpander;
    }

    public Result<Iso52016RoomEnergySimulationRequest> Build(
        Iso52016RoomEnergySimulationBuildRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016RoomEnergySimulationRequest>.Failure(validation);

        var defaults = request.Defaults ?? new Iso52016RoomSimulationDefaults();

        var windowsResult = _windowMapper.Map(
            request.Room,
            defaults);

        if (windowsResult.IsFailure)
            return Result<Iso52016RoomEnergySimulationRequest>.Failure(windowsResult);

        var envelopeResult = _envelopeInputCalculator.Calculate(
            request.Room,
            defaults);

        if (envelopeResult.IsFailure)
            return Result<Iso52016RoomEnergySimulationRequest>.Failure(envelopeResult);

        var hourCount = request.WeatherSolarContext.HourCount;

        var occupancyFactors = _scheduleProfileExpander.ExpandToAnnualProfile(
            request.Room.OccupancySchedule,
            hourCount,
            defaultFactor: 1.0);

        var equipmentFactors = _scheduleProfileExpander.ExpandToAnnualProfile(
            request.Room.EquipmentSchedule,
            hourCount,
            defaultFactor: 1.0);

        var lightingFactors = _scheduleProfileExpander.ExpandToAnnualProfile(
            request.Room.LightingSchedule,
            hourCount,
            defaultFactor: 1.0);

        var heatingSetpoint =
            request.HeatingSetpointOverrideC ??
            request.Room.IndoorTemperature.Celsius;

        var coolingSetpoint =
            request.CoolingSetpointOverrideC ??
            Math.Max(
                defaults.DefaultCoolingSetpointC,
                heatingSetpoint + 1.0);

        return Result<Iso52016RoomEnergySimulationRequest>.Success(
            new Iso52016RoomEnergySimulationRequest(
                RoomCode: request.Room.Name,
                WeatherSolarContext: request.WeatherSolarContext,
                Windows: windowsResult.Value,
                PeopleCount: request.Room.PeopleCount,
                SensibleHeatGainPerPersonW: defaults.DefaultSensibleHeatGainPerPersonW,
                EquipmentLoadW: request.Room.EquipmentLoad.Watts,
                LightingLoadW: request.Room.LightingLoad.Watts,
                OccupancyFactors: occupancyFactors,
                EquipmentFactors: equipmentFactors,
                LightingFactors: lightingFactors,
                TransmissionHeatTransferCoefficientWPerK: envelopeResult.Value.TransmissionHeatTransferCoefficientWPerK,
                VentilationHeatTransferCoefficientWPerK: envelopeResult.Value.VentilationHeatTransferCoefficientWPerK,
                ThermalCapacityJPerK: envelopeResult.Value.ThermalCapacityJPerK,
                HeatingSetpointC: heatingSetpoint,
                CoolingSetpointC: coolingSetpoint));
    }

    private static Result Validate(
        Iso52016RoomEnergySimulationBuildRequest request)
    {
        if (request.Room is null)
            return Result.Validation("Room is required.");

        if (request.WeatherSolarContext is null)
            return Result.Validation("ISO 52016 weather-solar context is required.");

        if (request.WeatherSolarContext.HourCount == 0)
            return Result.Validation("ISO 52016 weather-solar context must contain hourly records.");

        if (request.CoolingSetpointOverrideC.HasValue &&
            request.HeatingSetpointOverrideC.HasValue &&
            request.CoolingSetpointOverrideC.Value <= request.HeatingSetpointOverrideC.Value)
        {
            return Result.Validation(
                "Cooling setpoint override must be greater than heating setpoint override.");
        }

        return Result.Success();
    }
}