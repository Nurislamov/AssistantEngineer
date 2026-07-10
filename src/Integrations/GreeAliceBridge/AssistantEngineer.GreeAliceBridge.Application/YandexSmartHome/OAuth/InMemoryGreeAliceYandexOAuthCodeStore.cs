using System.Collections.Concurrent;
using System.Security.Cryptography;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.OAuth;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.OAuth;

public sealed class InMemoryGreeAliceYandexOAuthCodeStore : IGreeAliceYandexOAuthCodeStore
{
    private readonly ConcurrentDictionary<string, GreeAliceYandexOAuthAuthorizationCode> codes = new(StringComparer.Ordinal);

    public GreeAliceYandexOAuthAuthorizationCode Create(
        GreeAliceYandexOAuthAuthorizationRequest request,
        GreeAliceYandexOAuthOptions options,
        DateTimeOffset utcNow)
    {
        string code = CreateOpaqueValue("ga-code");
        GreeAliceYandexOAuthAuthorizationCode record = new(
            code,
            request.ClientId,
            request.RedirectUri,
            options.DevOnlyBridgeAccountId,
            options.DevOnlyMaskedYandexUserId,
            request.Scope,
            request.State,
            utcNow,
            utcNow.AddSeconds(options.AuthorizationCodeLifetimeSeconds),
            ConsumedAtUtc: null);

        codes[code] = record;

        return record;
    }

    public GreeAliceYandexOAuthAuthorizationCode? Consume(
        string code,
        string clientId,
        string redirectUri,
        DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(code) ||
            !codes.TryGetValue(code, out GreeAliceYandexOAuthAuthorizationCode? record) ||
            record.IsExpired(utcNow) ||
            record.IsConsumed ||
            !string.Equals(record.ClientId, clientId, StringComparison.Ordinal) ||
            !string.Equals(record.RedirectUri, redirectUri, StringComparison.Ordinal))
        {
            return null;
        }

        GreeAliceYandexOAuthAuthorizationCode consumed = record with { ConsumedAtUtc = utcNow };
        return codes.TryUpdate(code, consumed, record) ? consumed : null;
    }

    private static string CreateOpaqueValue(string prefix)
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        return prefix + "-" + Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
