namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

public sealed record YandexActionRequest(IReadOnlyList<YandexActionDeviceRequestDto> Devices);
