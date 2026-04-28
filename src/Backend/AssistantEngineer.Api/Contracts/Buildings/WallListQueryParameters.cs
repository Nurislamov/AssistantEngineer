using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;

namespace AssistantEngineer.Api.Contracts.Buildings;

public sealed class WallListQueryParameters : CollectionQueryParameters
{
    public CardinalDirectionDto? Orientation { get; set; }

    public WallBoundaryTypeDto? BoundaryType { get; set; }

    public bool? IsExternal { get; set; }
}