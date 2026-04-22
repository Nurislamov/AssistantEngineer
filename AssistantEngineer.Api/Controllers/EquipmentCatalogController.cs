using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Requests;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Application.Facades;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/equipment-catalog")]
public class EquipmentCatalogController : ControllerBase
{
    private readonly IEquipmentCatalogFacade _catalog;

    public EquipmentCatalogController(IEquipmentCatalogFacade catalog)
    {
        _catalog = catalog;
    }

    [HttpPost]
    public async Task<ActionResult<EquipmentCatalogItemResponse>> Create(
        [FromBody] CreateEquipmentCatalogItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _catalog.CreateAsync(request, cancellationToken);
        return result.ToCreatedResult(nameof(GetById), item => item.Id);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EquipmentCatalogItemResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _catalog.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet]
    public async Task<ActionResult<List<EquipmentCatalogItemResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _catalog.GetAllAsync(cancellationToken);
        return result.ToOkResult();
    }
}
