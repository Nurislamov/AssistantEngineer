using AssistantEngineer.Domain.Models;

namespace AssistantEngineer.Application.Services.Calculations;

public interface ICoolingLoadReferenceData
{
    double GetWindowSolarLoadWPerM2(CardinalDirection orientation);
    double GetPeopleHeatGainW(RoomType roomType);
}
