namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016RoomSolarGainProfileRequest(
    string RoomCode,
    Iso52016WeatherSolarContext WeatherSolarContext,
    IReadOnlyList<Iso52016WindowSolarGainInput> Windows);