using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
public sealed class RoomGroundContactController : ControllerBase
{
    private readonly IBuildingsFacade _buildings;

    public RoomGroundContactController(IBuildingsFacade buildings)
    {
        _buildings = buildings;
    }
    [HttpGet("api/v{version:apiVersion}/rooms/{roomId:int}/ground-contact")]
    public async Task<ActionResult<RoomGroundContactResponse>> Get(
        int roomId,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetRoomGroundContactAsync(roomId, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPut("api/v{version:apiVersion}/rooms/{roomId:int}/ground-contact")]
    public async Task<ActionResult<RoomGroundContactResponse>> Upsert(
        int roomId,
        [FromBody] UpsertRoomGroundContactRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.UpsertRoomGroundContactAsync(roomId, request, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete("api/v{version:apiVersion}/rooms/{roomId:int}/ground-contact")]
    public async Task<IActionResult> Delete(
        int roomId,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.DeleteRoomGroundContactAsync(roomId, cancellationToken);

        if (result.IsSuccess)
            return NoContent();

        return result.ToActionResult(this);
    }
}