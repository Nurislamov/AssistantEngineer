using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Comfort;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/buildings/{buildingId:int}/comfort-analysis")]
public class BuildingComfortAnalysisController : ControllerBase
{
    private readonly IBuildingComfortAnalysisFacade _comfort;

    public BuildingComfortAnalysisController(IBuildingComfortAnalysisFacade comfort)
    {
        _comfort = comfort;
    }

    [HttpPost("metrics")]
    public async Task<ActionResult<BuildingComfortMetricsResponse>> CalculateMetrics(
        int buildingId,
        [FromQuery] int? year,
        [FromBody] BuildingComfortMetricsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _comfort.CalculateMetricsAsync(
            buildingId,
            year,
            request,
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("zone-metrics")]
    public async Task<ActionResult<BuildingZoneComfortMetricsResponse>> CalculateZoneMetrics(
        int buildingId,
        [FromQuery] int? year,
        [FromBody] BuildingComfortMetricsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _comfort.CalculateZoneMetricsAsync(
            buildingId,
            year,
            request,
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("room-metrics")]
    public async Task<ActionResult<BuildingRoomComfortMetricsResponse>> CalculateRoomMetrics(
        int buildingId,
        [FromQuery] int? year,
        [FromBody] BuildingComfortMetricsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _comfort.CalculateRoomMetricsAsync(
            buildingId,
            year,
            request,
            cancellationToken);

        return result.ToActionResult();
    }
}