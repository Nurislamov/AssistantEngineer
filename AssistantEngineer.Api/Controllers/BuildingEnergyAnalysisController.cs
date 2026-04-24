using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Analytics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.CoolingSystems;
using AssistantEngineer.Modules.Calculations.Application.Contracts.HeatingSystems;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Performance;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/buildings/{buildingId:int}/energy-analysis")]
public class BuildingEnergyAnalysisController : ControllerBase
{
    private readonly ICalculationsFacade _calculations;

    public BuildingEnergyAnalysisController(ICalculationsFacade calculations)
    {
        _calculations = calculations;
    }

    [HttpGet("iso52016/breakdown")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<Iso52016EnergyBalanceBreakdown>> GetIso52016Breakdown(
        int buildingId,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var result = await _calculations.GetIso52016BreakdownAsync(buildingId, year, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("energy-signature")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<EnergySignatureResult>> GetEnergySignature(
        int buildingId,
        [FromQuery] int? year,
        [FromQuery] double? heatingBaseTemperatureC,
        CancellationToken cancellationToken)
    {
        var result = await _calculations.GetEnergySignatureAsync(
            buildingId,
            year,
            heatingBaseTemperatureC,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("heating-system-energy")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<HeatingSystemEnergyResult>> CalculateHeatingSystemEnergy(
        int buildingId,
        [FromQuery] int? year,
        [FromBody] HeatingSystemEnergyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _calculations.CalculateHeatingSystemEnergyAsync(
            buildingId,
            year,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("cooling-system-energy")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<CoolingSystemEnergyResult>> CalculateCoolingSystemEnergy(
        int buildingId,
        [FromQuery] int? year,
        [FromBody] CoolingSystemEnergyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _calculations.CalculateCoolingSystemEnergyAsync(
            buildingId,
            year,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("summary")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<BuildingEnergyPerformanceSummary>> CalculateSummary(
        int buildingId,
        [FromQuery] int? year,
        [FromBody] BuildingEnergyPerformanceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _calculations.CalculateSummaryAsync(
            buildingId,
            year,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }
}
