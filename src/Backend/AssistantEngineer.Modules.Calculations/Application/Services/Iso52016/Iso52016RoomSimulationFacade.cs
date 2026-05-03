using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016RoomSimulationFacade : IIso52016RoomSimulationFacade
{
    private readonly IIso52016WeatherSolarContextBuilder _weatherSolarContextBuilder;
    private readonly IIso52016RoomEnergySimulationRequestBuilder _simulationRequestBuilder;
    private readonly IIso52016RoomEnergySimulationService _simulationService;
    private readonly IIso52016V2RoomEnergySimulationService? _v2SimulationService;
    private readonly IIso52016V2RoomEnergySimulationResultMapper? _v2ResultMapper;

    public Iso52016RoomSimulationFacade(
        IIso52016WeatherSolarContextBuilder weatherSolarContextBuilder,
        IIso52016RoomEnergySimulationRequestBuilder simulationRequestBuilder,
        IIso52016RoomEnergySimulationService simulationService,
        IIso52016V2RoomEnergySimulationService? v2SimulationService = null,
        IIso52016V2RoomEnergySimulationResultMapper? v2ResultMapper = null)
    {
        _weatherSolarContextBuilder = weatherSolarContextBuilder;
        _simulationRequestBuilder = simulationRequestBuilder;
        _simulationService = simulationService;
        _v2SimulationService = v2SimulationService;
        _v2ResultMapper = v2ResultMapper;
    }

    public Result<Iso52016RoomSimulationFacadeResult> Simulate(
        Iso52016RoomSimulationFacadeRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016RoomSimulationFacadeResult>.Failure(validation);

        var weatherSolarContextResult = _weatherSolarContextBuilder.Build(
            new Iso52016WeatherSolarContextRequest(
                AnnualClimateData: request.AnnualClimateData,
                LatitudeDegrees: request.LatitudeDegrees,
                LongitudeDegrees: request.LongitudeDegrees,
                TimeZoneOffset: request.TimeZoneOffset,
                Surfaces: request.Surfaces,
                GroundReflectance: request.GroundReflectance,
                GroundBoundaryTemperature: request.GroundBoundaryTemperature));

        if (weatherSolarContextResult.IsFailure)
            return Result<Iso52016RoomSimulationFacadeResult>.Failure(weatherSolarContextResult);

        var simulationRequestResult = _simulationRequestBuilder.Build(
            new Iso52016RoomEnergySimulationBuildRequest(
                Room: request.Room,
                WeatherSolarContext: weatherSolarContextResult.Value,
                Defaults: request.Defaults,
                HeatingSetpointOverrideC: request.HeatingSetpointOverrideC,
                CoolingSetpointOverrideC: request.CoolingSetpointOverrideC));

        if (simulationRequestResult.IsFailure)
            return Result<Iso52016RoomSimulationFacadeResult>.Failure(simulationRequestResult);

        var simulationRequest = simulationRequestResult.Value with
        {
            HeatBalanceOptions = request.HeatBalanceOptions
        };

        var simulationResult = request.SimulationEngine switch
        {
            Iso52016SimulationEngine.Legacy => _simulationService.Simulate(
                simulationRequest),
            Iso52016SimulationEngine.V2Matrix => SimulateWithV2Matrix(
                simulationRequest),
            _ => Result<Iso52016RoomEnergySimulationResult>.Validation(
                "Unsupported ISO 52016 simulation engine.")
        };

        if (simulationResult.IsFailure)
            return Result<Iso52016RoomSimulationFacadeResult>.Failure(simulationResult);

        return Result<Iso52016RoomSimulationFacadeResult>.Success(
            new Iso52016RoomSimulationFacadeResult(
                RoomCode: request.Room.Name.Trim(),
                WeatherSolarContext: weatherSolarContextResult.Value,
                SimulationRequest: simulationRequest,
                SimulationResult: simulationResult.Value,
                SimulationEngine: request.SimulationEngine));
    }

    private Result<Iso52016RoomEnergySimulationResult> SimulateWithV2Matrix(
        Iso52016RoomEnergySimulationRequest simulationRequest)
    {
        if (_v2SimulationService is null)
        {
            return Result<Iso52016RoomEnergySimulationResult>.Failure(
                "ISO 52016 V2 room simulation service is not registered.");
        }

        if (_v2ResultMapper is null)
        {
            return Result<Iso52016RoomEnergySimulationResult>.Failure(
                "ISO 52016 V2 result mapper is not registered.");
        }

        var v2Result = _v2SimulationService.Simulate(
            simulationRequest);

        if (v2Result.IsFailure)
            return Result<Iso52016RoomEnergySimulationResult>.Failure(v2Result);

        return _v2ResultMapper.Map(
            v2Result.Value);
    }
    private static Result Validate(
        Iso52016RoomSimulationFacadeRequest request)
    {
        if (request.Room is null)
            return Result.Validation("Room is required.");

        if (!Enum.IsDefined(request.SimulationEngine))
            return Result.Validation("Unsupported ISO 52016 simulation engine.");

        if (request.AnnualClimateData is null)
            return Result.Validation("Annual climate data is required.");

        if (request.LatitudeDegrees is < -90.0 or > 90.0)
            return Result.Validation("Latitude must be between -90 and 90 degrees.");

        if (request.LongitudeDegrees is < -180.0 or > 180.0)
            return Result.Validation("Longitude must be between -180 and 180 degrees.");

        if (request.GroundReflectance is < 0.0 or > 1.0)
            return Result.Validation("Ground reflectance must be between 0 and 1.");

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