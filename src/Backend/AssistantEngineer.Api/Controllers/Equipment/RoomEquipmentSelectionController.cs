using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Requests;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Application.Facades;
using Asp.Versioning;
using AssistantEngineer.Api.Extensions.Http;
using AssistantEngineer.Api.Extensions.Results;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Equipment;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/rooms/{roomId:int}/equipment-selection")]
public sealed class RoomEquipmentSelectionController : ControllerBase
{
    private readonly ILoadCalculationsFacade _loadCalculations;
    private readonly IEquipmentFacade _equipment;

    public RoomEquipmentSelectionController(
        ILoadCalculationsFacade loadCalculations,
        IEquipmentFacade equipment)
    {
        _loadCalculations = loadCalculations;
        _equipment = equipment;
    }

    [HttpPost]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<EquipmentSelectionResult>> SelectEquipment(
        int roomId,
        [FromBody] EquipmentSelectionRequest request,
        [FromQuery] CoolingLoadCalculationMethodDto method = CoolingLoadCalculationMethodDto.Simplified,
        CancellationToken cancellationToken = default)
    {
        var coolingLoad = await _loadCalculations.CalculateRoomCoolingLoadAsync(
            roomId,
            method,
            cancellationToken);

        if (coolingLoad.IsFailure)
            return ApiProblemDetailsFactory.CreateResult(HttpContext, coolingLoad);

        var result = await _equipment.SelectRoomEquipmentAsync(
            roomId,
            request,
            coolingLoad.Value.TotalHeatLoadKw,
            coolingLoad.Value.DesignCapacityKw,
            cancellationToken);

        return result.ToOkResult(this);
    }
}

