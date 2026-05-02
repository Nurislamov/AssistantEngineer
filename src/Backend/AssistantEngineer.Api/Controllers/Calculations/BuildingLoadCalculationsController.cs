using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using Asp.Versioning;
using AssistantEngineer.Api.Extensions.Results;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Calculations;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/buildings/{buildingId:int}/load-calculations")]
public sealed class BuildingLoadCalculationsController : ControllerBase
{
    private readonly ILoadCalculationsFacade _loadCalculations;

    public BuildingLoadCalculationsController(
        ILoadCalculationsFacade loadCalculations)
    {
        _loadCalculations = loadCalculations;
    }

    [HttpGet("cooling-load")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<BuildingCalculationResult>> CalculateCoolingLoad(
        int buildingId,
        [FromQuery] CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken)
    {
        var result = await _loadCalculations.CalculateBuildingCoolingLoadAsync(
            buildingId,
            method,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpGet("heating-load")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<BuildingHeatingLoadResult>> CalculateHeatingLoad(
        int buildingId,
        [FromQuery] HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken)
    {
        var result = await _loadCalculations.CalculateBuildingHeatingLoadAsync(
            buildingId,
            method,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpGet("energy-balance")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<BuildingEnergyBalanceResult>> CalculateEnergyBalance(
        int buildingId,
        [FromQuery] CoolingLoadCalculationMethodDto coolingMethod,
        [FromQuery] HeatingLoadCalculationMethodDto heatingMethod,
        CancellationToken cancellationToken)
    {
        var result = await _loadCalculations.CalculateBuildingEnergyBalanceAsync(
            buildingId,
            coolingMethod,
            heatingMethod,
            cancellationToken);

        return result.ToActionResult(this);
    }
}

