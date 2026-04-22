using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/climate-zones/{climateZoneId:int}/annual-climate-data")]
public class AnnualClimateDataController : ControllerBase
{
    private readonly IAnnualClimateDataFacade _annualClimateData;

    public AnnualClimateDataController(IAnnualClimateDataFacade annualClimateData)
    {
        _annualClimateData = annualClimateData;
    }

    [HttpPost("epw")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<AnnualClimateDataImportResponse>> ImportFromEpw(
        int climateZoneId,
        [FromForm] int year,
        [FromForm] IFormFile? sourceFile,
        CancellationToken cancellationToken)
    {
        if (sourceFile is null)
            return BadRequest("EPW source file is required.");

        await using var stream = sourceFile.OpenReadStream();
        var result = await _annualClimateData.ImportFromEpwAsync(
            climateZoneId,
            year,
            stream,
            sourceFile.FileName,
            cancellationToken);
        return result.ToActionResult();
    }
}
