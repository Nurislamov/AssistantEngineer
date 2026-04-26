using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using Asp.Versioning;
using AssistantEngineer.Api.Extensions.Results;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Calculations;

[ApiController]
[ApiVersion("1.0")]
public sealed class RoomVentilationController : ControllerBase
{
    private readonly IBuildingsFacade _buildings;
    private readonly IVentilationAnalysisFacade _ventilationAnalysis;

    public RoomVentilationController(
        IBuildingsFacade buildings,
        IVentilationAnalysisFacade ventilationAnalysis)
    {
        _buildings = buildings;
        _ventilationAnalysis = ventilationAnalysis;
    }

    [HttpGet("api/v{version:apiVersion}/rooms/{roomId:int}/ventilation-parameters")]
    public async Task<ActionResult<RoomVentilationParametersResponse>> Get(
        int roomId,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.GetRoomVentilationParametersAsync(
            roomId,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPut("api/v{version:apiVersion}/rooms/{roomId:int}/ventilation-parameters")]
    public async Task<ActionResult<RoomVentilationParametersResponse>> Upsert(
        int roomId,
        [FromBody] UpsertRoomVentilationParametersRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.UpsertRoomVentilationParametersAsync(
            roomId,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpDelete("api/v{version:apiVersion}/rooms/{roomId:int}/ventilation-parameters")]
    public async Task<IActionResult> Delete(
        int roomId,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.DeleteRoomVentilationParametersAsync(
            roomId,
            cancellationToken);

        return result.ToNoContentResult(this);
    }

    [HttpGet("api/v{version:apiVersion}/rooms/{roomId:int}/ventilation-parameters/defaults")]
    public async Task<ActionResult<RoomVentilationDefaultsResponse>> GetDefaults(
        int roomId,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.PreviewRoomVentilationDefaultsAsync(
            roomId,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("api/v{version:apiVersion}/rooms/{roomId:int}/ventilation-parameters/apply-defaults")]
    public async Task<ActionResult<RoomVentilationParametersResponse>> ApplyDefaults(
        int roomId,
        [FromBody] ApplyRoomVentilationDefaultsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.ApplyRoomVentilationDefaultsAsync(
            roomId,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }

    [HttpPost("api/v{version:apiVersion}/rooms/{roomId:int}/natural-ventilation/preview")]
    public async Task<ActionResult<NaturalVentilationPreviewResponse>> Preview(
        int roomId,
        [FromBody] NaturalVentilationPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ventilationAnalysis.PreviewNaturalVentilationAsync(
            roomId,
            request,
            cancellationToken);

        return result.ToActionResult(this);
    }
}
