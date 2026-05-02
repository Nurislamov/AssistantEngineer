using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;
using System.Collections.ObjectModel;

namespace AssistantEngineer.Modules.Buildings.Domain.Entities;

public class Floor
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    private readonly List<Room> _rooms = new();
    public IReadOnlyCollection<Room> Rooms => new ReadOnlyCollection<Room>(_rooms);

    public int BuildingId { get; private set; }
    public Building Building { get; private set; } = null!;

    private Floor() { }

    private Floor(string name, Building building)
    {
        Name = name;
        Building = building;
        BuildingId = building.Id;
    }

    public static Result<Floor> Create(string name, Building building)
    {
        var nameResult = name.ToRequiredTrimmed("Floor name", maxLength: 100);
        if (nameResult.IsFailure) return Result<Floor>.Failure(nameResult);

        return Result<Floor>.Success(new Floor(nameResult.Value, building));
    }

    public Result UpdateName(string name)
    {
        var nameResult = name.ToRequiredTrimmed("Floor name", maxLength: 100);
        if (nameResult.IsFailure) return nameResult;

        Name = nameResult.Value;
        return Result.Success();
    }

    public Result<Room> AddRoom(
        string name,
        Area area,
        double heightM,
        Temperature indoorTemp,
        Temperature? outdoorTemperatureOverride = null,
        int peopleCount = 0,
        Power? equipmentLoad = null,
        Power? lightingLoad = null,
        RoomType type = RoomType.Office)
    {
        var roomResult = Room.Create(
            name, area, heightM, indoorTemp, outdoorTemperatureOverride, this,
            peopleCount, equipmentLoad, lightingLoad, type);

        if (roomResult.IsFailure)
            return Result<Room>.Failure(roomResult);

        var room = roomResult.Value;

        if (_rooms.Any(r => r.Name.Equals(room.Name, StringComparison.OrdinalIgnoreCase)))
            return Result<Room>.Conflict($"Room with name '{room.Name}' already exists on this floor.");

        _rooms.Add(room);
        return Result<Room>.Success(room);
    }

    public Result RemoveRoom(int roomId)
    {
        var room = _rooms.FirstOrDefault(r => r.Id == roomId);
        if (room == null)
            return Result.NotFound($"Room with id {roomId} not found.");

        _rooms.Remove(room);
        return Result.Success();
    }
}
