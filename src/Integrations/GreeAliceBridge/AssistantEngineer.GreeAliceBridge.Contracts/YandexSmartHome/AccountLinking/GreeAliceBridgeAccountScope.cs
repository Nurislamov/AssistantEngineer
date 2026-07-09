namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;

public sealed record GreeAliceBridgeAccountScope(
    string BridgeAccountReference,
    string RegistryScopeReference,
    IReadOnlyList<string> AllowedHomeIds,
    IReadOnlyList<string> AllowedDeviceIds,
    IReadOnlyList<string> AllowedVrfGatewayIds,
    IReadOnlyList<string> AllowedVrfChildUnitIds,
    bool IsMasked,
    bool IsDummyOrTemplate);
