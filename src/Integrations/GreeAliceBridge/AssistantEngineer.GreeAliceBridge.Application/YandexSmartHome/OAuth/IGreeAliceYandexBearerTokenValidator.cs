using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.OAuth;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.OAuth;

public interface IGreeAliceYandexBearerTokenValidator
{
    GreeAliceYandexOAuthTokenValidationResult Validate(string? authorizationHeader, DateTimeOffset utcNow);
}
