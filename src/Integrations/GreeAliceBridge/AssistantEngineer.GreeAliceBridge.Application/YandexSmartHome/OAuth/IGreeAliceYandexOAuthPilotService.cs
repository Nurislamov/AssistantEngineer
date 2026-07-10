using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.OAuth;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.OAuth;

public interface IGreeAliceYandexOAuthPilotService
{
    GreeAliceYandexOAuthAuthorizationCode Authorize(
        GreeAliceYandexOAuthAuthorizationRequest request,
        GreeAliceYandexOAuthOptions options,
        DateTimeOffset utcNow);

    GreeAliceYandexOAuthTokenResponse ExchangeCode(
        GreeAliceYandexOAuthTokenRequest request,
        GreeAliceYandexOAuthOptions options,
        DateTimeOffset utcNow);
}
