using AssistantEngineer.Modules.Buildings.Domain.Enums;

namespace AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;

public sealed record DomesticHotWaterStandardRow(
    string TableKey,
    string Version,
    RoomType RoomType,
    double LitersPerPersonDay,
    double ColdWaterTemperatureC,
    double HotWaterTemperatureC,
    double DistributionLossFactor,
    double StorageLossKWhPerDay,
    double CirculationLossKWhPerDay,
    string Notes);