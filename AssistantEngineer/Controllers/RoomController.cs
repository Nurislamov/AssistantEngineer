using AssistantEngineer.Contracts;
using AssistantEngineer.Data;
using AssistantEngineer.Models;
using AssistantEngineer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly RoomCalculationService _roomCalculationService;

    public RoomController(AppDbContext contextб , RoomCalculationService roomCalculationService)
    {
        _context = contextб;
        _roomCalculationService = roomCalculationService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoomResponse>>> GetRooms()
    {
        var rooms = await _context.Rooms
            .Select(room => new RoomResponse
            {
                Id = room.Id,
                Name = room.Name,
                AreaM2 = room.AreaM2,
                HeightM = room.HeightM,
                VolumeM3 = room.VolumeM3,
                IndoorTemperatureC = room.IndoorTemperatureC,
                OutdoorTemperatureC = room.OutdoorTemperatureC
            })
            .ToListAsync();
        
        return Ok(rooms);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<IEnumerable<RoomResponse>>> GetRoom(int id)
    {
        var room = await _context.Rooms.FindAsync(id);
        
        if(room == null)
            return NotFound();
        
        return Ok(new RoomResponse
        {
            Id = room.Id,
            Name = room.Name,
            AreaM2 = room.AreaM2,
            HeightM = room.HeightM,
            VolumeM3 = room.VolumeM3,
            IndoorTemperatureC = room.IndoorTemperatureC,
            OutdoorTemperatureC = room.OutdoorTemperatureC
        });
    }

    [HttpPost]
    public async Task<ActionResult<RoomResponse>> CreateRoom(CreateRoomRequest request)
    {
        var room = new Room
        {
            Name = request.Name,
            AreaM2 =  request.AreaM2,
            HeightM =  request.HeightM,
            VolumeM3 = request.AreaM2 * request.HeightM,
            IndoorTemperatureC = request.IndoorTemperatureC,
            OutdoorTemperatureC = request.OutdoorTemperatureC
        };
        _context.Rooms.Add(room);
        await  _context.SaveChangesAsync();

        var response = new RoomResponse
        {
            Id = room.Id,
            Name = room.Name,
            AreaM2 = room.AreaM2,
            HeightM = room.HeightM,
            VolumeM3 = room.VolumeM3,
            IndoorTemperatureC = room.IndoorTemperatureC,
            OutdoorTemperatureC = room.OutdoorTemperatureC
        };
        return CreatedAtAction(nameof(GetRooms), new { id = room.Id }, response);
    }
    
    [HttpGet("{id}/calculate")]
    public async Task<ActionResult<RoomCalculationResult>> CalculateRoom(int id)
    {
        var room = await _context.Rooms.FindAsync(id);

        if (room == null)
            return NotFound();

        var result = _roomCalculationService.Calculate(room);

        return Ok(result);
    }
}

    