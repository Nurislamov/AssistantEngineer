using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.SharedKernel.Primitives;
using System.Collections.ObjectModel;

namespace AssistantEngineer.Modules.Buildings.Domain.ThermalZones;

public class ThermalZone
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public int BuildingId { get; private set; }
    public Building Building { get; private set; } = null!;

    private readonly List<Room> _rooms = new();
    public ICollection<Room> Rooms => _rooms;
    public IReadOnlyCollection<Room> AssignedRooms => new ReadOnlyCollection<Room>(_rooms);

    private ThermalZone() { }

    private ThermalZone(string name, Building building, IReadOnlyCollection<Room> rooms)
    {
        Name = name;
        Building = building;
        BuildingId = building.Id;
        _rooms.AddRange(rooms);
    }

    public static Result<ThermalZone> Create(
        string name,
        Building building,
        IEnumerable<Room> rooms)
    {
        var nameResult = name.ToRequiredTrimmed("Thermal zone name", maxLength: 100, minLength: 2);
        if (nameResult.IsFailure)
            return Result<ThermalZone>.Failure(nameResult);

        var roomList = rooms
            .GroupBy(room => room.Id)
            .Select(group => group.First())
            .ToList();

        if (roomList.Count == 0)
            return Result<ThermalZone>.Validation("At least one room must be assigned to a thermal zone.");

        if (roomList.Any(room => room.Floor.BuildingId != building.Id))
            return Result<ThermalZone>.Validation("All rooms must belong to the same building as the thermal zone.");

        return Result<ThermalZone>.Success(new ThermalZone(nameResult.Value, building, roomList));
    }

    public Result Rename(string name)
    {
        var nameResult = name.ToRequiredTrimmed("Thermal zone name", maxLength: 100, minLength: 2);
        if (nameResult.IsFailure)
            return nameResult;

        Name = nameResult.Value;
        return Result.Success();
    }

    public Result ReplaceRooms(IEnumerable<Room> rooms)
    {
        var roomList = rooms
            .GroupBy(room => room.Id)
            .Select(group => group.First())
            .ToList();

        if (roomList.Count == 0)
            return Result.Validation("At least one room must be assigned to a thermal zone.");

        if (roomList.Any(room => room.Floor.BuildingId != BuildingId))
            return Result.Validation("All rooms must belong to the same building as the thermal zone.");

        _rooms.Clear();
        _rooms.AddRange(roomList);
        return Result.Success();
    }
}