using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Requests;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[Route("api/v1/equipment-catalog")]
[Route("api/equipment-catalog")]
public class EquipmentCatalogController : ControllerBase
{
    private readonly CoolingEquipmentCatalogCommandService _command;
    private readonly CoolingEquipmentCatalogQueryService _query;

    public EquipmentCatalogController(
        CoolingEquipmentCatalogCommandService command,
        CoolingEquipmentCatalogQueryService query)
    {
        _command = command;
        _query = query;
    }

    [HttpPost]
    public async Task<ActionResult<EquipmentCatalogItemResponse>> Create(
        [FromBody] CreateEquipmentCatalogItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _command.CreateAsync(request, cancellationToken);
        return result.ToCreatedResult(nameof(GetById), item => item.Id);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EquipmentCatalogItemResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _query.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet]
    public async Task<ActionResult<List<EquipmentCatalogItemResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _query.GetAllAsync(cancellationToken);
        return result.ToOkResult();
    }
}
