using AssistantEngineer.Application.Services.Equipment;
using AssistantEngineer.Contracts.Requests;
using AssistantEngineer.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Controllers;

[ApiController]
[Route("api/equipment-catalog-items")]
public class EquipmentCatalogController : ControllerBase
{
    private readonly CoolingEquipmentCatalogService _catalog;

    public EquipmentCatalogController(CoolingEquipmentCatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EquipmentCatalogItemResponse>> GetById(int id)
    {
        var item = await _catalog.GetByIdAsync(id);

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EquipmentCatalogItemResponse>>> GetAll()
    {
        return Ok(await _catalog.GetAllAsync());
    }

    [HttpPost]
    public async Task<ActionResult<EquipmentCatalogItemResponse>> Create(
        CreateEquipmentCatalogItemRequest request)
    {
        var response = await _catalog.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }
}
