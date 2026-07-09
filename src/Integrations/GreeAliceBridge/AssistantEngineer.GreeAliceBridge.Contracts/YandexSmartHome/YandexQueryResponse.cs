namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

public sealed record YandexQueryResponse(IReadOnlyList<YandexQueryDeviceDto> Devices);
