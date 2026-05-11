using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016BuildingRoomCollector : ISo52016BuildingRoomCollector
{
    public Result<IReadOnlyList<Room>> CollectRooms(
        Building building)
    {
        if (building is null)
            return Result<IReadOnlyList<Room>>.Validation("Building is required.");

        var rooms = building.Floors
            .SelectMany(floor => floor.Rooms)
            .OrderBy(room => room.Id)
            .ThenBy(room => room.Name)
            .ToArray();

        if (rooms.Length == 0)
            return Result<IReadOnlyList<Room>>.Validation("Building must contain at least one room.");

        var duplicateRoomNames = rooms
            .GroupBy(room => room.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (duplicateRoomNames.Length > 0)
        {
            return Result<IReadOnlyList<Room>>.Conflict(
                $"Room names must be unique inside building simulation request: {string.Join(", ", duplicateRoomNames)}.");
        }

        return Result<IReadOnlyList<Room>>.Success(rooms);
    }
}