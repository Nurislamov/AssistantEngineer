using AssistantEngineer.Domain.Models;

namespace AssistantEngineer.Application.Services.Calculations;

public sealed class CoolingLoadReferenceData : ICoolingLoadReferenceData
{
    public double GetWindowSolarLoadWPerM2(CardinalDirection orientation) =>
        orientation switch
        {
            CardinalDirection.North => 0,
            CardinalDirection.NorthEast => 190,
            CardinalDirection.East => 250,
            CardinalDirection.SouthEast => 240,
            CardinalDirection.South => 240,
            CardinalDirection.SouthWest => 350,
            CardinalDirection.West => 470,
            CardinalDirection.NorthWest => 370,
            _ => 0
        };

    public double GetPeopleHeatGainW(RoomType roomType) =>
        roomType switch
        {
            RoomType.Residential => 80,
            RoomType.Corridor => 80,
            RoomType.MeetingRoom => 125,
            RoomType.Office => 125,
            RoomType.Retail => 170,
            RoomType.ServerRoom => 125,
            _ => 125
        };
}
