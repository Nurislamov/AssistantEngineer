using AssistantEngineer.Api.Extensions.Http;
using AssistantEngineer.Api.Extensions.Results;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Contracts.EquipmentSizing;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Requests;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Equipment;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/rooms/{roomId:int}/equipment-selection")]
public sealed class RoomEquipmentSelectionController : ControllerBase
{
    private readonly ILoadCalculationsFacade _loadCalculations;

    public RoomEquipmentSelectionController(ILoadCalculationsFacade loadCalculations)
    {
        _loadCalculations = loadCalculations;
    }

    [HttpPost]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<EquipmentSelectionResult>> SelectEquipment(
        int roomId,
        [FromBody] EquipmentSelectionRequest request,
        [FromQuery] CoolingLoadCalculationMethodDto method = CoolingLoadCalculationMethodDto.Simplified,
        CancellationToken cancellationToken = default)
    {
        var sizing = await _loadCalculations.CalculateRoomEquipmentSizingAsync(
            roomId,
            request.SystemType,
            request.UnitType,
            method,
            cancellationToken);

        if (sizing.IsFailure)
            return ApiProblemDetailsFactory.CreateResult(HttpContext, sizing);

        return Ok(MapSelectionResult(roomId, request, sizing.Value));
    }

    private static EquipmentSelectionResult MapSelectionResult(
        int roomId,
        EquipmentSelectionRequest request,
        EquipmentSizingResult sizing)
    {
        var best = sizing.BestMatch;
        var capacityWithReserveW = Math.Max(
            sizing.RequiredHeatingCapacityWithReserveW,
            sizing.RequiredCoolingCapacityWithReserveW);

        return new EquipmentSelectionResult
        {
            RoomId = roomId,
            EquipmentSelected = best is not null,
            CalculationMethod = "ExternalReferenceValidationEquipmentSizing",
            CoolingLoadKw = RoundKw(sizing.RequiredCoolingCapacityW),
            DesignCapacityKw = RoundKw(capacityWithReserveW),
            RequiredCoolingCapacityW = sizing.RequiredCoolingCapacityW,
            RequiredHeatingCapacityW = sizing.RequiredHeatingCapacityW,
            CapacityWithReserveW = capacityWithReserveW,
            SafetyFactor = sizing.SafetyFactor,
            HeatingSafetyFactor = sizing.HeatingSafetyFactor,
            CoolingSafetyFactor = sizing.CoolingSafetyFactor,
            RequestedSystemType = request.SystemType,
            RequestedUnitType = request.UnitType,
            SelectedCatalogItemId = best?.EquipmentId ?? 0,
            SelectedManufacturer = best?.Name ?? string.Empty,
            SelectedModelName = best?.Model ?? string.Empty,
            SelectedNominalCoolingCapacityKw = best?.CoolingCapacityW is null ? 0 : RoundKw(best.CoolingCapacityW.Value),
            CapacityReserveKw = best is null ? 0 : RoundKw(best.CoolingMarginW),
            AcceptedCandidates = sizing.RecommendedEquipment
                .Select(candidate => new EquipmentSelectionCandidateResult
                {
                    CatalogItemId = candidate.EquipmentId,
                    Manufacturer = candidate.Name,
                    ModelName = candidate.Model,
                    HeatingCapacityW = candidate.HeatingCapacityW,
                    CoolingCapacityW = candidate.CoolingCapacityW,
                    HeatingMarginW = candidate.HeatingMarginW,
                    CoolingMarginW = candidate.CoolingMarginW,
                    Score = candidate.Score,
                    Notes = candidate.Notes.ToList()
                })
                .ToList(),
            RejectedCandidates = sizing.RejectedEquipment
                .Select(candidate => new EquipmentSelectionRejectedCandidate
                {
                    CatalogItemId = candidate.EquipmentId,
                    Manufacturer = candidate.Name,
                    ModelName = candidate.Model,
                    Reasons = candidate.Reasons.ToList()
                })
                .ToList(),
            Diagnostics = sizing.Diagnostics
                .Select(diagnostic => new EquipmentSelectionDiagnostic
                {
                    Severity = diagnostic.Severity.ToString(),
                    Code = diagnostic.Code,
                    Message = diagnostic.Message,
                    Context = diagnostic.Context
                })
                .ToList()
        };
    }

    private static double RoundKw(double watts) =>
        Math.Round(watts / 1000.0, 2, MidpointRounding.AwayFromZero);
}
