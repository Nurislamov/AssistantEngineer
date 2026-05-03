using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;

public sealed record Iso52016MatrixReducedRoomModelRequest(
    Iso52016RoomHourlyInputProfile HourlyInputProfile,
    Iso52016RoomHeatBalanceOptions? HeatBalanceOptions = null,
    Iso52016MatrixReducedRoomModelOptions? ModelOptions = null);