using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;

namespace AssistantEngineer.Api.Contracts.Buildings;

public sealed class WindowListQueryParameters : CollectionQueryParameters
{
    public CardinalDirectionDto? Orientation { get; set; }
}