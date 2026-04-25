using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.Modules.Buildings.Domain.ThermalZones;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using System.Collections.ObjectModel;

namespace AssistantEngineer.Modules.Buildings.Domain.Entities;

public class Building
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public ClimateZone? ClimateZone { get; private set; }

    private readonly List<Floor> _floors = new();
    public IReadOnlyCollection<Floor> Floors => new ReadOnlyCollection<Floor>(_floors);

    private readonly List<ThermalZone> _thermalZones = new();
    public IReadOnlyCollection<ThermalZone> ThermalZones => new ReadOnlyCollection<ThermalZone>(_thermalZones);

    public int ProjectId { get; private set; }
    public Project Project { get; private set; } = null!;

    private Building() { }

    private Building(string name, Project project, ClimateZone? climateZone = null)
    {
        Name = name;
        Project = project;
        ProjectId = project.Id;
        ClimateZone = climateZone;
    }

    public static Result<Building> Create(string name, Project project, ClimateZone? climateZone = null)
    {
        var nameResult = name.ToRequiredTrimmed("Building name", maxLength: 200);
        if (nameResult.IsFailure) return Result<Building>.Failure(nameResult);

        return Result<Building>.Success(new Building(nameResult.Value, project, climateZone));
    }

    public Result<Floor> AddFloor(string name)
    {
        var floorResult = Floor.Create(name, this);
        if (floorResult.IsFailure)
            return Result<Floor>.Failure(floorResult);

        var floor = floorResult.Value;

        if (_floors.Any(f => f.Name.Equals(floor.Name, StringComparison.OrdinalIgnoreCase)))
            return Result<Floor>.Conflict($"Floor with name '{floor.Name}' already exists in this building.");

        _floors.Add(floor);
        return Result<Floor>.Success(floor);
    }

    public Result RemoveFloor(int floorId)
    {
        var floor = _floors.FirstOrDefault(f => f.Id == floorId);
        if (floor == null)
            return Result.NotFound($"Floor with id {floorId} not found.");

        _floors.Remove(floor);
        return Result.Success();
    }

    public Result SetClimateZone(ClimateZone? climateZone)
    {
        ClimateZone = climateZone;
        return Result.Success();
    }

    public Result<ThermalZone> AddThermalZone(string name, IEnumerable<Room> rooms)
    {
        var assignedRooms = rooms.Distinct().ToArray();
        var knownRooms = _floors
            .SelectMany(floor => floor.Rooms)
            .ToHashSet();

        if (assignedRooms.Any(room => !knownRooms.Contains(room)))
            return Result<ThermalZone>.Validation("Thermal zone rooms must belong to this building.");

        var assignedRoomSet = assignedRooms.ToHashSet();
        var alreadyAssignedRooms = _thermalZones
            .SelectMany(zone => zone.AssignedRooms)
            .ToHashSet();

        if (alreadyAssignedRooms.Overlaps(assignedRoomSet))
            return Result<ThermalZone>.Conflict("Room cannot belong to more than one thermal zone.");

        if (_thermalZones.Any(zone => zone.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return Result<ThermalZone>.Conflict($"Thermal zone with name '{name}' already exists in this building.");

        var zoneResult = ThermalZone.Create(name, this, assignedRooms);
        if (zoneResult.IsFailure)
            return Result<ThermalZone>.Failure(zoneResult);

        _thermalZones.Add(zoneResult.Value);
        return Result<ThermalZone>.Success(zoneResult.Value);
    }
    
    public Result RemoveThermalZone(int thermalZoneId)
    {
        var thermalZone = _thermalZones.FirstOrDefault(x => x.Id == thermalZoneId);
        if (thermalZone is null)
            return Result.NotFound($"Thermal zone with id {thermalZoneId} not found.");

        _thermalZones.Remove(thermalZone);
        return Result.Success();
    }
}
