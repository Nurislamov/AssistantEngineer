using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Services.Buildings;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Mappers;
using AssistantEngineer.Modules.Calculations.Application.Services.Buildings;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[Route("api/v1/buildings")]
[Route("api/buildings")]
public class BuildingsController : ControllerBase
{
    private readonly BuildingCommandService _command;
    private readonly BuildingQueryService _query;
    private readonly BuildingCoolingLoadService _coolingLoadService;
    private readonly BuildingHeatingLoadService _heatingLoadService;
    private readonly BuildingEnergyBalanceService _energyBalanceService;

    public BuildingsController(
        BuildingCommandService command,
        BuildingQueryService query,
        BuildingCoolingLoadService coolingLoadService,
        BuildingHeatingLoadService heatingLoadService,
        BuildingEnergyBalanceService energyBalanceService)
    {
        _command = command;
        _query = query;
        _coolingLoadService = coolingLoadService;
        _heatingLoadService = heatingLoadService;
        _energyBalanceService = energyBalanceService;
    }

    [HttpPost("{projectId:int}")]
    public async Task<ActionResult<BuildingResponse>> Create(
        int projectId,
        [FromBody] CreateBuildingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _command.CreateAsync(projectId, request, cancellationToken);
        return result.ToCreatedResult(nameof(GetById), building => building.Id);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BuildingResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _query.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("project/{projectId:int}")]
    public async Task<ActionResult<List<BuildingResponse>>> GetByProject(
        int projectId,
        CancellationToken cancellationToken)
    {
        var result = await _query.GetByProjectIdAsync(projectId, cancellationToken);
        return result.ToOkResult();
    }

    [HttpGet("{id:int}/calculate")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<BuildingCalculationResult>> Calculate(
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
