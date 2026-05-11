using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016BuildingEnergySimulationApplicationService : ISo52016BuildingEnergySimulationApplicationService
{
    private readonly IBuildingRepository _buildings;
    private readonly IAnnualClimateDataProvider _annualClimateData;
    private readonly ISo52016BuildingDomainSimulationFacade _buildingSimulationFacade;
    private readonly Iso52016EnergyNeedOptions _options;

    public Iso52016BuildingEnergySimulationApplicationService(
        IBuildingRepository buildings,
        IAnnualClimateDataProvider annualClimateData,
        ISo52016BuildingDomainSimulationFacade buildingSimulationFacade,
        IOptions<Iso52016EnergyNeedOptions> options)
    {
        _buildings = buildings;
        _annualClimateData = annualClimateData;
        _buildingSimulationFacade = buildingSimulationFacade;
        _options = options.Value;
    }

    public async Task<Result<Iso52016BuildingEnergySimulationApplicationResult>> SimulateAsync(
        Iso52016BuildingEnergySimulationApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016BuildingEnergySimulationApplicationResult>.Failure(validation);

        var building = await _buildings.GetForCalculationAsync(
            request.BuildingId,
            cancellationToken);

        if (building is null)
        {
            return Result<Iso52016BuildingEnergySimulationApplicationResult>.NotFound(
                $"Building with id {request.BuildingId} not found.");
        }

        if (building.ClimateZone is null)
            return Result<Iso52016BuildingEnergySimulationApplicationResult>.Validation("Building climate zone is not assigned.");

        var weatherYear =
            request.WeatherYear ??
            _options.DefaultWeatherYear;

        var annualClimateData = await _annualClimateData.GetForClimateZoneAsync(
            building.ClimateZone.Id,
            weatherYear,
            cancellationToken);

        if (annualClimateData is null)
        {
            return Result<Iso52016BuildingEnergySimulationApplicationResult>.Validation(
                $"Annual climate data for climate zone {building.ClimateZone.Id} and year {weatherYear} was not found.");
        }

        if (annualClimateData.HourlyData.Count == 0)
        {
            return Result<Iso52016BuildingEnergySimulationApplicationResult>.Validation(
                "Annual climate data must contain hourly records.");
        }

        var simulationResult = _buildingSimulationFacade.Simulate(
            new Iso52016BuildingDomainSimulationFacadeRequest(
                Building: building,
                AnnualClimateData: annualClimateData,
                LatitudeDegrees: request.LatitudeDegrees,
                LongitudeDegrees: request.LongitudeDegrees,
                TimeZoneOffset: request.TimeZoneOffset,
                Surfaces: request.Surfaces,
                GroundReflectance: request.GroundReflectance,
                GroundBoundaryTemperature: request.GroundBoundaryTemperature,
                Defaults: request.Defaults,
                HeatingSetpointOverrideC: request.HeatingSetpointOverrideC,
                CoolingSetpointOverrideC: request.CoolingSetpointOverrideC,
                HeatBalanceOptions: request.HeatBalanceOptions));

        if (simulationResult.IsFailure)
            return Result<Iso52016BuildingEnergySimulationApplicationResult>.Failure(simulationResult);

        return Result<Iso52016BuildingEnergySimulationApplicationResult>.Success(
            new Iso52016BuildingEnergySimulationApplicationResult(
                BuildingId: building.Id,
                BuildingName: building.Name,
                ClimateZoneId: building.ClimateZone.Id,
                WeatherYear: weatherYear,
                LatitudeDegrees: request.LatitudeDegrees,
                LongitudeDegrees: request.LongitudeDegrees,
                TimeZoneOffset: request.TimeZoneOffset,
                Simulation: simulationResult.Value));
    }

    private static Result Validate(
        Iso52016BuildingEnergySimulationApplicationRequest request)
    {
        if (request.BuildingId <= 0)
            return Result.Validation("Building id must be greater than zero.");
        if (request.LatitudeDegrees is < -90.0 or > 90.0)
            return Result.Validation("Latitude must be between -90 and 90 degrees.");

        if (request.LongitudeDegrees is < -180.0 or > 180.0)
            return Result.Validation("Longitude must be between -180 and 180 degrees.");

        if (request.WeatherYear.HasValue &&
            request.WeatherYear.Value is < 1900 or > 2100)
        {
            return Result.Validation("Weather year must be between 1900 and 2100.");
        }

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