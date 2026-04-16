using AssistantEngineer.Application.Services.Rooms;
using AssistantEngineer.Contracts.Requests;
using AssistantEngineer.Contracts.Responses;
using AssistantEngineer.Domain.Contracts.Calculations;
using AssistantEngineer.Services.Calculations;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Controllers;

[ApiController]
[Route("api/rooms")]
public class RoomsController : ControllerBase
{
    private readonly RoomApplicationService _rooms;
    private readonly EquipmentSelectionService _equipmentSelectionService;

    public RoomsController(
        RoomApplicationService rooms,
        EquipmentSelectionService equipmentSelectionService)
    {
        _rooms = rooms;
        _equipmentSelectionService = equipmentSelectionService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoomResponse>>> GetRooms()
    {
        return Ok(await _rooms.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RoomResponse>> GetRoom(int id)
    {
        var room = await _rooms.GetByIdAsync(id);

        if (room == null)
            return NotFound();

        return Ok(room);
    }

    [HttpPost]
    public async Task<ActionResult<RoomResponse>> CreateRoom(CreateRoomRequest request)
    {
        var response = await _rooms.CreateAsync(request);

        if (response == null)
            return NotFound($"Floor with id {request.FloorId} not found.");

        return CreatedAtAction(nameof(GetRoom), new { id = response.Id }, response);
    }

    [HttpGet("{id}/calculate")]
    public async Task<ActionResult<RoomCalculationResult>> CalculateRoom(int id)
    {
        var result = await _rooms.CalculateAsync(id);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("{roomId}/windows")]
    public async Task<ActionResult<IEnumerable<WindowResponse>>> GetWindows(int roomId)
    {
        var windows = await _rooms.GetWindowsAsync(roomId);

        if (windows == null)
            return NotFound($"Room with id {roomId} not found.");

        return Ok(windows);
    }

    [HttpPost("{roomId}/windows")]
    public async Task<ActionResult<WindowResponse>> AddWindow(int roomId, CreateWindowRequest request)
    {
        var response = await _rooms.AddWindowAsync(roomId, request);

        if (response == null)
            return NotFound($"Room with id {roomId} not found.");

        return Ok(response);
    }

    [HttpPost("{roomId}/walls")]
    public async Task<ActionResult<WallResponse>> AddWall(int roomId, CreateWallRequest request)
    {
        var response = await _rooms.AddWallAsync(roomId, request);

        if (response == null)
            return NotFound($"Room with id {roomId} not found.");

        return Ok(response);
    }

    [HttpGet("{roomId}/walls")]
    public async Task<ActionResult<IEnumerable<WallResponse>>> GetWalls(int roomId)
    {
        var walls = await _rooms.GetWallsAsync(roomId);

        if (walls == null)
            return NotFound($"Room with id {roomId} not found.");

        return Ok(walls);
    }

    [HttpPost("{roomId}/select-equipment")]
    public async Task<ActionResult<EquipmentSelectionResult>> SelectEquipment(
        int roomId,
        EquipmentSelectionRequest request)
    {
        var room = await _rooms.GetByIdAsync(roomId);
        if (room == null)
            return NotFound($"Room with id {roomId} not found.");

        var result = await _equipmentSelectionService.SelectForRoomAsync(
            roomId,
            request.SystemType,
            request.UnitType);

        if (result == null)
            return NotFound("No suitable equipment found for the specified room and filters.");

        return Ok(result);
    }
}
