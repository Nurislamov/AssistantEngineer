using AssistantEngineer.Modules.Calculations.Application.Contracts.Analytics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.CoolingSystems;
using AssistantEngineer.Modules.Calculations.Application.Contracts.HeatingSystems;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Performance;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using Asp.Versioning;
using AssistantEngineer.Api.Extensions.Results;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Analysis;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/buildings/{buildingId:int}/energy-analysis")]
public class BuildingEnergyAnalysisController : ControllerBase
{
    private readonly IBuildingEnergyAnalysisFacade _energyAnalysis;

    public BuildingEnergyAnalysisController(
        IBuildingEnergyAnalysisFacade energyAnalysis)
    {
        _energyAnalysis = energyAnalysis;
    }

    [HttpGet("iso52016/breakdown")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<Iso52016EnergyBalanceBreakdown>> GetIso52016Breakdown(
        int buildingId,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var result = await _energyAnalysis.GetIso52016BreakdownAsync(
            buildingId,
            year,
            cancellationToken);

        return result.ToActionResult(this);
    }


    [HttpPost("iso52016/simulate")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<Iso52016BuildingEnergySimulationApplicationResult>> SimulateIso52016(
        int buildingId,
        [FromBody] Iso52016BuildingEnergySimulationCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _energyAnalysis.SimulateIso52016Async(
            buildingId,
            request,
            cancellationToken);

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
        var result = await _energyAnalysis.GetEnergySignatureAsync(
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
        var result = await _energyAnalysis.CalculateHeatingSystemEnergyAsync(
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
        var result = await _energyAnalysis.CalculateCoolingSystemEnergyAsync(
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
        var result = await _energyAnalysis.CalculateSummaryAsync(
            buildingId,
            year,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }
}

