using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016BuildingDomainSimulationFacade : IIso52016BuildingDomainSimulationFacade
{
    private readonly IIso52016BuildingRoomCollector _roomCollector;
    private readonly IIso52016BuildingSimulationFacade _buildingSimulationFacade;

    public Iso52016BuildingDomainSimulationFacade(
        IIso52016BuildingRoomCollector roomCollector,
        IIso52016BuildingSimulationFacade buildingSimulationFacade)
    {
        _roomCollector = roomCollector;
        _buildingSimulationFacade = buildingSimulationFacade;
    }

    public Result<Iso52016BuildingDomainSimulationFacadeResult> Simulate(
        Iso52016BuildingDomainSimulationFacadeRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016BuildingDomainSimulationFacadeResult>.Failure(validation);

        var roomsResult = _roomCollector.CollectRooms(
            request.Building);

        if (roomsResult.IsFailure)
            return Result<Iso52016BuildingDomainSimulationFacadeResult>.Failure(roomsResult);

        var simulationResult = _buildingSimulationFacade.Simulate(
            new Iso52016BuildingSimulationFacadeRequest(
                BuildingCode: request.Building.Name,
                Rooms: roomsResult.Value,
                AnnualClimateData: request.AnnualClimateData,
                LatitudeDegrees: request.LatitudeDegrees,
                LongitudeDegrees: request.LongitudeDegrees,
                TimeZoneOffset: request.TimeZoneOffset,
                Surfaces: request.Surfaces,
                GroundReflectance: request.GroundReflectance,
                GroundBoundaryTemperature: request.GroundBoundaryTemperature,
                Defaults: request.Defaults,
                HeatingSetpointOverrideC: request.HeatingSetpointOverrideC,
                CoolingSetpointOverrideC: request.CoolingSetpointOverrideC,
                HeatBalanceOptions: request.HeatBalanceOptions,
                SimulationEngine: request.SimulationEngine));

        if (simulationResult.IsFailure)
            return Result<Iso52016BuildingDomainSimulationFacadeResult>.Failure(simulationResult);

        return Result<Iso52016BuildingDomainSimulationFacadeResult>.Success(
            new Iso52016BuildingDomainSimulationFacadeResult(
                BuildingId: request.Building.Id,
                BuildingName: request.Building.Name,
                Rooms: roomsResult.Value,
                SimulationResult: simulationResult.Value));
    }

    private static Result Validate(
        Iso52016BuildingDomainSimulationFacadeRequest request)
    {
        if (request.Building is null)
            return Result.Validation("Building is required.");

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