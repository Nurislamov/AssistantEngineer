namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016RoomInternalGainProfileRequest(
    string RoomCode,
    int HourCount,
    int PeopleCount,
    double SensibleHeatGainPerPersonW,
    double EquipmentLoadW,
    double LightingLoadW,
    IReadOnlyList<double> OccupancyFactors,
    IReadOnlyList<double> EquipmentFactors,
    IReadOnlyList<double> LightingFactors);