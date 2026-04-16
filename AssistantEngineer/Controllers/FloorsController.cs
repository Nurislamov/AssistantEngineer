using AssistantEngineer.Application.Services.Floors;
using AssistantEngineer.Contracts.Requests;
using AssistantEngineer.Contracts.Responses;
using AssistantEngineer.Domain.Contracts.Calculations;
using AssistantEngineer.Services.Calculations;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FloorsController : ControllerBase
{
    private readonly FloorApplicationService _floors;
    private readonly AggregateCalculationService _aggregateCalculationService;

    public FloorsController(
        FloorApplicationService floors,
        AggregateCalculationService aggregateCalculationService)
    {
        _floors = floors;
        _aggregateCalculationService = aggregateCalculationService;
    }

    [HttpPost("{buildingId}")]
    public async Task<ActionResult<FloorResponse>> CreateFloor(int buildingId, CreateFloorRequest request)
    {
        var response = await _floors.CreateAsync(buildingId, request);

        if (response == null)
            return NotFound($"Building with id {buildingId} not found.");

        return CreatedAtAction(nameof(GetFloor), new { id = response.Id }, response);
    }

    [HttpGet("{buildingId}")]
    public async Task<ActionResult<IEnumerable<FloorResponse>>> GetFloors(int buildingId)
    {
        return Ok(await _floors.GetByBuildingIdAsync(buildingId));
    }

    [HttpGet("by-id/{id}")]
    public async Task<ActionResult<FloorResponse>> GetFloor(int id)
    {
        var floor = await _floors.GetByIdAsync(id);

        if (floor == null)
            return NotFound();

        return Ok(floor);
    }

    [HttpGet("{floorId}/calculate")]
    public async Task<ActionResult<FloorCalculationResult>> CalculateFloor(int floorId)
    {
        var floorCalculationResult = await _aggregateCalculationService.CalculateFloorAsync(floorId);

        if (floorCalculationResult == null)
            return NotFound();

        return Ok(floorCalculationResult);
    }
}
