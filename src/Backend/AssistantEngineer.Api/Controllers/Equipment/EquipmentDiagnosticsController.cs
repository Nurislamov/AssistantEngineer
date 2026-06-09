using AssistantEngineer.Api.Extensions.Http;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Equipment;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/equipment-diagnostics")]
public sealed class EquipmentDiagnosticsController : ControllerBase
{
    private readonly IEquipmentDiagnosticsFacade _equipmentDiagnostics;
    private readonly IEquipmentDiagnosticBotFacade _equipmentDiagnosticBot;

    public EquipmentDiagnosticsController(
        IEquipmentDiagnosticsFacade equipmentDiagnostics,
        IEquipmentDiagnosticBotFacade equipmentDiagnosticBot)
    {
        _equipmentDiagnostics = equipmentDiagnostics;
        _equipmentDiagnosticBot = equipmentDiagnosticBot;
    }

    [HttpGet("catalog")]
    public async Task<ActionResult<EquipmentDiagnosticsCatalogIndexDto>> GetCatalogIndex(
        CancellationToken cancellationToken)
    {
        var catalog = await _equipmentDiagnostics.GetCatalogIndexAsync(cancellationToken);

        return Ok(catalog);
    }

    [HttpGet("error-codes")]
    public async Task<ActionResult<IReadOnlyList<EquipmentErrorCodeSummaryDto>>> SearchErrorCodes(
        [FromQuery] string? manufacturer,
        [FromQuery(Name = "code")] string? errorCode,
        [FromQuery] string? series,
        [FromQuery] string? modelCode,
        [FromQuery] EquipmentCategory? category,
        [FromQuery] string? query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manufacturer))
        {
            return ApiProblemDetailsFactory.CreateValidationResult(
                this,
                "Manufacturer query parameter is required.",
                nameof(manufacturer),
                "Manufacturer query parameter is required.");
        }

        var results = await _equipmentDiagnostics.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(
                Manufacturer: manufacturer,
                ErrorCode: errorCode,
                Series: series,
                ModelCode: modelCode,
                Category: category,
                Query: query),
            cancellationToken);

        return Ok(results);
    }

    [HttpGet("cases")]
    public async Task<ActionResult<EquipmentDiagnosticCaseDto>> GetDiagnosticCase(
        [FromQuery] string? manufacturer,
        [FromQuery(Name = "code")] string? errorCode,
        [FromQuery] string? series,
        [FromQuery] string? modelCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manufacturer))
        {
            return ApiProblemDetailsFactory.CreateValidationResult(
                this,
                "Manufacturer query parameter is required.",
                nameof(manufacturer),
                "Manufacturer query parameter is required.");
        }

        if (string.IsNullOrWhiteSpace(errorCode))
        {
            return ApiProblemDetailsFactory.CreateValidationResult(
                this,
                "Code query parameter is required.",
                "code",
                "Code query parameter is required.");
        }

        var diagnosticCase = await _equipmentDiagnostics.GetDiagnosticCaseAsync(
            manufacturer,
            errorCode,
            series,
            modelCode,
            cancellationToken);

        if (diagnosticCase is null)
        {
            return ApiProblemDetailsFactory.CreateProblemResult(
                HttpContext,
                StatusCodes.Status404NotFound,
                "resource_not_found",
                "Not found",
                $"No equipment diagnostic case was found for manufacturer '{manufacturer}' and code '{errorCode}'.");
        }

        return Ok(diagnosticCase);
    }

    [HttpPost("bot/diagnose")]
    public async Task<ActionResult<EquipmentDiagnosticBotResponse>> DiagnoseBotRequest(
        [FromBody] EquipmentDiagnosticBotRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return ApiProblemDetailsFactory.CreateValidationResult(
                this,
                "Request body is required.",
                "request",
                "Request body is required.");
        }

        var response = await _equipmentDiagnosticBot.DiagnoseAsync(request, cancellationToken);

        return Ok(response);
    }
}
