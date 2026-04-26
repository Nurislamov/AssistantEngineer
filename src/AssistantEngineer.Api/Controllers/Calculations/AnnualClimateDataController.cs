using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using Asp.Versioning;
using AssistantEngineer.Api.Extensions.Http;
using AssistantEngineer.Api.Extensions.Results;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Calculations;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/climate-zones/{climateZoneId:int}/annual-climate-data")]
public class AnnualClimateDataController : ControllerBase
{
    private readonly IBuildingsFacade _buildings;

    public AnnualClimateDataController(IBuildingsFacade buildings)
    {
        _buildings = buildings;
    }

    [HttpPost("epw")]
    [Consumes("multipart/form-data")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<AnnualClimateDataImportResponse>> ImportFromEpw(
        int climateZoneId,
        [FromForm] int year,
        [FromForm] IFormFile? sourceFile,
        CancellationToken cancellationToken)
    {
        if (sourceFile is null)
            return ApiProblemDetailsFactory.CreateValidationResult(
                this,
                "EPW source file is required.",
                nameof(sourceFile),
                "EPW source file is required.");

        await using var stream = sourceFile.OpenReadStream();
        var result = await _buildings.ImportAnnualClimateDataFromEpwAsync(
            climateZoneId,
            year,
            stream,
            sourceFile.FileName,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("pvgis")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<AnnualClimateDataImportResponse>> ImportFromPvgis(
        int climateZoneId,
        [FromBody] ImportPvgisWeatherRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.ImportAnnualClimateDataFromPvgisAsync(
            climateZoneId,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }
}

