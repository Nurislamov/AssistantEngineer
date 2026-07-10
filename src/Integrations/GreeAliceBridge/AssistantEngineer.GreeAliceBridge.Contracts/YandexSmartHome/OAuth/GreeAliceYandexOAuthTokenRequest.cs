namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.OAuth;

public sealed record GreeAliceYandexOAuthTokenRequest(
    string GrantType,
    string Code,
    string ClientId,
    string SharedSecret,
    string RedirectUri);
