using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;

namespace AssistantEngineer.Api.Contracts.Buildings;

public sealed class RoomListQueryParameters : CollectionQueryParameters
{
    public int? FloorId { get; set; }

    public RoomTypeDto? Type { get; set; }
}