using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

public sealed record Iso52016V2ReducedRoomModelRequest(
    Iso52016RoomHourlyInputProfile HourlyInputProfile,
    Iso52016RoomHeatBalanceOptions? HeatBalanceOptions = null,
    Iso52016V2ReducedRoomModelOptions? ModelOptions = null);