using AssistantEngineer.Api.Contracts.Buildings;
using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/buildings")]
public class BuildingsController : ControllerBase
{
    private readonly IBuildingsFacade _buildings;
    private readonly ICalculationsFacade _calculations;

    public BuildingsController(
        IBuildingsFacade buildings,
        ICalculationsFacade calculations)
    {
        _buildings = buildings;
        _calculations = calculations;
    }

    [HttpPost("~/api/v{version:apiVersion}/projects/{projectId:int}/buildings")]
    public async Task<ActionResult<BuildingResponse>> Create(
        int projectId,
        [FromBody] CreateBuildingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.CreateBuildingAsync(projectId, request, cancellationToken);
        return result.ToCreatedResult(this, nameof(GetById), building => building.Id);
    }

    [HttpPost("~/api/v{version:apiVersion}/projects/{projectId:int}/buildings/from-archetype")]
    public async Task<ActionResult<BuildingResponse>> CreateFromArchetype(
        int projectId,
        [FromBody] CreateBuildingFromArchetypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.CreateBuildingFromArchetypeAsync(projectId, request, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BuildingResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _buildings.GetBuildingByIdAsync(id, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("~/api/v{version:apiVersion}/projects/{projectId:int}/buildings")]
    public async Task<ActionResult<PagedResponse<BuildingResponse>>> GetByProject(
        int projectId,
        [FromQuery] BuildingListQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetBuildingsByProjectAsync(projectId, cancellationToken);
        if (result.IsFailure)
            return ApiProblemDetailsFactory.CreateResult(HttpContext, result);

        IEnumerable<BuildingResponse> items = result.Value;

        if (query.ClimateZoneId.HasValue)
            items = items.Where(building => building.ClimateZoneId == query.ClimateZoneId.Value);

        items = SortBuildings(
            items.ApplySearch(query.Search, building => building.Name, building => building.ClimateZoneName),
            query);

        return Ok(items.ToPagedResponse(query));
    }

    [HttpGet("{id:int}/cooling-load")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<BuildingCalculationResult>> CalculateCoolingLoad(
        int id,
        [FromQuery] CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken)
    {
        var result = await _calculations.CalculateBuildingCoolingLoadAsync(id, method, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{id:int}/heating-load")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<BuildingHeatingLoadResult>> CalculateHeatingLoad(
        int id,
        [FromQuery] HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken)
    {
        var result = await _calculations.CalculateBuildingHeatingLoadAsync(id, method, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("{id:int}/energy-balance")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<BuildingEnergyBalanceResult>> CalculateEnergyBalance(
        int id,
        [FromQuery] CoolingLoadCalculationMethodDto coolingMethod,
        [FromQuery] HeatingLoadCalculationMethodDto heatingMethod,
        CancellationToken cancellationToken)
    {
        var result = await _calculations.CalculateBuildingEnergyBalanceAsync(
            id,
            coolingMethod,
            heatingMethod,
            cancellationToken);
        return result.ToActionResult(this);
    }

    private static IEnumerable<BuildingResponse> SortBuildings(
        IEnumerable<BuildingResponse> source,
        CollectionQueryParameters query) =>
        (query.SortBy ?? "id").ToLowerInvariant() switch
        {
            "name" => query.SortDescending
                ? source.OrderByDescending(building => building.Name).ThenByDescending(building => building.Id)
                : source.OrderBy(building => building.Name).ThenBy(building => building.Id),
            "climatezonename" => query.SortDescending
                ? source.OrderByDescending(building => building.ClimateZoneName).ThenByDescending(building => building.Id)
                : source.OrderBy(building => building.ClimateZoneName).ThenBy(building => building.Id),
            _ => query.SortDescending
                ? source.OrderByDescending(building => building.Id)
                : source.OrderBy(building => building.Id)
        };
}
