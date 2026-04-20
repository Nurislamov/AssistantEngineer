using AssistantEngineer.Domain.Primitives;
using AssistantEngineer.Domain.ValueObjects;
using System.Collections.ObjectModel;

namespace AssistantEngineer.Domain.Models;

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

    public Result<Room> AddRoom(
        string name,
        Area area,
        double heightM,
        Temperature indoorTemp,
        Temperature outdoorTemp,
        int peopleCount = 0,
        Power? equipmentLoad = null,
        Power? lightingLoad = null,
        RoomType type = RoomType.Office)
    {
        var roomResult = Room.Create(
            name, area, heightM, indoorTemp, outdoorTemp, this,
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
