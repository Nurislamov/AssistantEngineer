using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Contracts.Equipment;
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
    private readonly IEquipmentFacade _equipment;

    public EquipmentCatalogController(IEquipmentFacade equipment)
    {
        _equipment = equipment;
    }

    [HttpPost]
    public async Task<ActionResult<EquipmentCatalogItemResponse>> Create(
        [FromBody] CreateEquipmentCatalogItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _equipment.CreateCatalogItemAsync(request, cancellationToken);
        return result.ToCreatedResult(this, nameof(GetById), item => item.Id);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EquipmentCatalogItemResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _equipment.GetCatalogItemByIdAsync(id, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<EquipmentCatalogItemResponse>>> GetAll(
        [FromQuery] EquipmentCatalogListQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _equipment.GetCatalogItemsAsync(cancellationToken);
        if (result.IsFailure)
            return ApiProblemDetailsFactory.CreateResult(HttpContext, result);

        IEnumerable<EquipmentCatalogItemResponse> items = result.Value;

        if (!string.IsNullOrWhiteSpace(query.Manufacturer))
            items = items.Where(item => string.Equals(item.Manufacturer, query.Manufacturer.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(query.SystemType))
            items = items.Where(item => string.Equals(item.SystemType, query.SystemType.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(query.UnitType))
            items = items.Where(item => string.Equals(item.UnitType, query.UnitType.Trim(), StringComparison.OrdinalIgnoreCase));

        if (query.IsActive.HasValue)
            items = items.Where(item => item.IsActive == query.IsActive.Value);

        items = SortCatalogItems(
            items.ApplySearch(
                query.Search,
                item => item.Manufacturer,
                item => item.SystemType,
                item => item.UnitType,
                item => item.ModelName),
            query);

        return Ok(items.ToPagedResponse(query));
    }

    private static IEnumerable<EquipmentCatalogItemResponse> SortCatalogItems(
        IEnumerable<EquipmentCatalogItemResponse> source,
        CollectionQueryParameters query) =>
        (query.SortBy ?? "id").ToLowerInvariant() switch
        {
            "manufacturer" => query.SortDescending ? source.OrderByDescending(item => item.Manufacturer).ThenByDescending(item => item.Id) : source.OrderBy(item => item.Manufacturer).ThenBy(item => item.Id),
            "systemtype" => query.SortDescending ? source.OrderByDescending(item => item.SystemType).ThenByDescending(item => item.Id) : source.OrderBy(item => item.SystemType).ThenBy(item => item.Id),
            "unittype" => query.SortDescending ? source.OrderByDescending(item => item.UnitType).ThenByDescending(item => item.Id) : source.OrderBy(item => item.UnitType).ThenBy(item => item.Id),
            "modelname" => query.SortDescending ? source.OrderByDescending(item => item.ModelName).ThenByDescending(item => item.Id) : source.OrderBy(item => item.ModelName).ThenBy(item => item.Id),
            "nominalcoolingcapacitykw" => query.SortDescending ? source.OrderByDescending(item => item.NominalCoolingCapacityKw).ThenByDescending(item => item.Id) : source.OrderBy(item => item.NominalCoolingCapacityKw).ThenBy(item => item.Id),
            "isactive" => query.SortDescending ? source.OrderByDescending(item => item.IsActive).ThenByDescending(item => item.Id) : source.OrderBy(item => item.IsActive).ThenBy(item => item.Id),
            _ => query.SortDescending ? source.OrderByDescending(item => item.Id) : source.OrderBy(item => item.Id)
        };
}
