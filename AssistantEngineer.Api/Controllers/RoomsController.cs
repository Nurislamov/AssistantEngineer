using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Api.Facades;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Requests;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/rooms")]
public class RoomsController : ControllerBase
{
    private readonly IRoomsFacade _rooms;

    public RoomsController(IRoomsFacade rooms)
    {
        _rooms = rooms;
    }

    [HttpPost]
    public async Task<ActionResult<RoomResponse>> Create(
        [FromBody] CreateRoomRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _rooms.CreateAsync(request, cancellationToken);
        return result.ToCreatedResult(nameof(GetById), room => room.Id);
    }

    [HttpGet]
    public async Task<ActionResult<List<RoomResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _rooms.GetAllAsync(cancellationToken);
        return result.ToOkResult();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _rooms.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}/calculate")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<RoomCalculationResult>> Calculate(
        int id,
        [FromQuery] CoolingLoadCalculationMethodDto method = CoolingLoadCalculationMethodDto.Simplified,
        CancellationToken cancellationToken = default)
    {
        var result = await _rooms.CalculateAsync(id, method, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}/heating-load")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<RoomHeatingLoadResult>> CalculateHeatingLoad(
        int id,
        [FromQuery] HeatingLoadCalculationMethodDto method = HeatingLoadCalculationMethodDto.En12831,
        CancellationToken cancellationToken = default)
    {
        var result = await _rooms.CalculateHeatingLoadAsync(id, method, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:int}/windows")]
    public async Task<ActionResult<WindowResponse>> AddWindow(
        int id,
        [FromBody] CreateWindowRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _rooms.AddWindowAsync(id, request, cancellationToken);
        return result.ToOkResult();
    }

    [HttpPost("{id:int}/walls")]
    public async Task<ActionResult<WallResponse>> AddWall(
        int id,
        [FromBody] CreateWallRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _rooms.AddWallAsync(id, request, cancellationToken);
        return result.ToOkResult();
    }

    [HttpPost("{id:int}/select-equipment")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<EquipmentSelectionResult>> SelectEquipment(
        int id,
        [FromBody] EquipmentSelectionRequest request,
        [FromQuery] CoolingLoadCalculationMethodDto method = CoolingLoadCalculationMethodDto.Simplified,
        CancellationToken cancellationToken = default)
    {
        var result = await _rooms.SelectEquipmentAsync(
            id,
            request,
            method,
            cancellationToken);

        return result.ToOkResult();
    }

    [HttpGet("{id:int}/windows")]
    public async Task<ActionResult<List<WindowResponse>>> GetWindows(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _rooms.GetWindowsAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}/walls")]
    public async Task<ActionResult<List<WallResponse>>> GetWalls(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _rooms.GetWallsAsync(id, cancellationToken);
        return result.ToActionResult();
    }
}
