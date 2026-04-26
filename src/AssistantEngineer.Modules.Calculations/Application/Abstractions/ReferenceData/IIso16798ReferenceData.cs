using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;

public interface IIso16798ReferenceData
{
    Iso16798RoomDefaults GetRoomDefaults(RoomType roomType);
}