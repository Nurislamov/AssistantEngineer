using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Mappers;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Options;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.SharedKernel.Abstractions;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Buildings.Application.Services.Buildings;

public sealed class BuildingArchetypeService
{
    private readonly IProjectRepository _projects;
    private readonly IClimateZoneRepository _climateZones;
    private readonly IBuildingRepository _buildings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly BuildingArchetypeCatalogOptions _catalog;

    public BuildingArchetypeService(
        IProjectRepository projects,
        IClimateZoneRepository climateZones,
        IBuildingRepository buildings,
        IUnitOfWork unitOfWork,
        IOptions<BuildingArchetypeCatalogOptions> catalog)
    {
        _projects = projects;
        _climateZones = climateZones;
        _buildings = buildings;
        _unitOfWork = unitOfWork;
        _catalog = catalog.Value;
    }

    public IReadOnlyList<BuildingArchetypeSummary> ListArchetypes() =>
        _catalog.Archetypes
            .Select(archetype => new BuildingArchetypeSummary(
                archetype.Code,
                archetype.DisplayName,
                archetype.Type,
                archetype.RoomsCount,
                archetype.RoomAreaM2,
                archetype.RoomHeightM))
            .ToArray();

    public async Task<Result<BuildingResponse>> CreateFromArchetypeAsync(
        int projectId,
        CreateBuildingFromArchetypeRequest request,
        CancellationToken cancellationToken = default)
    {
        var archetype = _catalog.Archetypes.FirstOrDefault(item => item.Code == request.ArchetypeCode);
        if (archetype is null)
            return Result<BuildingResponse>.Validation($"Unknown building archetype '{request.ArchetypeCode}'.");

        var project = await _projects.GetByIdAsync(projectId, includeBuildings: true, cancellationToken);
        if (project is null)
            return Result<BuildingResponse>.NotFound($"Project with id {projectId} not found.");

        ClimateZone? climateZone = null;
        if (request.ClimateZoneId.HasValue)
        {
            climateZone = await _climateZones.GetByIdAsync(request.ClimateZoneId.Value, cancellationToken);
            if (climateZone is null)
                return Result<BuildingResponse>.NotFound($"Climate zone with id {request.ClimateZoneId} not found.");
        }

        var building = Building.Create(request.Name, project, climateZone);
        if (building.IsFailure)
            return Result<BuildingResponse>.Failure(building);

        var addResult = project.AddBuilding(building.Value);
        if (addResult.IsFailure)
            return Result<BuildingResponse>.Failure(addResult);

        var floor = building.Value.AddFloor("Level 1");
        if (floor.IsFailure)
            return Result<BuildingResponse>.Failure(floor);

        for (var i = 1; i <= archetype.RoomsCount; i++)
        {
            var room = floor.Value.AddRoom(
                $"{archetype.DisplayName} {i:00}",
                Area.FromSquareMeters(archetype.RoomAreaM2).Value,
                archetype.RoomHeightM,
                Temperature.FromCelsius(archetype.IndoorTemperatureC).Value,
                peopleCount: archetype.PeopleCount,
                equipmentLoad: Power.FromWatts(archetype.RoomAreaM2 * archetype.EquipmentLoadWPerM2).Value,
                lightingLoad: Power.FromWatts(archetype.RoomAreaM2 * archetype.LightingLoadWPerM2).Value,
                type: archetype.Type);
            if (room.IsFailure)
                return Result<BuildingResponse>.Failure(room);

            var wall = room.Value.AddWall(
                Area.FromSquareMeters(archetype.RoomAreaM2 * archetype.ExternalWallAreaFactor).Value,
                ThermalTransmittance.FromValue(archetype.ExternalWallUValue).Value,
                GetRoomOrientation(archetype, i),
                WallBoundaryType.External);
            if (wall.IsFailure)
                return Result<BuildingResponse>.Failure(wall);

            var window = room.Value.AddWindow(
                Area.FromSquareMeters(Math.Max(archetype.WindowAreaM2Minimum, archetype.RoomAreaM2 * archetype.WindowAreaFactor)).Value,
                ThermalTransmittance.FromValue(archetype.WindowUValue).Value,
                SolarHeatGainCoefficient.FromValue(archetype.WindowShgc).Value,
                GetRoomOrientation(archetype, i));
            if (window.IsFailure)
                return Result<BuildingResponse>.Failure(window);
        }

        _buildings.Add(building.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<BuildingResponse>.Success(BuildingsMapper.ToResponse(building.Value));
    }

    private static CardinalDirection GetRoomOrientation(BuildingArchetypeOptions archetype, int roomNumber) =>
        roomNumber % 2 == 0
            ? archetype.EvenRoomOrientation
            : archetype.OddRoomOrientation;
}
