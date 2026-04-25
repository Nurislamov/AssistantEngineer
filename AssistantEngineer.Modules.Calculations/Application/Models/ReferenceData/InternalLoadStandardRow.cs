using AssistantEngineer.Modules.Buildings.Domain.Enums;

namespace AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;

public sealed record InternalLoadStandardRow(
    string TableKey,
    string Version,
    RoomType RoomType,
    double SensibleHeatGainPerPersonW,
    double LatentHeatGainPerPersonW,
    double EquipmentGainWPerM2,
    double LightingGainWPerM2,
    double MinimumVentilationLitersPerSecondM2,
    double OccupantDensityPeoplePer100M2,
    string Notes);