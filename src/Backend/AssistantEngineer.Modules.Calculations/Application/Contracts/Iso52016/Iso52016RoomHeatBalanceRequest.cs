namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016RoomHeatBalanceRequest(
    Iso52016RoomHourlyInputProfile InputProfile,
    Iso52016RoomHeatBalanceOptions? Options = null);