using AssistantEngineer.Modules.Calculations.Application.Contracts.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/profiles/annual")]
public sealed class AnnualProfilesController : ControllerBase
{
    private readonly ICalculationsFacade _calculations;

    public AnnualProfilesController(ICalculationsFacade calculations)
    {
        _calculations = calculations;
    }

    [HttpPost("generate")]
    public ActionResult<AnnualProfileResponse> Generate(
        [FromBody] AnnualProfileGenerationRequest request)
    {
        var result = _calculations.GenerateAnnualProfile(request);
        return Ok(result);
    }
}