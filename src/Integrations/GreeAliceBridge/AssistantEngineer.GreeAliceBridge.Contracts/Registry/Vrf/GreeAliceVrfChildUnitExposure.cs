namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry.Vrf;

public sealed record GreeAliceVrfChildUnitExposure(
    string ChildUnitId,
    string StableYandexDeviceId,
    bool ExposeToYandex,
    string DeviceKind,
    string RuntimeMode);
