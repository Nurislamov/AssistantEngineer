using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

namespace AssistantEngineer.Api.Sorting.Buildings;

internal static class RoomSortRules
{
    public static readonly IReadOnlyDictionary<string, SortRule<RoomResponse>> ByField =
        new Dictionary<string, SortRule<RoomResponse>>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = (items, descending) =>
                items.SortBy(descending, room => room.Id),

            ["name"] = (items, descending) =>
                items.SortBy(descending, room => room.Name)
                    .ThenByStable(descending, room => room.Id),

            ["aream2"] = (items, descending) =>
                items.SortBy(descending, room => room.AreaM2)
                    .ThenByStable(descending, room => room.Id),

            ["volumem3"] = (items, descending) =>
                items.SortBy(descending, room => room.VolumeM3)
                    .ThenByStable(descending, room => room.Id),

            ["indoortemperaturec"] = (items, descending) =>
                items.SortBy(descending, room => room.IndoorTemperatureC)
                    .ThenByStable(descending, room => room.Id),

            ["peoplecount"] = (items, descending) =>
                items.SortBy(descending, room => room.PeopleCount)
                    .ThenByStable(descending, room => room.Id),

            ["type"] = (items, descending) =>
                items.SortBy(descending, room => room.Type)
                    .ThenByStable(descending, room => room.Id)
        };
}