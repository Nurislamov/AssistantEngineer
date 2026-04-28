using AssistantEngineer.Modules.Calculations.Application.Contracts.Comfort;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using Asp.Versioning;
using AssistantEngineer.Api.Extensions.Results;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Analysis;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/buildings/{buildingId:int}/comfort-analysis")]
public class BuildingComfortAnalysisController : ControllerBase
{
    private readonly IBuildingComfortAnalysisFacade _comfortAnalysis;

    public BuildingComfortAnalysisController(
        IBuildingComfortAnalysisFacade comfortAnalysis)
    {
        _comfortAnalysis = comfortAnalysis;
    }

    [HttpPost("metrics")]
    public async Task<ActionResult<BuildingComfortMetricsResponse>> CalculateMetrics(
        int buildingId,
        [FromQuery] int? year,
        [FromBody] BuildingComfortMetricsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _comfortAnalysis.CalculateMetricsAsync(
            buildingId,
            year,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("zone-metrics")]
    public async Task<ActionResult<BuildingZoneComfortMetricsResponse>> CalculateZoneMetrics(
        int buildingId,
        [FromQuery] int? year,
        [FromBody] BuildingComfortMetricsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _comfortAnalysis.CalculateZoneMetricsAsync(
            buildingId,
            year,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("room-metrics")]
    public async Task<ActionResult<BuildingRoomComfortMetricsResponse>> CalculateRoomMetrics(
        int buildingId,
        [FromQuery] int? year,
        [FromBody] BuildingComfortMetricsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _comfortAnalysis.CalculateRoomMetricsAsync(
            buildingId,
            year,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }
}

