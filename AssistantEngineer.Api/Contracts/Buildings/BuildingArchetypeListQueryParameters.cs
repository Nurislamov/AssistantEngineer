using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Domain.Enums;

namespace AssistantEngineer.Api.Contracts.Buildings;

public sealed class BuildingArchetypeListQueryParameters : CollectionQueryParameters
{
    public RoomType? Type { get; init; }
}
