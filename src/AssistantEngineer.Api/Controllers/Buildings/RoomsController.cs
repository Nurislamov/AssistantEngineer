using AssistantEngineer.Api.Contracts.Buildings;
using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Api.Extensions.Http;
using AssistantEngineer.Api.Extensions.Results;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using Asp.Versioning;
using AssistantEngineer.Api.Querying.Buildings;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Buildings;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/rooms")]
public class RoomsController : ControllerBase
{
    private readonly IBuildingsFacade _buildings;

    public RoomsController(
        IBuildingsFacade buildings)
    {
        _buildings = buildings;
    }

    [HttpPost]
    public async Task<ActionResult<RoomResponse>> Create(
        [FromBody] CreateRoomRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.CreateRoomAsync(
            request,
            cancellationToken);

        return result.ToCreatedAtGetByIdResult(
            this,
            room => room.Id);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<RoomResponse>>> GetAll(
        [FromQuery] RoomListQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetRoomsAsync(
            cancellationToken);

        return result.ToPagedOkResult(
            this,
            query,
            items => items.ApplyRoomListQuery(query));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetRoomByIdAsync(
            id,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("{id:int}/windows")]
    public async Task<ActionResult<WindowResponse>> AddWindow(
        int id,
        [FromBody] CreateWindowRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.AddWindowAsync(
            id,
            request,
            cancellationToken);

        return result.ToOkResult(this);
    }

    [HttpPost("{id:int}/walls")]
    public async Task<ActionResult<WallResponse>> AddWall(
        int id,
        [FromBody] CreateWallRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.AddWallAsync(
            id,
            request,
            cancellationToken);

        return result.ToOkResult(this);
    }

    [HttpGet("{id:int}/windows")]
    public async Task<ActionResult<PagedResponse<WindowResponse>>> GetWindows(
        int id,
        [FromQuery] WindowListQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetRoomWindowsAsync(
            id,
            cancellationToken);

        return result.ToPagedOkResult(
            this,
            query,
            items => items.ApplyWindowListQuery(query));
    }

    [HttpGet("{id:int}/walls")]
    public async Task<ActionResult<PagedResponse<WallResponse>>> GetWalls(
        int id,
        [FromQuery] WallListQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetRoomWallsAsync(
            id,
            cancellationToken);

        return result.ToPagedOkResult(
            this,
            query,
            items => items.ApplyWallListQuery(query));
    }
}
