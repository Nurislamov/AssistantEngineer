using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Buildings.Application.Facades;

public interface IBuildingArchetypesFacade
{
    IReadOnlyList<BuildingArchetypeSummary> ListArchetypes();

    Task<Result<BuildingResponse>> CreateFromArchetypeAsync(
        int projectId,
        CreateBuildingFromArchetypeRequest request,
        CancellationToken cancellationToken);
}
