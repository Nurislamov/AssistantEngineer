using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.OAuth;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.OAuth;

public sealed class DevOnlyGreeAliceYandexBearerTokenValidator(
    IGreeAliceYandexOAuthTokenStore tokenStore) : IGreeAliceYandexBearerTokenValidator
{
    public GreeAliceYandexOAuthTokenValidationResult Validate(string? authorizationHeader, DateTimeOffset utcNow)
    {
        const string prefix = "Bearer ";
        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith(prefix, StringComparison.Ordinal))
        {
            return GreeAliceYandexOAuthTokenValidationResult.Invalid("missing-bearer");
        }

        string bearerValue = authorizationHeader[prefix.Length..].Trim();

        return tokenStore.ValidateAccessToken(bearerValue, utcNow);
    }
}
