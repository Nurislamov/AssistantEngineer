using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Services.Buildings;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Analytics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.CoolingSystems;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.HeatingSystems;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Performance;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Performance;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[Route("api/v1/building-performance")]
[Route("api/building-performance")]
public class BuildingPerformanceController : ControllerBase
{
    private readonly BuildingPerformanceService _performance;
    private readonly DomesticHotWaterDemandService _dhw;
    private readonly BuildingCalculationReadinessService _readiness;
    private readonly BuildingArchetypeService _archetypes;

    public BuildingPerformanceController(
        BuildingPerformanceService performance,
        DomesticHotWaterDemandService dhw,
        BuildingCalculationReadinessService readiness,
        BuildingArchetypeService archetypes)
    {
        _performance = performance;
        _dhw = dhw;
        _readiness = readiness;
        _archetypes = archetypes;
    }

    [HttpGet("buildings/{buildingId:int}/readiness")]
    public async Task<ActionResult<BuildingCalculationReadinessReport>> CheckReadiness(
        int buildingId,
        [FromQuery] int weatherYear,
        CancellationToken cancellationToken)
    {
        var result = await _readiness.CheckAsync(buildingId, weatherYear == 0 ? 2020 : weatherYear, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("archetypes")]
    public ActionResult<IReadOnlyList<BuildingArchetypeSummary>> ListArchetypes() =>
        Ok(_archetypes.ListArchetypes());

    [HttpPost("projects/{projectId:int}/buildings/from-archetype")]
    public async Task<ActionResult<BuildingResponse>> CreateFromArchetype(
        int projectId,
        [FromBody] CreateBuildingFromArchetypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _archetypes.CreateFromArchetypeAsync(projectId, request, cancellationToken);
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
        [FromQuery] int year,
        CancellationToken cancellationToken)
    {
        var result = await _performance.GetIso52016BreakdownAsync(buildingId, year, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("buildings/{buildingId:int}/energy-signature")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<EnergySignatureResult>> GetEnergySignature(
        int buildingId,
        [FromQuery] int year,
        [FromQuery] double heatingBaseTemperatureC,
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
        [FromQuery] int year,
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
        [FromQuery] int year,
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
        [FromQuery] int year,
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