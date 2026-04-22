using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/buildings/{buildingId:int}/readiness")]
public class BuildingReadinessController : ControllerBase
{
    private readonly IBuildingReadinessFacade _readiness;

    public BuildingReadinessController(IBuildingReadinessFacade readiness)
    {
        _readiness = readiness;
    }

    [HttpGet]
    public async Task<ActionResult<BuildingCalculationReadinessReport>> Check(
        int buildingId,
        [FromQuery] int? weatherYear,
        CancellationToken cancellationToken)
    {
        var result = await _readiness.CheckAsync(buildingId, weatherYear, cancellationToken);
        return result.ToActionResult();
    }
}
