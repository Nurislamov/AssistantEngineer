using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Services.Rooms;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using AssistantEngineer.SharedKernel.Primitives;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
public class RoomVentilationController : ControllerBase
{
    private readonly RoomVentilationCommandService _command;
    private readonly RoomVentilationQueryService _query;
    private readonly NaturalVentilationPreviewService _preview;

    public RoomVentilationController(
        RoomVentilationCommandService command,
        RoomVentilationQueryService query,
        NaturalVentilationPreviewService preview)
    {
        _command = command;
        _query = query;
        _preview = preview;
    }

    [HttpGet("api/v{version:apiVersion}/rooms/{roomId:int}/ventilation-parameters")]
    public async Task<ActionResult<RoomVentilationParametersResponse>> Get(
        int roomId,
        CancellationToken cancellationToken)
    {
        var result = await _query.GetAsync(roomId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("api/v{version:apiVersion}/rooms/{roomId:int}/ventilation-parameters")]
    public async Task<ActionResult<RoomVentilationParametersResponse>> Upsert(
        int roomId,
        [FromBody] UpsertRoomVentilationParametersRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _command.UpsertAsync(roomId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("api/v{version:apiVersion}/rooms/{roomId:int}/ventilation-parameters")]
    public async Task<IActionResult> Delete(
        int roomId,
        CancellationToken cancellationToken)
    {
        var result = await _command.DeleteAsync(roomId, cancellationToken);

        if (result.IsSuccess)
            return NoContent();

        return result.ErrorType switch
        {
            ResultErrorType.NotFound => NotFound(result.Error),
            ResultErrorType.Validation => BadRequest(result.Error),
            ResultErrorType.Conflict => Conflict(result.Error),
            _ => BadRequest(result.Error)
        };
    }

    [HttpPost("api/v{version:apiVersion}/rooms/{roomId:int}/natural-ventilation/preview")]
    public async Task<ActionResult<NaturalVentilationPreviewResponse>> Preview(
        int roomId,
        [FromBody] NaturalVentilationPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _preview.PreviewAsync(roomId, request, cancellationToken);
        return result.ToActionResult();
    }
}