using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using Asp.Versioning;
using AssistantEngineer.Api.Extensions.Results;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Calculations;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/domestic-hot-water")]
public class DomesticHotWaterController : ControllerBase
{
    private readonly IDomesticHotWaterFacade _domesticHotWater;

    public DomesticHotWaterController(
        IDomesticHotWaterFacade domesticHotWater)
    {
        _domesticHotWater = domesticHotWater;
    }

    [HttpPost("demand")]
    public ActionResult<DomesticHotWaterDemandResult> CalculateDemand(
        [FromBody] DomesticHotWaterDemandRequest request)
    {
        var result = _domesticHotWater.CalculateDomesticHotWaterDemand(request);
        return result.ToActionResult(this);
    }
}
