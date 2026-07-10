using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.OAuth;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.OAuth;

public interface IGreeAliceYandexOAuthTokenStore
{
    GreeAliceYandexOAuthTokenRecord Issue(
        GreeAliceYandexOAuthAuthorizationCode code,
        GreeAliceYandexOAuthOptions options,
        DateTimeOffset utcNow);

    GreeAliceYandexOAuthTokenValidationResult ValidateAccessToken(string token, DateTimeOffset utcNow);

    bool RevokeByAccessToken(string token, DateTimeOffset utcNow);

    int RevokeByBridgeAccountId(string bridgeAccountId, DateTimeOffset utcNow);
}
