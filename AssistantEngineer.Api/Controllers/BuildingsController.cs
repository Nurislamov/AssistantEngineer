using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Services.Buildings;
using AssistantEngineer.Modules.Calculations.Application.Mappers;
using AssistantEngineer.Modules.Calculations.Application.Services.Buildings;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/buildings")]
public class BuildingsController : ControllerBase
{
    private readonly BuildingCommandService _command;
    private readonly BuildingQueryService _query;
    private readonly BuildingArchetypeService _archetypes;
    private readonly BuildingCoolingLoadService _coolingLoadService;
    private readonly BuildingHeatingLoadService _heatingLoadService;
    private readonly BuildingEnergyBalanceService _energyBalanceService;

    public BuildingsController(
        BuildingCommandService command,
        BuildingQueryService query,
        BuildingArchetypeService archetypes,
        BuildingCoolingLoadService coolingLoadService,
        BuildingHeatingLoadService heatingLoadService,
        BuildingEnergyBalanceService energyBalanceService)
    {
        _command = command;
        _query = query;
        _archetypes = archetypes;
        _coolingLoadService = coolingLoadService;
        _heatingLoadService = heatingLoadService;
        _energyBalanceService = energyBalanceService;
    }

    [HttpPost("~/api/v{version:apiVersion}/projects/{projectId:int}/buildings")]
    public async Task<ActionResult<BuildingResponse>> Create(
        int projectId,
        [FromBody] CreateBuildingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _command.CreateAsync(projectId, request, cancellationToken);
        return result.ToCreatedResult(nameof(GetById), building => building.Id);
    }

    [HttpPost("~/api/v{version:apiVersion}/projects/{projectId:int}/buildings/from-archetype")]
    public async Task<ActionResult<BuildingResponse>> CreateFromArchetype(
        int projectId,
        [FromBody] CreateBuildingFromArchetypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _archetypes.CreateFromArchetypeAsync(projectId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BuildingResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _query.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("~/api/v{version:apiVersion}/projects/{projectId:int}/buildings")]
    public async Task<ActionResult<List<BuildingResponse>>> GetByProject(
        int projectId,
        CancellationToken cancellationToken)
    {
        var result = await _query.GetByProjectIdAsync(projectId, cancellationToken);
        return result.ToOkResult();
    }

    [HttpGet("{id:int}/cooling-load")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<BuildingCalculationResult>> CalculateCoolingLoad(
        int id,
        [FromQuery] CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken)
    {
        var result = await _coolingLoadService.CalculateAsync(id, method.ToDomain(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}/heating-load")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<BuildingHeatingLoadResult>> CalculateHeatingLoad(
        int id,
        [FromQuery] HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken)
    {
        var result = await _heatingLoadService.CalculateAsync(id, method.ToDomain(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}/energy-balance")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<BuildingEnergyBalanceResult>> CalculateEnergyBalance(
        int id,
        [FromQuery] CoolingLoadCalculationMethodDto coolingMethod,
        [FromQuery] HeatingLoadCalculationMethodDto heatingMethod,
        CancellationToken cancellationToken)
    {
        var result = await _energyBalanceService.CalculateAsync(
            id,
            coolingMethod.ToDomain(),
            heatingMethod.ToDomain(),
            cancellationToken);
        return result.ToActionResult();
    }
}
