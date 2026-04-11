using AssistantEngineer.Data;
using AssistantEngineer.Models;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomController : ControllerBase
{
    private readonly AppDbContext _context;

    public RoomController(AppDbContext context)
    {
        _context = context;
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
}

    