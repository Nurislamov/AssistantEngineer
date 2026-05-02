namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016HourlyRoomInternalGainRecord(
    int HourOfYear,
    double OccupancyFactor,
    double EquipmentFactor,
    double LightingFactor,
    double PeopleGainW,
    double EquipmentGainW,
    double LightingGainW,
    double TotalInternalGainW);