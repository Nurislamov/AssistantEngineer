using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Services.Climate;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/climate-zones/{climateZoneId:int}/annual-climate-data")]
public class AnnualClimateDataController : ControllerBase
{
    private readonly EpwAnnualClimateDataImportService _epwImport;
    private readonly PvgisAnnualClimateDataImportService _pvgisImport;

    public AnnualClimateDataController(
        EpwAnnualClimateDataImportService epwImport,
        PvgisAnnualClimateDataImportService pvgisImport)
    {
        _epwImport = epwImport;
        _pvgisImport = pvgisImport;
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
            return BadRequest("EPW source file is required.");

        await using var stream = sourceFile.OpenReadStream();
        var result = await _epwImport.ImportAsync(
            climateZoneId,
            year,
            stream,
            sourceFile.FileName,
            cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("pvgis")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<AnnualClimateDataImportResponse>> ImportFromPvgis(
        int climateZoneId,
        [FromBody] ImportPvgisWeatherRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _pvgisImport.ImportAsync(
            climateZoneId,
            request,
            cancellationToken);

        return result.ToActionResult();
    }
}