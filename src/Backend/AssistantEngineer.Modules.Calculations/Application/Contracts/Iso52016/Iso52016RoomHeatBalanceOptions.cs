namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016RoomHeatBalanceOptions(
    double InitialIndoorTemperatureC = 20.0,
    double TimeStepSeconds = 3600.0);