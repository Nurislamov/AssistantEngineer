using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;

namespace AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;

public sealed class En16798ProfileCatalog : IEn16798ProfileCatalog
{
    public En16798RoomUsageProfile GetProfile(RoomType roomType, En16798ProfileCategory category)
    {
        var baseProfile = roomType switch
        {
            RoomType.Office => CreateOffice(category),
            RoomType.MeetingRoom => CreateMeetingRoom(category),
            RoomType.Corridor => CreateCorridor(category),
            RoomType.ServerRoom => CreateServerRoom(category),
            RoomType.Retail => CreateRetail(category),
            RoomType.Residential => CreateResidential(category),
            _ => CreateOther(category)
        };

        return baseProfile;
    }

    private static En16798RoomUsageProfile CreateOffice(En16798ProfileCategory category) =>
        new(
            RoomType.Office,
            category,
            HeatingSetpoint(category),
            CoolingSetpoint(category),
            OutdoorAirPerPerson(category),
            OccupancyFactors:
            [
                0.00, 0.00, 0.00, 0.00, 0.00, 0.03, 0.08, 0.20, 0.60, 0.90, 1.00, 1.00,
                1.00, 1.00, 1.00, 0.95, 0.85, 0.55, 0.20, 0.05, 0.00, 0.00, 0.00, 0.00
            ],
            EquipmentFactors:
            [
                0.10, 0.10, 0.10, 0.10, 0.10, 0.15, 0.25, 0.45, 0.75, 0.95, 1.00, 1.00,
                1.00, 1.00, 1.00, 0.95, 0.85, 0.60, 0.30, 0.15, 0.10, 0.10, 0.10, 0.10
            ],
            LightingFactors:
            [
                0.00, 0.00, 0.00, 0.00, 0.00, 0.05, 0.15, 0.35, 0.75, 0.95, 1.00, 1.00,
                1.00, 1.00, 1.00, 0.95, 0.90, 0.65, 0.25, 0.08, 0.00, 0.00, 0.00, 0.00
            ],
            VentilationFactors:
            [
                0.10, 0.10, 0.10, 0.10, 0.10, 0.15, 0.25, 0.45, 0.75, 0.95, 1.00, 1.00,
                1.00, 1.00, 1.00, 0.95, 0.85, 0.60, 0.25, 0.10, 0.10, 0.10, 0.10, 0.10
            ]);

    private static En16798RoomUsageProfile CreateMeetingRoom(En16798ProfileCategory category) =>
        new(
            RoomType.MeetingRoom,
            category,
            HeatingSetpoint(category),
            CoolingSetpoint(category),
            OutdoorAirPerPerson(category) * 1.2,
            OccupancyFactors:
            [
                0.00, 0.00, 0.00, 0.00, 0.00, 0.02, 0.05, 0.10, 0.40, 0.75, 0.90, 0.75,
                0.65, 0.80, 0.95, 0.85, 0.70, 0.45, 0.15, 0.05, 0.00, 0.00, 0.00, 0.00
            ],
            EquipmentFactors:
            [
                0.05, 0.05, 0.05, 0.05, 0.05, 0.08, 0.10, 0.20, 0.45, 0.75, 0.90, 0.80,
                0.70, 0.80, 0.95, 0.85, 0.75, 0.50, 0.20, 0.10, 0.05, 0.05, 0.05, 0.05
            ],
            LightingFactors:
            [
                0.00, 0.00, 0.00, 0.00, 0.00, 0.03, 0.05, 0.15, 0.50, 0.80, 0.95, 0.85,
                0.75, 0.85, 1.00, 0.90, 0.80, 0.55, 0.20, 0.05, 0.00, 0.00, 0.00, 0.00
            ],
            VentilationFactors:
            [
                0.10, 0.10, 0.10, 0.10, 0.10, 0.10, 0.15, 0.25, 0.55, 0.85, 1.00, 0.90,
                0.80, 0.90, 1.00, 0.95, 0.80, 0.60, 0.25, 0.10, 0.10, 0.10, 0.10, 0.10
            ]);

    private static En16798RoomUsageProfile CreateCorridor(En16798ProfileCategory category) =>
        new(
            RoomType.Corridor,
            category,
            HeatingSetpoint(category) - 1.0,
            CoolingSetpoint(category) + 1.0,
            OutdoorAirPerPerson(category) * 0.3,
            OccupancyFactors:
            [
                0.05, 0.05, 0.05, 0.05, 0.05, 0.08, 0.10, 0.15, 0.20, 0.25, 0.25, 0.25,
                0.25, 0.25, 0.25, 0.25, 0.20, 0.18, 0.15, 0.10, 0.08, 0.05, 0.05, 0.05
            ],
            EquipmentFactors:
            [
                0.05, 0.05, 0.05, 0.05, 0.05, 0.05, 0.05, 0.05, 0.10, 0.10, 0.10, 0.10,
                0.10, 0.10, 0.10, 0.10, 0.10, 0.08, 0.05, 0.05, 0.05, 0.05, 0.05, 0.05
            ],
            LightingFactors:
            [
                0.20, 0.20, 0.20, 0.20, 0.20, 0.25, 0.35, 0.45, 0.60, 0.60, 0.60, 0.60,
                0.60, 0.60, 0.60, 0.60, 0.60, 0.50, 0.40, 0.30, 0.25, 0.20, 0.20, 0.20
            ],
            VentilationFactors:
            [
                0.30, 0.30, 0.30, 0.30, 0.30, 0.35, 0.40, 0.45, 0.50, 0.50, 0.50, 0.50,
                0.50, 0.50, 0.50, 0.50, 0.50, 0.45, 0.40, 0.35, 0.30, 0.30, 0.30, 0.30
            ]);

