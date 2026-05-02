namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Aggregation;

public sealed record LoadAggregationRoomBreakdown(
    int RoomId,
    string RoomName,
    double AreaM2,
    double HeatingLoadW,
    double CoolingLoadW);
