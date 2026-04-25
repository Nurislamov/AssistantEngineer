using AssistantEngineer.Modules.Buildings.Application.Abstractions.StandardDefaults;
using AssistantEngineer.Modules.Buildings.Application.Models.StandardDefaults;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;

namespace AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;

public sealed class RoomVentilationDefaultsProvider : IRoomVentilationDefaultsProvider
{
    private readonly IInternalLoadStandardProvider _internalLoads;
    private readonly ITb14ReferenceDataProvider _tb14;

    public RoomVentilationDefaultsProvider(
        IInternalLoadStandardProvider internalLoads,
        ITb14ReferenceDataProvider tb14)
    {
        _internalLoads = internalLoads;
        _tb14 = tb14;
    }

    public RoomVentilationDefaults GetDefaults(Room room)
    {
        var area = room.Area.SquareMeters;
        if (area <= 0)
        {
            return new RoomVentilationDefaults
            {
                CanApply = false,
                Reason = "Room area must be positive to derive ventilation defaults."
            };
        }

        if (room.HeightM <= 0)
        {
            return new RoomVentilationDefaults
            {
                CanApply = false,
                Reason = "Room height must be positive to derive ventilation defaults."
            };
        }

        var internalLoadRow = _internalLoads.GetRow(room.Type);
        var tb14Row = _tb14.GetRow(room.Type);

        var derivedPeopleCount = room.PeopleCount > 0
            ? room.PeopleCount
            : CalculateSuggestedPeopleCount(area, internalLoadRow.OccupantDensityPeoplePer100M2);

        var outdoorAirLitersPerSecond =
            (tb14Row.OutdoorAirLitersPerSecondPerPerson * derivedPeopleCount) +
            (tb14Row.OutdoorAirLitersPerSecondPerM2 * area);

        var volume = Math.Max(room.CalculateVolume(), 0.001);
        var outdoorAirAch = outdoorAirLitersPerSecond * 3.6 / volume;
        var exhaustAch = Math.Max(0, tb14Row.ExhaustAirChangesPerHour);
        var proposedAch = Math.Max(outdoorAirAch, exhaustAch);

        return new RoomVentilationDefaults
        {
            CanApply = proposedAch > 0,
            Reason = proposedAch > 0
                ? string.Empty
                : "Derived ventilation default is zero, so no parameters were proposed.",
            DesignPeopleCount = derivedPeopleCount,
            DesignOutdoorAirLitersPerSecond = Math.Round(outdoorAirLitersPerSecond, 4),
            OutdoorAirAirChangesPerHour = Math.Round(outdoorAirAch, 4),
            ExhaustAirChangesPerHour = Math.Round(exhaustAch, 4),
            ProposedAirChangesPerHour = Math.Round(proposedAch, 4),
            HeatRecoveryEfficiency = 0,
            InfiltrationAirChangesPerHour = GetDefaultInfiltrationAch(room.Type),
            WindExposureFactor = GetDefaultWindExposureFactor(room),
            StackCoefficient = 1.0,
            WindCoefficient = GetDefaultWindCoefficient(room),
            SourceTableVersion = $"internal={internalLoadRow.Version};tb14={tb14Row.Version}"
        };
    }

    private static int CalculateSuggestedPeopleCount(double area, double peoplePer100M2)
    {
        if (area <= 0 || peoplePer100M2 <= 0)
            return 0;

        return Math.Max(
            1,
            (int)Math.Round(area * peoplePer100M2 / 100.0, MidpointRounding.AwayFromZero));
    }

    private static double GetDefaultInfiltrationAch(RoomType roomType) =>
        roomType switch
        {
            RoomType.Residential => 0.35,
            RoomType.Corridor => 0.30,
            RoomType.Retail => 0.25,
            RoomType.Office => 0.20,
            RoomType.MeetingRoom => 0.20,
            RoomType.ServerRoom => 0.15,
            _ => 0.20
        };

    private static double GetDefaultWindExposureFactor(Room room) =>
        room.Walls.Any(wall => wall.IsExternal) ? 1.0 : 0.5;

    private static double GetDefaultWindCoefficient(Room room) =>
        room.Windows.Count > 0 ? 0.6 : 0.3;
}