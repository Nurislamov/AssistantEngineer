using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Services.Buildings;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Services.Analytics;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingSystems;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingSystems;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Performance;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[Route("api/v1/building-performance")]
[Route("api/building-performance")]
public class BuildingPerformanceController : ControllerBase
{
    private readonly IBuildingRepository _buildings;
    private readonly ICalculationPreferencesRepository _preferences;
    private readonly Iso52016HourlySteadyStateCalculator _iso52016;
    private readonly EnergySignatureService _energySignature;
    private readonly HeatingSystemEnergyService _heatingSystemEnergy;
    private readonly CoolingSystemEnergyService _coolingSystemEnergy;
    private readonly BuildingEnergyPerformanceSummaryService _performanceSummary;
    private readonly DomesticHotWaterDemandService _dhw;
    private readonly BuildingCalculationReadinessService _readiness;
    private readonly BuildingArchetypeService _archetypes;

    public BuildingPerformanceController(
        IBuildingRepository buildings,
        ICalculationPreferencesRepository preferences,
        Iso52016HourlySteadyStateCalculator iso52016,
        EnergySignatureService energySignature,
        HeatingSystemEnergyService heatingSystemEnergy,
        CoolingSystemEnergyService coolingSystemEnergy,
        BuildingEnergyPerformanceSummaryService performanceSummary,
        DomesticHotWaterDemandService dhw,
        BuildingCalculationReadinessService readiness,
        BuildingArchetypeService archetypes)
    {
        _buildings = buildings;
        _preferences = preferences;
        _iso52016 = iso52016;
        _energySignature = energySignature;
        _heatingSystemEnergy = heatingSystemEnergy;
        _coolingSystemEnergy = coolingSystemEnergy;
        _performanceSummary = performanceSummary;
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
        var energyNeed = await CalculateEnergyNeedAsync(buildingId, year, cancellationToken);
        if (energyNeed.IsFailure)
            return energyNeed.ToFailureResult();

        return Ok(energyNeed.Value.Breakdown);
    }

    [HttpGet("buildings/{buildingId:int}/energy-signature")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<EnergySignatureResult>> GetEnergySignature(
        int buildingId,
        [FromQuery] int year,
        [FromQuery] double heatingBaseTemperatureC,
        CancellationToken cancellationToken)
    {
        var energyNeed = await CalculateEnergyNeedAsync(buildingId, year, cancellationToken);
        if (energyNeed.IsFailure)
            return energyNeed.ToFailureResult();

        var result = _energySignature.Calculate(
            energyNeed.Value,
            heatingBaseTemperatureC == 0 ? 18 : heatingBaseTemperatureC);
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
        var energyNeed = await CalculateEnergyNeedAsync(buildingId, year, cancellationToken);
        if (energyNeed.IsFailure)
            return energyNeed.ToFailureResult();

        var result = _heatingSystemEnergy.Calculate(energyNeed.Value, request);
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
        var energyNeed = await CalculateEnergyNeedAsync(buildingId, year, cancellationToken);
        if (energyNeed.IsFailure)
            return energyNeed.ToFailureResult();

        var result = _coolingSystemEnergy.Calculate(energyNeed.Value, request);
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
        var context = await CalculateEnergyNeedContextAsync(buildingId, year, cancellationToken);
        if (context.IsFailure)
            return context.ToFailureResult();

        var result = _performanceSummary.Calculate(
            context.Value.Building,
            context.Value.EnergyNeed,
            request);
        return result.ToActionResult();
    }

    private async Task<Result<Iso52016AnnualEnergyNeedResult>> CalculateEnergyNeedAsync(
        int buildingId,
        int year,
        CancellationToken cancellationToken)
    {
        var readiness = await EnsureCalculationReadyAsync(buildingId, year, cancellationToken);
        if (readiness.IsFailure)
            return Result<Iso52016AnnualEnergyNeedResult>.Failure(readiness);

        var building = await _buildings.GetForCalculationAsync(buildingId, cancellationToken);
        if (building is null)
            return Result<Iso52016AnnualEnergyNeedResult>.NotFound($"Building with id {buildingId} not found.");

        var preferences = await _preferences.GetByProjectIdAsync(building.ProjectId, cancellationToken);
        var result = await _iso52016.CalculateBuildingEnergyNeedsAsync(
            building,
            preferences,
            year == 0 ? null : year,
            cancellationToken);

        return result is null
            ? Result<Iso52016AnnualEnergyNeedResult>.Validation("Complete annual climate data is required for ISO 52016 analysis.")
            : Result<Iso52016AnnualEnergyNeedResult>.Success(result);
    }

    private async Task<Result<BuildingEnergyNeedContext>> CalculateEnergyNeedContextAsync(
        int buildingId,
        int year,
        CancellationToken cancellationToken)
    {
        var readiness = await EnsureCalculationReadyAsync(buildingId, year, cancellationToken);
        if (readiness.IsFailure)
            return Result<BuildingEnergyNeedContext>.Failure(readiness);

        var building = await _buildings.GetForCalculationAsync(buildingId, cancellationToken);
        if (building is null)
            return Result<BuildingEnergyNeedContext>.NotFound($"Building with id {buildingId} not found.");

        var preferences = await _preferences.GetByProjectIdAsync(building.ProjectId, cancellationToken);
        var result = await _iso52016.CalculateBuildingEnergyNeedsAsync(
            building,
            preferences,
            year == 0 ? null : year,
            cancellationToken);

        return result is null
            ? Result<BuildingEnergyNeedContext>.Validation("Complete annual climate data is required for ISO 52016 analysis.")
            : Result<BuildingEnergyNeedContext>.Success(new BuildingEnergyNeedContext(building, result));
    }

    private async Task<Result> EnsureCalculationReadyAsync(
        int buildingId,
        int year,
        CancellationToken cancellationToken)
    {
        var report = await _readiness.CheckAsync(
            buildingId,
            year == 0 ? 2020 : year,
            cancellationToken);
        if (report.IsFailure)
            return Result.Failure(report.Error, report.ErrorType);

        var errors = report.Value.Issues
            .Where(issue => issue.Severity == BuildingCalculationReadinessSeverity.Error)
            .ToArray();
        if (errors.Length == 0)
            return Result.Success();

        return Result.Validation(
            "Building is not ready for calculation: " +
            string.Join("; ", errors.Select(issue => $"{issue.Path}: {issue.Message}")));
    }

    private sealed record BuildingEnergyNeedContext(
        Building Building,
        Iso52016AnnualEnergyNeedResult EnergyNeed);
}
