namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

public sealed record YandexActionResponse(IReadOnlyList<YandexActionDeviceResultDto> Devices);
