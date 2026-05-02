using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Profiles;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Profiles;

public sealed class En16798ProfileService
{
    private readonly IEn16798ProfileCatalog _catalog;

    public En16798ProfileService(IEn16798ProfileCatalog catalog)
    {
        _catalog = catalog;
    }

    public En16798RoomUsageProfileResponse GetRoomUsageProfile(
        RoomTypeDto roomType,
        En16798ProfileCategory category)
    {
        var profile = _catalog.GetProfile(ToDomain(roomType), category);

        return new En16798RoomUsageProfileResponse
        {
            RoomType = roomType,
            Category = category,
            HeatingSetpointOccupiedC = profile.HeatingSetpointOccupiedC,
            CoolingSetpointOccupiedC = profile.CoolingSetpointOccupiedC,
            OutdoorAirLPerPerson = profile.OutdoorAirLPerPerson,
            OccupancyFactors = profile.OccupancyFactors.ToList(),
            EquipmentFactors = profile.EquipmentFactors.ToList(),
            LightingFactors = profile.LightingFactors.ToList(),
            VentilationFactors = profile.VentilationFactors.ToList()
        };
    }

    private static RoomType ToDomain(RoomTypeDto roomType) => roomType switch
    {
        RoomTypeDto.Office => RoomType.Office,
        RoomTypeDto.MeetingRoom => RoomType.MeetingRoom,
        RoomTypeDto.Corridor => RoomType.Corridor,
        RoomTypeDto.ServerRoom => RoomType.ServerRoom,
        RoomTypeDto.Retail => RoomType.Retail,
        RoomTypeDto.Residential => RoomType.Residential,
        RoomTypeDto.Other => RoomType.Other,
        _ => RoomType.Other
    };
}