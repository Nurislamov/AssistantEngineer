using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;

namespace AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;

public sealed class Iso16798ReferenceData : IIso16798ReferenceData
{
    public Iso16798RoomDefaults GetRoomDefaults(RoomType roomType) =>
        roomType switch
        {
            RoomType.Residential => new(80, 4, 5, 0.5),
            RoomType.Corridor => new(80, 1, 3, 0.5),
            RoomType.MeetingRoom => new(125, 8, 10, 3.8),
            RoomType.Office => new(125, 12, 10, 0.8),
            RoomType.Retail => new(170, 15, 15, 4.5),
            RoomType.ServerRoom => new(125, 120, 5, 0.5),
            _ => new(125, 10, 10, 0.8)
        };
}