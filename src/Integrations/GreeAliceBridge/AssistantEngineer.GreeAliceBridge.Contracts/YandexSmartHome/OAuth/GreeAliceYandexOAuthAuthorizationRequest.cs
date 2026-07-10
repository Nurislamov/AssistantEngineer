namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.OAuth;

public sealed record GreeAliceYandexOAuthAuthorizationRequest(
    string ResponseType,
    string ClientId,
    string RedirectUri,
    string? State,
    string? Scope);
