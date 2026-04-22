using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Services.Buildings;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/building-archetypes")]
public class BuildingArchetypesController : ControllerBase
{
    private readonly BuildingArchetypeService _archetypes;

    public BuildingArchetypesController(BuildingArchetypeService archetypes)
    {
        _archetypes = archetypes;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<BuildingArchetypeSummary>> ListArchetypes() =>
        Ok(_archetypes.ListArchetypes());
}
