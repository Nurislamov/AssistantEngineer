using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.OAuth;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.OAuth;

public sealed class DevOnlyGreeAliceYandexOAuthPilotService(
    IGreeAliceYandexOAuthCodeStore codeStore,
    IGreeAliceYandexOAuthTokenStore tokenStore) : IGreeAliceYandexOAuthPilotService
{
    public GreeAliceYandexOAuthAuthorizationCode Authorize(
        GreeAliceYandexOAuthAuthorizationRequest request,
        GreeAliceYandexOAuthOptions options,
        DateTimeOffset utcNow)
    {
        if (!string.Equals(request.ResponseType, "code", StringComparison.Ordinal) ||
            !string.Equals(request.ClientId, options.ClientId, StringComparison.Ordinal) ||
            !IsRedirectUriAllowed(request.RedirectUri, options))
        {
            throw new InvalidOperationException("authorization-request-rejected");
        }

        return codeStore.Create(request, options, utcNow);
    }

    public GreeAliceYandexOAuthTokenResponse ExchangeCode(
        GreeAliceYandexOAuthTokenRequest request,
        GreeAliceYandexOAuthOptions options,
        DateTimeOffset utcNow)
    {
        if (!string.Equals(request.GrantType, "authorization_code", StringComparison.Ordinal) ||
            !string.Equals(request.ClientId, options.ClientId, StringComparison.Ordinal) ||
            !string.Equals(request.SharedSecret, options.SharedSecret, StringComparison.Ordinal) ||
            !IsRedirectUriAllowed(request.RedirectUri, options))
        {
            throw new InvalidOperationException("token-request-rejected");
        }

        GreeAliceYandexOAuthAuthorizationCode? code = codeStore.Consume(
            request.Code,
            request.ClientId,
            request.RedirectUri,
            utcNow);

        if (code is null)
        {
            throw new InvalidOperationException("authorization-code-rejected");
        }

        GreeAliceYandexOAuthTokenRecord record = tokenStore.Issue(code, options, utcNow);

        return new GreeAliceYandexOAuthTokenResponse(
            record.AccessToken,
            record.RefreshToken,
            "Bearer",
            options.AccessTokenLifetimeSeconds);
    }

    private static bool IsRedirectUriAllowed(string redirectUri, GreeAliceYandexOAuthOptions options)
    {
        return options.AllowedRedirectUris.Any(allowed =>
            string.Equals(allowed, redirectUri, StringComparison.Ordinal));
    }
}
