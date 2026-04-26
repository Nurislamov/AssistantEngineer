using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using Asp.Versioning;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Api.Extensions.Http;
using AssistantEngineer.Api.Extensions.Results;
using AssistantEngineer.Api.Querying.Buildings;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Buildings;

[ApiController]
[ApiVersion("1.0")]
public class ThermalZonesController : ControllerBase
{
    private readonly IBuildingsFacade _buildings;

    public ThermalZonesController(
        IBuildingsFacade buildings)
    {
        _buildings = buildings;
    }

    [HttpGet("api/v{version:apiVersion}/buildings/{buildingId:int}/thermal-zones")]
    public async Task<ActionResult<PagedResponse<ThermalZoneResponse>>> GetByBuilding(
        int buildingId,
        [FromQuery] CollectionQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetThermalZonesByBuildingAsync(
            buildingId,
            cancellationToken);

        return result.ToPagedOkResult(
            this,
            query,
            items => items.ApplyThermalZoneListQuery(query));
    }

    [HttpPost("api/v{version:apiVersion}/buildings/{buildingId:int}/thermal-zones")]
    public async Task<ActionResult<ThermalZoneResponse>> Create(
        int buildingId,
        [FromBody] CreateThermalZoneRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.CreateThermalZoneAsync(
            buildingId,
            request,
            cancellationToken);

        return result.ToCreatedAtGetByIdResult(
            this,
            zone => zone.Id);
    }

    [HttpPut("api/v{version:apiVersion}/thermal-zones/{id:int}")]
    public async Task<ActionResult<ThermalZoneResponse>> Update(
        int id,
        [FromBody] UpdateThermalZoneRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.UpdateThermalZoneAsync(
            id,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpGet("api/v{version:apiVersion}/thermal-zones/{id:int}")]
    public async Task<ActionResult<ThermalZoneResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetThermalZoneByIdAsync(
            id,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpDelete("api/v{version:apiVersion}/thermal-zones/{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.DeleteThermalZoneAsync(
            id,
            cancellationToken);

        return result.ToNoContentResult(this);
    }
    
    private static readonly IReadOnlyDictionary<string, Func<IEnumerable<ThermalZoneResponse>, bool, IOrderedEnumerable<ThermalZoneResponse>>> SortRules =
        new Dictionary<string, Func<IEnumerable<ThermalZoneResponse>, bool, IOrderedEnumerable<ThermalZoneResponse>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = (items, descending) =>
                items.SortBy(descending, zone => zone.Id),

            ["name"] = (items, descending) =>
                items.SortBy(descending, zone => zone.Name)
                    .ThenByStable(descending, zone => zone.Id),

            ["roomscount"] = (items, descending) =>
                items.SortBy(descending, zone => zone.Rooms.Count)
                    .ThenByStable(descending, zone => zone.Id)
        };
}