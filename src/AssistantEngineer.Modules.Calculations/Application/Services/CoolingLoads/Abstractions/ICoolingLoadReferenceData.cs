using AssistantEngineer.Modules.Buildings.Domain.Enums;

namespace AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.Abstractions;

public interface ICoolingLoadReferenceData
{
    double GetWindowSolarLoadWPerM2(CardinalDirection orientation);
    double GetPeopleHeatGainW(RoomType roomType);
}
