using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground;
using Asp.Versioning;
using AssistantEngineer.Api.Extensions.Results;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Calculations;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/buildings/{buildingId:int}/ground-temperature")]
public sealed class GroundTemperatureController : ControllerBase
{
    private readonly GroundTemperatureProfilePreviewService _groundTemperature;

    public GroundTemperatureController(GroundTemperatureProfilePreviewService groundTemperature)
    {
        _groundTemperature = groundTemperature;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<GroundTemperatureProfileResponse>> GetProfile(
        int buildingId,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var result = await _groundTemperature.PreviewAsync(
            buildingId,
            year,
            cancellationToken);

        return result.ToActionResult(this);
    }
}

