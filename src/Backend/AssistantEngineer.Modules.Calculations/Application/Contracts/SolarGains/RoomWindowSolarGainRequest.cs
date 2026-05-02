namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SolarGains;

public sealed record RoomWindowSolarGainRequest(
    int RoomId,
    IReadOnlyList<WindowSolarGainInput> Windows);
