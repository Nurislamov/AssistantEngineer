using System.Collections.Concurrent;
using System.Security.Cryptography;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.OAuth;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.OAuth;

public sealed class InMemoryGreeAliceYandexOAuthTokenStore : IGreeAliceYandexOAuthTokenStore
{
    private readonly ConcurrentDictionary<string, GreeAliceYandexOAuthTokenRecord> records = new(StringComparer.Ordinal);

    public GreeAliceYandexOAuthTokenRecord Issue(
        GreeAliceYandexOAuthAuthorizationCode code,
        GreeAliceYandexOAuthOptions options,
        DateTimeOffset utcNow)
    {
        GreeAliceYandexOAuthTokenRecord record = new(
            CreateOpaqueValue("ga-at"),
            CreateOpaqueValue("ga-rt"),
            utcNow,
            utcNow.AddSeconds(options.AccessTokenLifetimeSeconds),
            utcNow.AddSeconds(options.RefreshTokenLifetimeSeconds),
            RevokedAtUtc: null,
            code.BridgeAccountId,
            code.MaskedYandexUserId,
            code.Scope,
            code.ClientId,
            code.RedirectUri,
            code.State);

        records[record.AccessToken] = record;

        return record;
    }

    public GreeAliceYandexOAuthTokenValidationResult ValidateAccessToken(string token, DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            token.Length > GreeAliceYandexOAuthPilotBoundary.MaxTokenLength ||
            !records.TryGetValue(token, out GreeAliceYandexOAuthTokenRecord? record))
        {
            return GreeAliceYandexOAuthTokenValidationResult.Invalid("unknown-token");
        }

        if (record.IsRevoked)
        {
            return GreeAliceYandexOAuthTokenValidationResult.Invalid("revoked-token");
        }

        return record.IsExpired(utcNow)
            ? GreeAliceYandexOAuthTokenValidationResult.Invalid("expired-token")
            : GreeAliceYandexOAuthTokenValidationResult.Valid(record);
    }

    public bool RevokeByAccessToken(string token, DateTimeOffset utcNow)
    {
        if (!records.TryGetValue(token, out GreeAliceYandexOAuthTokenRecord? record))
        {
            return false;
        }

        return records.TryUpdate(token, record with { RevokedAtUtc = utcNow }, record);
    }

    public int RevokeByBridgeAccountId(string bridgeAccountId, DateTimeOffset utcNow)
    {
        int revoked = 0;
        foreach (KeyValuePair<string, GreeAliceYandexOAuthTokenRecord> item in records)
        {
            GreeAliceYandexOAuthTokenRecord record = item.Value;
            if (record.IsRevoked ||
                !string.Equals(record.BridgeAccountId, bridgeAccountId, StringComparison.Ordinal))
            {
                continue;
            }

            if (records.TryUpdate(item.Key, record with { RevokedAtUtc = utcNow }, record))
            {
                revoked++;
            }
        }

        return revoked;
    }

    private static string CreateOpaqueValue(string prefix)
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(48);
        return prefix + "-" + Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
