using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Services.ThermalZones;
using AssistantEngineer.SharedKernel.Primitives;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
public class ThermalZonesController : ControllerBase
{
    private readonly ThermalZoneCommandService _command;
    private readonly ThermalZoneQueryService _query;

    public ThermalZonesController(
        ThermalZoneCommandService command,
        ThermalZoneQueryService query)
    {
        _command = command;
        _query = query;
    }

    [HttpGet("api/v{version:apiVersion}/buildings/{buildingId:int}/thermal-zones")]
    public async Task<ActionResult<List<ThermalZoneResponse>>> GetByBuilding(
        int buildingId,
        CancellationToken cancellationToken)
    {
        var result = await _query.GetByBuildingIdAsync(buildingId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("api/v{version:apiVersion}/buildings/{buildingId:int}/thermal-zones")]
    public async Task<ActionResult<ThermalZoneResponse>> Create(
        int buildingId,
        [FromBody] CreateThermalZoneRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _command.CreateAsync(buildingId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("api/v{version:apiVersion}/thermal-zones/{id:int}")]
    public async Task<ActionResult<ThermalZoneResponse>> Update(
        int id,
        [FromBody] UpdateThermalZoneRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _command.UpdateAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("api/v{version:apiVersion}/thermal-zones/{id:int}")]
    public async Task<ActionResult<ThermalZoneResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _query.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("api/v{version:apiVersion}/thermal-zones/{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _command.DeleteAsync(id, cancellationToken);

        if (result.IsSuccess)
            return NoContent();

        return result.ErrorType switch
        {
            ResultErrorType.NotFound => NotFound(result.Error),
            ResultErrorType.Validation => BadRequest(result.Error),
            ResultErrorType.Conflict => Conflict(result.Error),
            _ => BadRequest(result.Error)
        };
    }
}