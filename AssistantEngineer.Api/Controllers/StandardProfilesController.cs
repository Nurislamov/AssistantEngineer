using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Services.Profiles;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/standard-profiles/en16798")]
public class StandardProfilesController : ControllerBase
{
    private readonly En16798ProfileService _profiles;

    public StandardProfilesController(En16798ProfileService profiles)
    {
        _profiles = profiles;
    }

    [HttpGet("room-usage")]
    public ActionResult<En16798RoomUsageProfileResponse> GetRoomUsageProfile(
        [FromQuery] RoomTypeDto roomType,
        [FromQuery] En16798ProfileCategory category = En16798ProfileCategory.II)
    {
        var result = _profiles.GetRoomUsageProfile(roomType, category);
        return Ok(result);
    }
}