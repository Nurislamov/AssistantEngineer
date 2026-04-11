using AssistantEngineer.Data;
using AssistantEngineer.Models;
using AssistantEngineer.Services;
using Microsoft.AspNetCore.Mvc;

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

    [HttpGet("{id}")]
    public async Task<ActionResult<IEnumerable<Room>>> GetRoom(int id)
    {
        var rooms = await _context.Rooms.FindAsync(id);
        
        if(rooms == null)
            return NotFound();
        
        return Ok(rooms);
    }

    [HttpPost]
    public async Task<ActionResult<Room>> CreateRoom(Room room)
    {
        _context.Rooms.Add(room);
        await  _context.SaveChangesAsync();
        return CreatedAtRoute(nameof(GetRoom), new { id = room.Id }, room);
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

    