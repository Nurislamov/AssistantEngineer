namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;

public sealed record GreeAliceRegistryScopeBinding(
    string YandexUserReference,
    string BridgeAccountReference,
    string RegistryScopeReference,
    IReadOnlyList<string> AllowedDeviceIds,
    string Status);
