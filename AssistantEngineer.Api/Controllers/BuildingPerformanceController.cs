using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Services.Buildings;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Analytics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.CoolingSystems;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.HeatingSystems;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Performance;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.Performance;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/building-performance")]
public class BuildingPerformanceController : ControllerBase
{
    private const int DefaultWeatherYear = 2020;

    private readonly BuildingPerformanceService _performance;
    private readonly DomesticHotWaterDemandService _dhw;
    private readonly BuildingCalculationReadinessService _readiness;

    public BuildingPerformanceController(
        BuildingPerformanceService performance,
        DomesticHotWaterDemandService dhw,
        BuildingCalculationReadinessService readiness)
    {
        _performance = performance;
        _dhw = dhw;
        _readiness = readiness;
    }

    [HttpGet("buildings/{buildingId:int}/readiness")]
    public async Task<ActionResult<BuildingCalculationReadinessReport>> CheckReadiness(
        int buildingId,
        [FromQuery] int? weatherYear,
        CancellationToken cancellationToken)
    {
        var effectiveWeatherYear = weatherYear ?? DefaultWeatherYear;
        var result = await _readiness.CheckAsync(buildingId, effectiveWeatherYear, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("dhw")]
    public ActionResult<DomesticHotWaterDemandResult> CalculateDhw(
        [FromBody] DomesticHotWaterDemandRequest request)
    {
        var result = _dhw.Calculate(request);
        return result.ToActionResult();
    }

    [HttpGet("buildings/{buildingId:int}/iso52016/breakdown")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<Iso52016EnergyBalanceBreakdown>> GetIso52016Breakdown(
        int buildingId,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var result = await _performance.GetIso52016BreakdownAsync(buildingId, year, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("buildings/{buildingId:int}/energy-signature")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<EnergySignatureResult>> GetEnergySignature(
        int buildingId,
        [FromQuery] int? year,
        [FromQuery] double? heatingBaseTemperatureC,
        CancellationToken cancellationToken)
    {
        var result = await _performance.GetEnergySignatureAsync(
            buildingId,
            year,
            heatingBaseTemperatureC,
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("buildings/{buildingId:int}/heating-system-energy")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<HeatingSystemEnergyResult>> CalculateHeatingSystemEnergy(
        int buildingId,
        [FromQuery] int? year,
        [FromBody] HeatingSystemEnergyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _performance.CalculateHeatingSystemEnergyAsync(
            buildingId,
            year,
            request,
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("buildings/{buildingId:int}/cooling-system-energy")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<CoolingSystemEnergyResult>> CalculateCoolingSystemEnergy(
        int buildingId,
        [FromQuery] int? year,
        [FromBody] CoolingSystemEnergyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _performance.CalculateCoolingSystemEnergyAsync(
            buildingId,
            year,
            request,
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("buildings/{buildingId:int}/summary")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<BuildingEnergyPerformanceSummary>> CalculateSummary(
        int buildingId,
        [FromQuery] int? year,
        [FromBody] BuildingEnergyPerformanceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _performance.CalculateSummaryAsync(
            buildingId,
            year,
            request,
            cancellationToken);

        return result.ToActionResult();
    }
}
