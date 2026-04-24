using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/domestic-hot-water")]
public class DomesticHotWaterController : ControllerBase
{
    private readonly ICalculationsFacade _calculations;

    public DomesticHotWaterController(ICalculationsFacade calculations)
    {
        _calculations = calculations;
    }

    [HttpPost("demand")]
    public ActionResult<DomesticHotWaterDemandResult> CalculateDemand(
        [FromBody] DomesticHotWaterDemandRequest request)
    {
        var result = _calculations.CalculateDomesticHotWaterDemand(request);
        return result.ToActionResult(this);
    }
}
