using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Api;
using AssistantEngineer.Application.Contracts.Calculations;
using AssistantEngineer.Application.Contracts.Common;
using AssistantEngineer.Application.Contracts.Requests;
using AssistantEngineer.Application.Contracts.Responses;
using AssistantEngineer.Application;
using AssistantEngineer.Application.Services.Equipment;
using AssistantEngineer.Application.Services.Rooms;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[Route("api/v1/rooms")]
[Route("api/rooms")]
public class RoomsController : ControllerBase
{
    private readonly RoomCommandService _command;
    private readonly RoomQueryService _query;
    private readonly EquipmentSelectionService _equipmentSelectionService;

    public RoomsController(
        RoomCommandService command,
        RoomQueryService query,
        EquipmentSelectionService equipmentSelectionService)
    {
        _command = command;
        _query = query;
        _equipmentSelectionService = equipmentSelectionService;
    }

    [HttpPost]
    public async Task<ActionResult<RoomResponse>> Create(
        [FromBody] CreateRoomRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _command.CreateAsync(request, cancellationToken);
        return result.ToCreatedResult(nameof(GetById), room => room.Id);
    }

    [HttpGet]
    public async Task<ActionResult<List<RoomResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _query.GetAllAsync(cancellationToken);
        return result.ToOkResult();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _query.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}/calculate")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<RoomCalculationResult>> Calculate(
        int id,
        [FromQuery] CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken)
    {
        var result = await _query.CalculateAsync(id, method.ToDomain(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}/heating-load")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<RoomHeatingLoadResult>> CalculateHeatingLoad(
        int id,
        [FromQuery] HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken)
    {
        var result = await _query.CalculateHeatingLoadAsync(id, method.ToDomain(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:int}/windows")]
    public async Task<ActionResult<WindowResponse>> AddWindow(
        int id,
        [FromBody] CreateWindowRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _command.AddWindowAsync(id, request, cancellationToken);
        return result.ToOkResult();
    }

    [HttpPost("{id:int}/walls")]
    public async Task<ActionResult<WallResponse>> AddWall(
        int id,
        [FromBody] CreateWallRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _command.AddWallAsync(id, request, cancellationToken);
        return result.ToOkResult();
    }

    [HttpPost("{id:int}/select-equipment")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<EquipmentSelectionResult>> SelectEquipment(
        int id,
        [FromBody] EquipmentSelectionRequest request,
        [FromQuery] CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken)
    {
        var result = await _equipmentSelectionService.SelectForRoomAsync(id, request, method.ToDomain(), cancellationToken);
        return result.ToOkResult();
    }

    [HttpGet("{id:int}/windows")]
    public async Task<ActionResult<List<WindowResponse>>> GetWindows(int id, CancellationToken cancellationToken)
    {
        var result = await _query.GetWindowsAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}/walls")]
    public async Task<ActionResult<List<WallResponse>>> GetWalls(int id, CancellationToken cancellationToken)
    {
        var result = await _query.GetWallsAsync(id, cancellationToken);
        return result.ToActionResult();
    }
}
