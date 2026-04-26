using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using Asp.Versioning;
using AssistantEngineer.Api.Extensions.Results;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Calculations;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/floors/{floorId:int}/load-calculations")]
public sealed class FloorLoadCalculationsController : ControllerBase
{
    private readonly ILoadCalculationsFacade _loadCalculations;

    public FloorLoadCalculationsController(
        ILoadCalculationsFacade loadCalculations)
    {
        _loadCalculations = loadCalculations;
    }

    [HttpGet("cooling-load")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<FloorCalculationResult>> CalculateCoolingLoad(
        int floorId,
        [FromQuery] CoolingLoadCalculationMethodDto method = CoolingLoadCalculationMethodDto.Simplified,
        CancellationToken cancellationToken = default)
    {
        var result = await _loadCalculations.CalculateFloorCoolingLoadAsync(
            floorId,
            method,
            cancellationToken);

        return result.ToActionResult(this);
    }
}
