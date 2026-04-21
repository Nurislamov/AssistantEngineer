using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Services.Rooms;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Mappers;
using AssistantEngineer.Modules.Calculations.Application.Services.Rooms;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Requests;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Application.Services;
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
    private readonly RoomCalculationService _calculation;
    private readonly EquipmentSelectionService _equipmentSelectionService;

    public RoomsController(
        RoomCommandService command,
        RoomQueryService query,
        RoomCalculationService calculation,
        EquipmentSelectionService equipmentSelectionService)
    {
        _command = command;
        _query = query;
        _calculation = calculation;
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
    public async Task<ActionResult<RoomResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _query.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}/calculate")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<RoomCalculationResult>> Calculate(
        int id,
        [FromQuery] CoolingLoadCalculationMethodDto method = CoolingLoadCalculationMethodDto.Simplified,
        CancellationToken cancellationToken = default)
    {
        var result = await _calculation.CalculateAsync(id, method.ToDomain(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}/heating-load")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<RoomHeatingLoadResult>> CalculateHeatingLoad(
        int id,
        [FromQuery] HeatingLoadCalculationMethodDto method = HeatingLoadCalculationMethodDto.En12831,
        CancellationToken cancellationToken = default)
    {
        var result = await _calculation.CalculateHeatingLoadAsync(id, method.ToDomain(), cancellationToken);
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
        [FromQuery] CoolingLoadCalculationMethodDto method = CoolingLoadCalculationMethodDto.Simplified,
        CancellationToken cancellationToken = default)
    {
        var result = await _equipmentSelectionService.SelectForRoomAsync(
            id,
            request,
            method.ToDomain(),
            cancellationToken);

        return result.ToOkResult();
    }

    [HttpGet("{id:int}/windows")]
    public async Task<ActionResult<List<WindowResponse>>> GetWindows(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _query.GetWindowsAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}/walls")]
    public async Task<ActionResult<List<WallResponse>>> GetWalls(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _query.GetWallsAsync(id, cancellationToken);
        return result.ToActionResult();
    }
}