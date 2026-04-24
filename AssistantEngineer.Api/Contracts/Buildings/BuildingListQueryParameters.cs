using AssistantEngineer.Api.Contracts.Common;

namespace AssistantEngineer.Api.Contracts.Buildings;

public sealed class BuildingListQueryParameters : CollectionQueryParameters
{
    public int? ClimateZoneId { get; init; }
}
