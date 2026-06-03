using AssistantEngineer.Api.Extensions.Http;
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

    public EquipmentDiagnosticsController(IEquipmentDiagnosticsFacade equipmentDiagnostics)
    {
        _equipmentDiagnostics = equipmentDiagnostics;
    }

    [HttpGet("error-codes")]
    public async Task<ActionResult<IReadOnlyList<EquipmentErrorCodeSummaryDto>>> SearchErrorCodes(
        [FromQuery] string? manufacturer,
        [FromQuery(Name = "code")] string? errorCode,
        [FromQuery] string? series,
        [FromQuery] string? modelCode,
        [FromQuery] EquipmentCategory? category,
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
                Category: category),
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
}
