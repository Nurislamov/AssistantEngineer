using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Mappers;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.SharedKernel.Abstractions;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Buildings.Application.Services.Buildings;

public class BuildingCommandService
{
    private readonly IProjectRepository _projects;
    private readonly IClimateZoneRepository _climateZones;
    private readonly IBuildingRepository _buildings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BuildingCommandService> _logger;

    public BuildingCommandService(
        IProjectRepository projects,
        IClimateZoneRepository climateZones,
        IBuildingRepository buildings,
        IUnitOfWork unitOfWork,
        ILogger<BuildingCommandService>? logger = null)
    {
        _projects = projects;
        _climateZones = climateZones;
        _buildings = buildings;
        _unitOfWork = unitOfWork;
        _logger = logger ?? NullLogger<BuildingCommandService>.Instance;
    }

    public async Task<Result<BuildingResponse>> CreateAsync(
        int projectId,
        CreateBuildingRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating building with name {BuildingName} for project {ProjectId}.",
            request.Name,
            projectId);

        var project = await _projects.GetByIdAsync(
            projectId,
            includeBuildings: true,
            cancellationToken: cancellationToken);
        if (project is null)
        {
            _logger.LogWarning("Cannot create building because project {ProjectId} was not found.", projectId);
            return Result<BuildingResponse>.NotFound($"Project with id {projectId} not found.");
        }

        ClimateZone? climateZone = null;
        if (request.ClimateZoneId.HasValue)
        {
            climateZone = await _climateZones.GetByIdAsync(request.ClimateZoneId.Value, cancellationToken);
            if (climateZone is null)
            {
                _logger.LogWarning(
                    "Cannot create building for project {ProjectId} because climate zone {ClimateZoneId} was not found.",
                    projectId,
                    request.ClimateZoneId);
                return Result<BuildingResponse>.NotFound($"Climate zone with id {request.ClimateZoneId} not found.");
            }
        }

        var buildingResult = Building.Create(request.Name, project, climateZone);
        if (buildingResult.IsFailure)
        {
            _logger.LogWarning(
                "Building creation failed for project {ProjectId} with name {BuildingName}: {Error}.",
                projectId,
                request.Name,
                buildingResult.Error);
            return Result<BuildingResponse>.Failure(buildingResult);
        }

        var addResult = project.AddBuilding(buildingResult.Value);
        if (addResult.IsFailure)
        {
            _logger.LogWarning(
                "Building add failed for project {ProjectId} with name {BuildingName}: {Error}.",
                projectId,
                request.Name,
                addResult.Error);
            return Result<BuildingResponse>.Failure(addResult);
        }

        _buildings.Add(buildingResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created building {BuildingId} for project {ProjectId}.",
            buildingResult.Value.Id,
            projectId);
        return Result<BuildingResponse>.Success(BuildingsMapper.ToResponse(buildingResult.Value));
    }

    public async Task<Result<BuildingResponse>> UpdateAsync(
        int buildingId,
        UpdateBuildingRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating building {BuildingId}.", buildingId);

        var building = await _buildings.GetByIdAsync(
            buildingId,
            includeClimateZone: true,
            cancellationToken: cancellationToken);
        if (building is null)
        {
            _logger.LogWarning("Cannot update building because building {BuildingId} was not found.", buildingId);
            return Result<BuildingResponse>.NotFound($"Building with id {buildingId} not found.");
        }

        var project = await _projects.GetByIdAsync(
            building.ProjectId,
            includeBuildings: true,
            cancellationToken: cancellationToken);
        if (project is null)
            return Result<BuildingResponse>.Validation("Unable to validate building project ownership.");

        if (project.Buildings.Any(existing =>
                existing.Id != buildingId &&
                existing.Name.Equals((request.Name ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Result<BuildingResponse>.Conflict($"Building with name '{request.Name}' already exists in this project.");
        }

        ClimateZone? climateZone = null;
        if (request.ClimateZoneId.HasValue)
        {
            climateZone = await _climateZones.GetByIdAsync(request.ClimateZoneId.Value, cancellationToken);
            if (climateZone is null)
            {
                _logger.LogWarning(
                    "Cannot update building {BuildingId} because climate zone {ClimateZoneId} was not found.",
                    buildingId,
                    request.ClimateZoneId);
                return Result<BuildingResponse>.NotFound($"Climate zone with id {request.ClimateZoneId} not found.");
            }
        }

        var updateNameResult = building.UpdateName(request.Name);
        if (updateNameResult.IsFailure)
            return Result<BuildingResponse>.Failure(updateNameResult);

        var climateZoneResult = building.SetClimateZone(climateZone);
        if (climateZoneResult.IsFailure)
            return Result<BuildingResponse>.Failure(climateZoneResult);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated building {BuildingId}.", buildingId);
        return Result<BuildingResponse>.Success(BuildingsMapper.ToResponse(building));
    }

    public async Task<Result> DeleteAsync(
        int buildingId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting building {BuildingId}.", buildingId);

        var building = await _buildings.GetByIdAsync(buildingId, cancellationToken: cancellationToken);
        if (building is null)
        {
            _logger.LogWarning("Cannot delete building because building {BuildingId} was not found.", buildingId);
            return Result.NotFound($"Building with id {buildingId} not found.");
        }

        _buildings.Remove(building);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted building {BuildingId}.", buildingId);
        return Result.Success();
    }
}
