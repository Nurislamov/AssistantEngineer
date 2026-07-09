namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

public sealed record YandexActionCapabilityRequestDto(
    string Type,
    string Action,
    string? Value);
