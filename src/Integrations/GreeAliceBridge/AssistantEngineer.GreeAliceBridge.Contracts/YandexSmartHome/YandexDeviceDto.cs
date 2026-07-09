namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

public sealed record YandexDeviceDto(
    string Id,
    string Name,
    string Room,
    string Type,
    IReadOnlyList<YandexDeviceCapabilityDto> Capabilities,
    bool Online,
    string Source);
