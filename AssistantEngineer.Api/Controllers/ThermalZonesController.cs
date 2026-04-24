using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
public class ThermalZonesController : ControllerBase
{
    private readonly IBuildingsFacade _buildings;

    public ThermalZonesController(IBuildingsFacade buildings)
    {
        _buildings = buildings;
    }

    [HttpGet("api/v{version:apiVersion}/buildings/{buildingId:int}/thermal-zones")]
    public async Task<ActionResult<PagedResponse<ThermalZoneResponse>>> GetByBuilding(
        int buildingId,
        [FromQuery] CollectionQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetThermalZonesByBuildingAsync(buildingId, cancellationToken);
        if (result.IsFailure)
            return ApiProblemDetailsFactory.CreateResult(HttpContext, result);

        var searchTerm = query.GetSearchTerm();
        IEnumerable<ThermalZoneResponse> items = result.Value;
        if (searchTerm is not null)
        {
            items = items.Where(zone =>
                zone.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                zone.Rooms.Any(room => room.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
        }

        items = SortThermalZones(items, query);
        return Ok(items.ToPagedResponse(query));
    }

    [HttpPost("api/v{version:apiVersion}/buildings/{buildingId:int}/thermal-zones")]
    public async Task<ActionResult<ThermalZoneResponse>> Create(
        int buildingId,
        [FromBody] CreateThermalZoneRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.CreateThermalZoneAsync(buildingId, request, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPut("api/v{version:apiVersion}/thermal-zones/{id:int}")]
    public async Task<ActionResult<ThermalZoneResponse>> Update(
        int id,
        [FromBody] UpdateThermalZoneRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.UpdateThermalZoneAsync(id, request, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("api/v{version:apiVersion}/thermal-zones/{id:int}")]
    public async Task<ActionResult<ThermalZoneResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetThermalZoneByIdAsync(id, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete("api/v{version:apiVersion}/thermal-zones/{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.DeleteThermalZoneAsync(id, cancellationToken);

        if (result.IsSuccess)
            return NoContent();

        return result.ToActionResult(this);
    }

    private static IEnumerable<ThermalZoneResponse> SortThermalZones(
        IEnumerable<ThermalZoneResponse> source,
        CollectionQueryParameters query) =>
        (query.SortBy ?? "id").ToLowerInvariant() switch
        {
            "name" => query.SortDescending ? source.OrderByDescending(zone => zone.Name).ThenByDescending(zone => zone.Id) : source.OrderBy(zone => zone.Name).ThenBy(zone => zone.Id),
            "roomscount" => query.SortDescending ? source.OrderByDescending(zone => zone.Rooms.Count).ThenByDescending(zone => zone.Id) : source.OrderBy(zone => zone.Rooms.Count).ThenBy(zone => zone.Id),
            _ => query.SortDescending ? source.OrderByDescending(zone => zone.Id) : source.OrderBy(zone => zone.Id)
        };
}
