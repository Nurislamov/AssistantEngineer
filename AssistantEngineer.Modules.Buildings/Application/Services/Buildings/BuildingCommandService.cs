using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Mappers;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Persistence;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Buildings.Application.Services.Buildings;

public class BuildingCommandService
{
    private readonly IProjectRepository _projects;
    private readonly IClimateZoneRepository _climateZones;
    private readonly IBuildingRepository _buildings;
    private readonly IAppDbContext _context;
    private readonly ILogger<BuildingCommandService> _logger;

    public BuildingCommandService(
        IProjectRepository projects,
        IClimateZoneRepository climateZones,
        IBuildingRepository buildings,
        IAppDbContext context,
        ILogger<BuildingCommandService>? logger = null)
    {
        _projects = projects;
        _climateZones = climateZones;
        _buildings = buildings;
        _context = context;
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
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created building {BuildingId} for project {ProjectId}.",
            buildingResult.Value.Id,
            projectId);
        return Result<BuildingResponse>.Success(BuildingsMapper.ToResponse(buildingResult.Value));
    }
}
