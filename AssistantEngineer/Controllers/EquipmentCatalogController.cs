using AssistantEngineer.Contracts.Requests;
using AssistantEngineer.Contracts.Responses;
using AssistantEngineer.Data;
using AssistantEngineer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Controllers;

[ApiController]
[Route("api/equipment-catalog-items")]
public class EquipmentCatalogController : ControllerBase
{
    private readonly AppDbContext _context;

    public EquipmentCatalogController(AppDbContext context)
    {
        _context = context;
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<EquipmentCatalogItemResponse>> GetById(int id)
    {
        var item = await _context.EquipmentCatalogItems
            .Where(x => x.Id == id)
            .Select(x => new EquipmentCatalogItemResponse
            {
                Id = x.Id,
                Manufacturer = x.Manufacturer,
                SystemType = x.SystemType,
                UnitType = x.UnitType,
                ModelName = x.ModelName,
                NominalCoolingCapacityKw = x.NominalCoolingCapacityKw,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EquipmentCatalogItemResponse>>> GetAll()
    {
        var items = await _context.EquipmentCatalogItems
            .OrderBy(x => x.SystemType)
            .ThenBy(x => x.UnitType)
            .ThenBy(x => x.NominalCoolingCapacityKw)
            .Select(x => new EquipmentCatalogItemResponse
            {
                Id = x.Id,
                Manufacturer = x.Manufacturer,
                SystemType = x.SystemType,
                UnitType = x.UnitType,
                ModelName = x.ModelName,
                NominalCoolingCapacityKw = x.NominalCoolingCapacityKw,
                IsActive = x.IsActive
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<EquipmentCatalogItemResponse>> Create(
        CreateEquipmentCatalogItemRequest request)
    {
        var item = new EquipmentCatalogItem
        {
            Manufacturer = request.Manufacturer,
            SystemType = request.SystemType,
            UnitType = request.UnitType,
            ModelName = request.ModelName,
            NominalCoolingCapacityKw = request.NominalCoolingCapacityKw,
            IsActive = request.IsActive
        };

        _context.EquipmentCatalogItems.Add(item);
        await _context.SaveChangesAsync();

        var response = new EquipmentCatalogItemResponse
        {
            Id = item.Id,
            Manufacturer = item.Manufacturer,
            SystemType = item.SystemType,
            UnitType = item.UnitType,
            ModelName = item.ModelName,
            NominalCoolingCapacityKw = item.NominalCoolingCapacityKw,
            IsActive = item.IsActive
        };

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, response);
    }
}
