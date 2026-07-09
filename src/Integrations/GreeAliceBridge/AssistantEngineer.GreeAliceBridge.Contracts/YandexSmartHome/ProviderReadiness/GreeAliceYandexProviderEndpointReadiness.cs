namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.ProviderReadiness;

public sealed record GreeAliceYandexProviderEndpointReadiness(
    string EndpointGroup,
    string Method,
    string Path,
    string Status,
    bool IsImplemented,
    bool IsProductionReady);
