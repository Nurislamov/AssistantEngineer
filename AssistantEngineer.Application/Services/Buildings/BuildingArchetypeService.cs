using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Contracts.Responses;
using AssistantEngineer.Domain.Models;
using AssistantEngineer.Domain.Primitives;
using AssistantEngineer.Domain.ValueObjects;

namespace AssistantEngineer.Application.Services.Buildings;

public sealed class BuildingArchetypeService
{
    private readonly IProjectRepository _projects;
    private readonly IClimateZoneRepository _climateZones;
    private readonly IBuildingRepository _buildings;
    private readonly IAppDbContext _context;

    public BuildingArchetypeService(
        IProjectRepository projects,
        IClimateZoneRepository climateZones,
        IBuildingRepository buildings,
        IAppDbContext context)
    {
        _projects = projects;
        _climateZones = climateZones;
        _buildings = buildings;
        _context = context;
    }

    public IReadOnlyList<BuildingArchetypeSummary> ListArchetypes() =>
    [
        new("office-small", "Small office", RoomType.Office, 6, 18, 3),
        new("residential-small", "Small residential", RoomType.Residential, 4, 20, 2.8),
        new("retail-small", "Small retail", RoomType.Retail, 3, 40, 3.5)
    ];

    public async Task<Result<BuildingResponse>> CreateFromArchetypeAsync(
        int projectId,
        CreateBuildingFromArchetypeRequest request,
        CancellationToken cancellationToken = default)
    {
        var archetype = ListArchetypes().FirstOrDefault(item => item.Code == request.ArchetypeCode);
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
                Temperature.FromCelsius(archetype.Type == RoomType.Residential ? 21 : 22).Value,
                Temperature.FromCelsius(35).Value,
                peopleCount: archetype.Type == RoomType.Residential ? 2 : 1,
                equipmentLoad: Power.FromWatts(archetype.RoomAreaM2 * 8).Value,
                lightingLoad: Power.FromWatts(archetype.RoomAreaM2 * 7).Value,
                archetype.Type);
            if (room.IsFailure)
                return Result<BuildingResponse>.Failure(room);

            var wall = room.Value.AddWall(
                Area.FromSquareMeters(archetype.RoomAreaM2 * 0.8).Value,
                isExternal: true,
                ThermalTransmittance.FromValue(1.2).Value,
                i % 2 == 0 ? CardinalDirection.East : CardinalDirection.South);
            if (wall.IsFailure)
                return Result<BuildingResponse>.Failure(wall);

            _ = room.Value.AddWindow(
                Area.FromSquareMeters(Math.Max(1.5, archetype.RoomAreaM2 * 0.12)).Value,
                ThermalTransmittance.FromValue(2.2).Value,
                SolarHeatGainCoefficient.FromValue(0.5).Value,
                i % 2 == 0 ? CardinalDirection.East : CardinalDirection.South);
        }

        _buildings.Add(building.Value);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<BuildingResponse>.Success(ApplicationMapper.ToResponse(building.Value));
    }
}

public sealed class CreateBuildingFromArchetypeRequest
{
    public string ArchetypeCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? ClimateZoneId { get; set; }
}

public sealed record BuildingArchetypeSummary(
    string Code,
    string DisplayName,
    RoomType Type,
    int RoomsCount,
    double RoomAreaM2,
    double RoomHeightM);
