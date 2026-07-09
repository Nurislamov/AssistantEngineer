namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

public sealed record YandexActionDeviceRequestDto(
    string Id,
    IReadOnlyList<YandexActionCapabilityRequestDto> Capabilities);
