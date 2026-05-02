using AssistantEngineer.Modules.Buildings.Application.Abstractions.StandardDefaults;
using AssistantEngineer.Modules.Buildings.Application.Models.StandardDefaults;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;

namespace AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;

public sealed class RoomStandardDefaultsProvider : IRoomStandardDefaultsProvider
{
    private readonly IInternalLoadStandardProvider _internalLoads;
    private readonly ITb14ReferenceDataProvider _tb14;
    private readonly IDomesticHotWaterStandardProvider _dhw;

    public RoomStandardDefaultsProvider(
        IInternalLoadStandardProvider internalLoads,
        ITb14ReferenceDataProvider tb14,
        IDomesticHotWaterStandardProvider dhw)
    {
        _internalLoads = internalLoads;
        _tb14 = tb14;
        _dhw = dhw;
    }

    public RoomStandardDefaults GetDefaults(Room room)
    {
        var area = room.Area.SquareMeters > 0 ? room.Area.SquareMeters : 0;

        var internalLoadRow = _internalLoads.GetRow(room.Type);
        var tb14Row = _tb14.GetRow(room.Type);
        var dhwRow = _dhw.GetRow(room.Type);

        var people = area > 0
            ? Math.Max(1, (int)Math.Round(
                area * internalLoadRow.OccupantDensityPeoplePer100M2 / 100.0,
                MidpointRounding.AwayFromZero))
            : 0;

        return new RoomStandardDefaults
        {
            SuggestedPeopleCount = people,
            EquipmentLoadWatts = Math.Max(0, area * internalLoadRow.EquipmentGainWPerM2),
            LightingLoadWatts = Math.Max(0, area * internalLoadRow.LightingGainWPerM2),
            MinimumVentilationLitersPerSecondM2 = internalLoadRow.MinimumVentilationLitersPerSecondM2,
            OutdoorAirLitersPerSecondPerPerson = tb14Row.OutdoorAirLitersPerSecondPerPerson,
            OutdoorAirLitersPerSecondPerM2 = tb14Row.OutdoorAirLitersPerSecondPerM2,
            HasDhwDefaults = dhwRow.LitersPerPersonDay > 0,
            SourceTableVersion = $"internal={internalLoadRow.Version};tb14={tb14Row.Version};dhw={dhwRow.Version}"
        };
    }
}