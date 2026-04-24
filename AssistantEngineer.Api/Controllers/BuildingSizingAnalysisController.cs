using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/buildings/{buildingId:int}/sizing-analysis")]
public class BuildingSizingAnalysisController : ControllerBase
{
    private readonly ICalculationsFacade _calculations;

    public BuildingSizingAnalysisController(ICalculationsFacade calculations)
    {
        _calculations = calculations;
    }

    [HttpPost("peak-loads")]
    public async Task<ActionResult<BuildingPeakSizingResponse>> CalculatePeakLoads(
        int buildingId,
        [FromQuery] int? year,
        [FromBody] PeakSizingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _calculations.CalculatePeakLoadsAsync(
            buildingId,
            year,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("reference-design-day")]
    public async Task<ActionResult<BuildingReferenceDesignDayResponse>> CalculateReferenceDesignDay(
        int buildingId,
        [FromQuery] int? year,
        [FromBody] ReferenceDesignDayRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _calculations.CalculateReferenceDesignDayAsync(
            buildingId,
            year,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("synthetic-design-day")]
    public async Task<ActionResult<BuildingSyntheticDesignDayResponse>> CalculateSyntheticDesignDay(
        int buildingId,
        [FromBody] SyntheticDesignDayRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _calculations.CalculateSyntheticDesignDayAsync(
            buildingId,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("autosizing")]
    public async Task<ActionResult<BuildingAutosizingResponse>> CalculateAutosizing(
        int buildingId,
        [FromQuery] int? year,
        [FromBody] AutosizingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _calculations.CalculateAutosizingAsync(
            buildingId,
            year,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("catalog-autosizing")]
    public async Task<ActionResult<BuildingCatalogAutosizingResponse>> CalculateCatalogAutosizing(
        int buildingId,
        [FromQuery] int? year,
        [FromBody] CatalogAutosizingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _calculations.CalculateCatalogAutosizingAsync(
            buildingId,
            year,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("equipment-recommendations")]
    public async Task<ActionResult<BuildingEquipmentRecommendationResponse>> CalculateEquipmentRecommendations(
        int buildingId,
        [FromQuery] int? year,
        [FromBody] EquipmentRecommendationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _calculations.CalculateEquipmentRecommendationsAsync(
            buildingId,
            year,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("equipment-recommendations/compare")]
    public async Task<ActionResult<BuildingEquipmentRecommendationComparisonResponse>> CompareEquipmentRecommendations(
        int buildingId,
        [FromQuery] int? year,
        [FromBody] EquipmentRecommendationComparisonRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _calculations.CompareEquipmentRecommendationsAsync(
            buildingId,
            year,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }
}
