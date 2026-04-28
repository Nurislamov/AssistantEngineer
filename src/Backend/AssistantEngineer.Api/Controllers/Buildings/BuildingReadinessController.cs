using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using Asp.Versioning;
using AssistantEngineer.Api.Extensions.Results;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Buildings;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/buildings/{buildingId:int}/readiness")]
public class BuildingReadinessController : ControllerBase
{
    private const int DefaultWeatherYear = 2020;

    private readonly IBuildingsFacade _buildings;

    public BuildingReadinessController(IBuildingsFacade buildings)
    {
        _buildings = buildings;
    }

    [HttpGet]
    public async Task<ActionResult<BuildingCalculationReadinessReport>> Check(
        int buildingId,
        [FromQuery] int? weatherYear,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.CheckBuildingReadinessAsync(
            buildingId,
            weatherYear ?? DefaultWeatherYear,
            cancellationToken);
        return result.ToActionResult(this);
    }
}

