using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
public class RoomVentilationController : ControllerBase
{
    private readonly IBuildingsFacade _buildings;
    private readonly ICalculationsFacade _calculations;

    public RoomVentilationController(
        IBuildingsFacade buildings,
        ICalculationsFacade calculations)
    {
        _buildings = buildings;
        _calculations = calculations;
    }

    [HttpGet("api/v{version:apiVersion}/rooms/{roomId:int}/ventilation-parameters")]
    public async Task<ActionResult<RoomVentilationParametersResponse>> Get(
        int roomId,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetRoomVentilationParametersAsync(roomId, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPut("api/v{version:apiVersion}/rooms/{roomId:int}/ventilation-parameters")]
    public async Task<ActionResult<RoomVentilationParametersResponse>> Upsert(
        int roomId,
        [FromBody] UpsertRoomVentilationParametersRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.UpsertRoomVentilationParametersAsync(roomId, request, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete("api/v{version:apiVersion}/rooms/{roomId:int}/ventilation-parameters")]
    public async Task<IActionResult> Delete(
        int roomId,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.DeleteRoomVentilationParametersAsync(roomId, cancellationToken);

        if (result.IsSuccess)
            return NoContent();

        return result.ToActionResult(this);
    }

    [HttpPost("api/v{version:apiVersion}/rooms/{roomId:int}/natural-ventilation/preview")]
    public async Task<ActionResult<NaturalVentilationPreviewResponse>> Preview(
        int roomId,
        [FromBody] NaturalVentilationPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _calculations.PreviewNaturalVentilationAsync(roomId, request, cancellationToken);
        return result.ToActionResult(this);
    }
}
