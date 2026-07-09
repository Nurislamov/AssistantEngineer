namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

public sealed record YandexQueryRequest(IReadOnlyList<YandexDeviceRequestDto> Devices);
