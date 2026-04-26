using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using Asp.Versioning;
using AssistantEngineer.Api.Extensions.Results;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Buildings;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/buildings/{buildingId:int}/validation")]
public sealed class BuildingValidationController : ControllerBase
{
    private const int DefaultWeatherYear = 2020;

    private readonly IBuildingsFacade _buildings;

    public BuildingValidationController(IBuildingsFacade buildings)
    {
        _buildings = buildings;
    }

    [HttpGet]
    public async Task<ActionResult<BuildingValidationReport>> Validate(
        int buildingId,
        [FromQuery] int? weatherYear,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.ValidateBuildingModelAsync(
            buildingId,
            weatherYear ?? DefaultWeatherYear,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("autocorrect-preview")]
    public async Task<ActionResult<BuildingAutocorrectionPreview>> PreviewAutocorrection(
        int buildingId,
        [FromBody] AutocorrectBuildingModelRequest request,
        [FromQuery] int? weatherYear,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.PreviewBuildingAutocorrectionsAsync(
            buildingId,
            weatherYear ?? DefaultWeatherYear,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("autocorrect-apply")]
    public async Task<ActionResult<BuildingAutocorrectionResult>> ApplyAutocorrection(
        int buildingId,
        [FromBody] AutocorrectBuildingModelRequest request,
        [FromQuery] int? weatherYear,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.ApplyBuildingAutocorrectionsAsync(
            buildingId,
            weatherYear ?? DefaultWeatherYear,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }
}
