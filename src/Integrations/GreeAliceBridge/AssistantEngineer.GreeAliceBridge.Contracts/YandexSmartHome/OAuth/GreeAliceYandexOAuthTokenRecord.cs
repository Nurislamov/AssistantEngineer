namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.OAuth;

public sealed record GreeAliceYandexOAuthTokenRecord(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset AccessTokenExpiresAtUtc,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc,
    string BridgeAccountId,
    string MaskedYandexUserId,
    string? Scope,
    string ClientIdReference,
    string RedirectUri,
    string? NonceOrState)
{
    public bool IsExpired(DateTimeOffset utcNow) => AccessTokenExpiresAtUtc <= utcNow;
    public bool IsRevoked => RevokedAtUtc is not null;
}
