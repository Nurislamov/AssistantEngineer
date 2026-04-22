using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Services.Buildings;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/buildings/{buildingId:int}/readiness")]
public class BuildingReadinessController : ControllerBase
{
    private const int DefaultWeatherYear = 2020;

    private readonly BuildingCalculationReadinessService _readiness;

    public BuildingReadinessController(BuildingCalculationReadinessService readiness)
    {
        _readiness = readiness;
    }

    [HttpGet]
    public async Task<ActionResult<BuildingCalculationReadinessReport>> Check(
        int buildingId,
        [FromQuery] int? weatherYear,
        CancellationToken cancellationToken)
    {
        var result = await _readiness.CheckAsync(
            buildingId,
            weatherYear ?? DefaultWeatherYear,
            cancellationToken);
        return result.ToActionResult();
    }
}
