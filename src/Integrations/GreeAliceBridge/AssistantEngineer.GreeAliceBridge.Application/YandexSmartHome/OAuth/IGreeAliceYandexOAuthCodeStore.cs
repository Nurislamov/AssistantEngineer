using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.OAuth;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.OAuth;

public interface IGreeAliceYandexOAuthCodeStore
{
    GreeAliceYandexOAuthAuthorizationCode Create(
        GreeAliceYandexOAuthAuthorizationRequest request,
        GreeAliceYandexOAuthOptions options,
        DateTimeOffset utcNow);

    GreeAliceYandexOAuthAuthorizationCode? Consume(
        string code,
        string clientId,
        string redirectUri,
        DateTimeOffset utcNow);
}
