namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.OAuth;

public sealed record GreeAliceYandexOAuthRequestContext(
    string? XRequestId,
    string Source,
    bool ContainsBearerToken);
