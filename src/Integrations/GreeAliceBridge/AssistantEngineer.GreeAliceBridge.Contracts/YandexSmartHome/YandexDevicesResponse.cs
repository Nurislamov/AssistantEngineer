namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

public sealed record YandexDevicesResponse(IReadOnlyList<YandexDeviceDto> Devices);
