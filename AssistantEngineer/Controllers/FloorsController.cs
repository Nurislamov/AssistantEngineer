using AssistantEngineer.Contracts;
using AssistantEngineer.Contracts.Results;
using AssistantEngineer.Data;
using AssistantEngineer.Models;
using AssistantEngineer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FloorsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly StructureCalculationService _structureCalculationService;

    public FloorsController(
        AppDbContext context,
        StructureCalculationService structureCalculationService)
    {
        _context = context;
        _structureCalculationService = structureCalculationService;
    }

    [HttpPost("{buildingId}")]
    public async Task<ActionResult<Floor>> CreateFloor(int buildingId, CreateFloorRequest request)
    {
        var buildingExists = await _context.Buildings.AnyAsync(b => b.Id == buildingId);
        if (!buildingExists)
            return NotFound($"Building with id {buildingId} not found.");

        var floor = new Floor
        {
            Name = request.Name,
            BuildingId = buildingId
        };

        _context.Floors.Add(floor);
        await _context.SaveChangesAsync();

        return Ok(floor);
    }

    [HttpGet("{buildingId}")]
    public async Task<ActionResult<IEnumerable<Floor>>> GetFloors(int buildingId)
    {
        var floors = await _context.Floors
            .Where(f => f.BuildingId == buildingId)
            .ToListAsync();

        return Ok(floors);
    }

    [HttpGet("{floorId}/calculate")]
    public async Task<ActionResult<FloorCalculationResult>> CalculateFloor(int floorId)
    {
        var result = await _structureCalculationService.CalculateFloorAsync(floorId);

        if (result == null)
            return NotFound();

        return Ok(result);
    }
}