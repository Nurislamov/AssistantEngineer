using AssistantEngineer.Modules.Calculations.Application.Contracts.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Profiles;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/profiles/annual")]
public sealed class AnnualProfilesController : ControllerBase
{
    private readonly IProfilesFacade _profiles;

    public AnnualProfilesController(
        IProfilesFacade profiles)
    {
        _profiles = profiles;
    }

    [HttpPost("generate")]
    public ActionResult<AnnualProfileResponse> Generate(
        [FromBody] AnnualProfileGenerationRequest request)
    {
        var result = _profiles.GenerateAnnualProfile(request);
        return Ok(result);
    }
}

