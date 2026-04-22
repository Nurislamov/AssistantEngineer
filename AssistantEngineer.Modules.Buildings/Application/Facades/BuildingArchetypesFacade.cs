using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Services.Buildings;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Buildings.Application.Facades;

public sealed class BuildingArchetypesFacade : IBuildingArchetypesFacade
{
    private readonly BuildingArchetypeService _archetypes;

    public BuildingArchetypesFacade(BuildingArchetypeService archetypes)
    {
        _archetypes = archetypes;
    }

    public IReadOnlyList<BuildingArchetypeSummary> ListArchetypes() =>
        _archetypes.ListArchetypes();

    public Task<Result<BuildingResponse>> CreateFromArchetypeAsync(
        int projectId,
        CreateBuildingFromArchetypeRequest request,
        CancellationToken cancellationToken) =>
        _archetypes.CreateFromArchetypeAsync(projectId, request, cancellationToken);
}
