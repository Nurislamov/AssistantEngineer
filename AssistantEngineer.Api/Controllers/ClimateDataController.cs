using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Services.Climate;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[Route("api/v1/climate-zones")]
[Route("api/climate-zones")]
public class ClimateDataController : ControllerBase
{
    private readonly EpwWeatherImportService _epwWeatherImportService;

    public ClimateDataController(EpwWeatherImportService epwWeatherImportService)
    {
        _epwWeatherImportService = epwWeatherImportService;
    }

    [HttpPost("{climateZoneId:int}/annual-climate-data/epw")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<AnnualClimateDataImportResponse>> ImportEpwWeather(
        int climateZoneId,
        [FromBody] ImportEpwWeatherRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _epwWeatherImportService.ImportAsync(
            climateZoneId,
            request,
            cancellationToken);
        return result.ToActionResult();
    }
}
