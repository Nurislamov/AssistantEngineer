using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Contracts.Equipment;
using AssistantEngineer.Api.Extensions.Collections;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Requests;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Application.Facades;
using Asp.Versioning;
using AssistantEngineer.Api.Extensions.Http;
using AssistantEngineer.Api.Extensions.Results;
using AssistantEngineer.Api.Querying.Equipment;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Equipment;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/equipment-catalog")]
public class EquipmentCatalogController : ControllerBase
{
    private static readonly IReadOnlyDictionary<string, Func<IEnumerable<EquipmentCatalogItemResponse>, bool, IOrderedEnumerable<EquipmentCatalogItemResponse>>> SortRules =
        new Dictionary<string, Func<IEnumerable<EquipmentCatalogItemResponse>, bool, IOrderedEnumerable<EquipmentCatalogItemResponse>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = (items, descending) =>
                items.SortBy(descending, item => item.Id),

            ["manufacturer"] = (items, descending) =>
                items.SortBy(descending, item => item.Manufacturer)
                    .ThenByStable(descending, item => item.Id),

            ["systemtype"] = (items, descending) =>
                items.SortBy(descending, item => item.SystemType)
                    .ThenByStable(descending, item => item.Id),

            ["unittype"] = (items, descending) =>
                items.SortBy(descending, item => item.UnitType)
                    .ThenByStable(descending, item => item.Id),

            ["modelname"] = (items, descending) =>
                items.SortBy(descending, item => item.ModelName)
                    .ThenByStable(descending, item => item.Id),

            ["nominalcoolingcapacitykw"] = (items, descending) =>
                items.SortBy(descending, item => item.NominalCoolingCapacityKw)
                    .ThenByStable(descending, item => item.Id),

            ["isactive"] = (items, descending) =>
                items.SortBy(descending, item => item.IsActive)
                    .ThenByStable(descending, item => item.Id)
        };

    private readonly IEquipmentFacade _equipment;

    public EquipmentCatalogController(
        IEquipmentFacade equipment)
    {
        _equipment = equipment;
    }

    [HttpPost]
    public async Task<ActionResult<EquipmentCatalogItemResponse>> Create(
        [FromBody] CreateEquipmentCatalogItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _equipment.CreateCatalogItemAsync(
            request,
            cancellationToken);

        return result.ToCreatedAtGetByIdResult(
            this,
            item => item.Id);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EquipmentCatalogItemResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _equipment.GetCatalogItemByIdAsync(
            id,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<EquipmentCatalogItemResponse>>> GetAll(
        [FromQuery] EquipmentCatalogListQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _equipment.GetCatalogItemsAsync(
            cancellationToken);

        return result.ToPagedOkResult(
            this,
            query,
            items => items.ApplyEquipmentCatalogListQuery(query));
    }
}