using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/building-archetypes")]
public class BuildingArchetypesController : ControllerBase
{
    private readonly IBuildingArchetypesFacade _archetypes;

    public BuildingArchetypesController(IBuildingArchetypesFacade archetypes)
    {
        _archetypes = archetypes;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<BuildingArchetypeSummary>> ListArchetypes() =>
        Ok(_archetypes.ListArchetypes());

    [HttpPost("projects/{projectId:int}/buildings")]
    public async Task<ActionResult<BuildingResponse>> CreateFromArchetype(
        int projectId,
        [FromBody] CreateBuildingFromArchetypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _archetypes.CreateFromArchetypeAsync(projectId, request, cancellationToken);
        return result.ToActionResult();
    }
}
