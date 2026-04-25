using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;

namespace AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;

public sealed class Iso16798ReferenceData : IIso16798ReferenceData
{
    private readonly IInternalLoadStandardProvider _internalLoads;

    public Iso16798ReferenceData(IInternalLoadStandardProvider internalLoads)
    {
        _internalLoads = internalLoads;
    }

    public Iso16798RoomDefaults GetRoomDefaults(RoomType roomType)
    {
        var row = _internalLoads.GetRow(roomType);

        return new Iso16798RoomDefaults(
            row.SensibleHeatGainPerPersonW,
            row.EquipmentGainWPerM2,
            row.LightingGainWPerM2,
            row.MinimumVentilationLitersPerSecondM2);
    }
}