    private static En16798RoomUsageProfile CreateServerRoom(En16798ProfileCategory category) =>
        new(
            RoomType.ServerRoom,
            category,
            18.0,
            24.0,
            OutdoorAirPerPerson(category) * 0.1,
            OccupancyFactors: Enumerable.Repeat(0.02, 24).ToArray(),
            EquipmentFactors: Enumerable.Repeat(1.00, 24).ToArray(),
            LightingFactors:
            [
                0.05, 0.05, 0.05, 0.05, 0.05, 0.05, 0.10, 0.10, 0.15, 0.15, 0.15, 0.15,
                0.15, 0.15, 0.15, 0.15, 0.15, 0.10, 0.10, 0.08, 0.05, 0.05, 0.05, 0.05
            ],
            VentilationFactors: Enumerable.Repeat(1.00, 24).ToArray());

    private static En16798RoomUsageProfile CreateRetail(En16798ProfileCategory category) =>
        new(
            RoomType.Retail,
            category,
            HeatingSetpoint(category),
            CoolingSetpoint(category),
            OutdoorAirPerPerson(category),
            OccupancyFactors:
            [
                0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.02, 0.05, 0.10, 0.35, 0.65, 0.85,
                0.95, 1.00, 1.00, 1.00, 0.95, 0.90, 0.80, 0.60, 0.30, 0.10, 0.02, 0.00
            ],
            EquipmentFactors:
            [
                0.10, 0.10, 0.10, 0.10, 0.10, 0.10, 0.12, 0.15, 0.25, 0.45, 0.70, 0.90,
                1.00, 1.00, 1.00, 1.00, 0.95, 0.90, 0.80, 0.60, 0.35, 0.15, 0.10, 0.10
            ],
            LightingFactors:
            [
                0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.20, 0.25, 0.40, 0.60, 0.80, 0.95,
                1.00, 1.00, 1.00, 1.00, 1.00, 0.95, 0.85, 0.70, 0.45, 0.25, 0.15, 0.15
            ],
            VentilationFactors:
            [
                0.10, 0.10, 0.10, 0.10, 0.10, 0.10, 0.15, 0.20, 0.35, 0.55, 0.75, 0.90,
                1.00, 1.00, 1.00, 1.00, 0.95, 0.90, 0.80, 0.65, 0.40, 0.20, 0.10, 0.10
            ]);

    private static En16798RoomUsageProfile CreateResidential(En16798ProfileCategory category) =>
        new(
            RoomType.Residential,
            category,
            HeatingSetpoint(category),
            CoolingSetpoint(category) + 0.5,
            OutdoorAirPerPerson(category) * 0.7,
            OccupancyFactors:
            [
                0.95, 0.95, 0.95, 0.95, 0.95, 0.85, 0.65, 0.40, 0.20, 0.10, 0.10, 0.10,
                0.15, 0.20, 0.25, 0.35, 0.55, 0.80, 0.95, 1.00, 1.00, 1.00, 1.00, 0.98
            ],
            EquipmentFactors:
            [
                0.35, 0.30, 0.25, 0.20, 0.20, 0.30, 0.45, 0.55, 0.45, 0.30, 0.25, 0.25,
                0.30, 0.35, 0.40, 0.45, 0.60, 0.80, 0.95, 1.00, 1.00, 0.90, 0.75, 0.55
            ],
            LightingFactors:
            [
                0.30, 0.25, 0.20, 0.15, 0.15, 0.25, 0.35, 0.30, 0.15, 0.10, 0.10, 0.10,
                0.10, 0.10, 0.12, 0.20, 0.35, 0.55, 0.80, 1.00, 1.00, 0.95, 0.75, 0.50
            ],
            VentilationFactors:
            [
                0.50, 0.45, 0.40, 0.35, 0.35, 0.45, 0.55, 0.45, 0.30, 0.25, 0.25, 0.25,
                0.30, 0.30, 0.35, 0.40, 0.50, 0.65, 0.80, 0.95, 0.95, 0.85, 0.70, 0.60
            ]);

    private static En16798RoomUsageProfile CreateOther(En16798ProfileCategory category) =>
        CreateOffice(category) with { RoomType = RoomType.Other };

    private static double HeatingSetpoint(En16798ProfileCategory category) => category switch
    {
        En16798ProfileCategory.I => 21.0,
        En16798ProfileCategory.II => 20.0,
        En16798ProfileCategory.III => 20.0,
        En16798ProfileCategory.IV => 19.0,
        _ => 20.0
    };

    private static double CoolingSetpoint(En16798ProfileCategory category) => category switch
    {
        En16798ProfileCategory.I => 25.0,
        En16798ProfileCategory.II => 26.0,
        En16798ProfileCategory.III => 27.0,
        En16798ProfileCategory.IV => 28.0,
        _ => 26.0
    };

    private static double OutdoorAirPerPerson(En16798ProfileCategory category) => category switch
    {
        En16798ProfileCategory.I => 14.0,
        En16798ProfileCategory.II => 10.0,
        En16798ProfileCategory.III => 7.0,
        En16798ProfileCategory.IV => 4.0,
        _ => 10.0
    };
}