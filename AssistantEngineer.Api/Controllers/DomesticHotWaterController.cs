using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/domestic-hot-water")]
public class DomesticHotWaterController : ControllerBase
{
    private readonly DomesticHotWaterDemandService _dhw;

    public DomesticHotWaterController(DomesticHotWaterDemandService dhw)
    {
        _dhw = dhw;
    }

    [HttpPost("demand")]
    public ActionResult<DomesticHotWaterDemandResult> CalculateDemand(
        [FromBody] DomesticHotWaterDemandRequest request)
    {
        var result = _dhw.Calculate(request);
        return result.ToActionResult();
    }
}
