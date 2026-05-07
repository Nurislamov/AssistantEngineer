using AssistantEngineer.Api.Contracts.Buildings;
using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using Asp.Versioning;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Api.Extensions.Results;
using AssistantEngineer.Api.Querying.Buildings;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Buildings;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/building-archetypes")]
public class BuildingArchetypesController : ControllerBase
{
    private readonly IBuildingsFacade _buildings;

    public BuildingArchetypesController(IBuildingsFacade buildings)
    {
        _buildings = buildings;
    }

    [HttpGet]
    public ActionResult<PagedResponse<BuildingArchetypeSummary>> ListArchetypes(
        [FromQuery] BuildingArchetypeListQueryParameters query)
    {
        var items = _buildings
            .ListBuildingArchetypes()
            .ApplyBuildingArchetypeListQuery(query);

        return items.ToPagedOkResult(this, query);
    }

    private static readonly IReadOnlyDictionary<string, Func<IEnumerable<BuildingArchetypeSummary>, bool, IOrderedEnumerable<BuildingArchetypeSummary>>> SortRules =
        new Dictionary<string, Func<IEnumerable<BuildingArchetypeSummary>, bool, IOrderedEnumerable<BuildingArchetypeSummary>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["code"] = (items, descending) =>
                items.SortBy(descending, archetype => archetype.Code),

            ["displayname"] = (items, descending) =>
                items.SortBy(descending, archetype => archetype.DisplayName)
                    .ThenByStable(descending, archetype => archetype.Code),

            ["type"] = (items, descending) =>
                items.SortBy(descending, archetype => archetype.Type)
                    .ThenByStable(descending, archetype => archetype.Code),

            ["roomscount"] = (items, descending) =>
                items.SortBy(descending, archetype => archetype.RoomsCount)
                    .ThenByStable(descending, archetype => archetype.Code),

            ["roomaream2"] = (items, descending) =>
                items.SortBy(descending, archetype => archetype.RoomAreaM2)
                    .ThenByStable(descending, archetype => archetype.Code),

            ["roomheightm"] = (items, descending) =>
                items.SortBy(descending, archetype => archetype.RoomHeightM)
                    .ThenByStable(descending, archetype => archetype.Code)
        };
}

