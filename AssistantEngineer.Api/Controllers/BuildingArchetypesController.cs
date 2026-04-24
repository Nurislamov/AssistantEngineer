using AssistantEngineer.Api.Contracts.Buildings;
using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

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
        IEnumerable<BuildingArchetypeSummary> items = _buildings.ListBuildingArchetypes();

        if (query.Type.HasValue)
            items = items.Where(archetype => archetype.Type == query.Type.Value);

        items = SortArchetypes(
            items.ApplySearch(
                query.Search,
                archetype => archetype.Code,
                archetype => archetype.DisplayName,
                archetype => archetype.Type.ToString()),
            query);

        return Ok(items.ToPagedResponse(query));
    }

    private static IEnumerable<BuildingArchetypeSummary> SortArchetypes(
        IEnumerable<BuildingArchetypeSummary> source,
        CollectionQueryParameters query) =>
        (query.SortBy ?? "code").ToLowerInvariant() switch
        {
            "displayname" => query.SortDescending ? source.OrderByDescending(archetype => archetype.DisplayName).ThenByDescending(archetype => archetype.Code) : source.OrderBy(archetype => archetype.DisplayName).ThenBy(archetype => archetype.Code),
            "type" => query.SortDescending ? source.OrderByDescending(archetype => archetype.Type).ThenByDescending(archetype => archetype.Code) : source.OrderBy(archetype => archetype.Type).ThenBy(archetype => archetype.Code),
            "roomscount" => query.SortDescending ? source.OrderByDescending(archetype => archetype.RoomsCount).ThenByDescending(archetype => archetype.Code) : source.OrderBy(archetype => archetype.RoomsCount).ThenBy(archetype => archetype.Code),
            "roomaream2" => query.SortDescending ? source.OrderByDescending(archetype => archetype.RoomAreaM2).ThenByDescending(archetype => archetype.Code) : source.OrderBy(archetype => archetype.RoomAreaM2).ThenBy(archetype => archetype.Code),
            "roomheightm" => query.SortDescending ? source.OrderByDescending(archetype => archetype.RoomHeightM).ThenByDescending(archetype => archetype.Code) : source.OrderBy(archetype => archetype.RoomHeightM).ThenBy(archetype => archetype.Code),
            _ => query.SortDescending ? source.OrderByDescending(archetype => archetype.Code) : source.OrderBy(archetype => archetype.Code)
        };
}
