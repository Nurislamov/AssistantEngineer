namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.OAuth;

public sealed record GreeAliceYandexOAuthAuthorizationCode(
    string Code,
    string ClientId,
    string RedirectUri,
    string BridgeAccountId,
    string MaskedYandexUserId,
    string? Scope,
    string? State,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? ConsumedAtUtc)
{
    public bool IsExpired(DateTimeOffset utcNow) => ExpiresAtUtc <= utcNow;
    public bool IsConsumed => ConsumedAtUtc is not null;
}
