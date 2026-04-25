using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;

namespace AssistantEngineer.Api.Contracts.Buildings;

public sealed class WallListQueryParameters : CollectionQueryParameters
{
    public CardinalDirectionDto? Orientation { get; init; }
    public WallBoundaryTypeDto? BoundaryType { get; init; }
    public bool? IsExternal { get; init; }
}
