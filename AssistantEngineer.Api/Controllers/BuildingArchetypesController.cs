using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Api.Facades;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/building-performance")]
public class BuildingArchetypesController : ControllerBase
{
    private readonly IBuildingsFacade _buildings;

    public BuildingArchetypesController(IBuildingsFacade buildings)
    {
        _buildings = buildings;
    }

    [HttpGet("archetypes")]
    public ActionResult<IReadOnlyList<BuildingArchetypeSummary>> ListArchetypes() =>
        Ok(_buildings.ListArchetypes());

    [HttpPost("projects/{projectId:int}/buildings/from-archetype")]
    public async Task<ActionResult<BuildingResponse>> CreateFromArchetype(
        int projectId,
        [FromBody] CreateBuildingFromArchetypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _buildings.CreateFromArchetypeAsync(projectId, request, cancellationToken);
        return result.ToActionResult();
    }
}
