namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.OAuth;

public sealed record GreeAliceYandexOAuthOptions(
    string PilotMode,
    string ProviderMode,
    string PublicBaseUrl,
    string ClientId,
    string SharedSecret,
    IReadOnlyList<string> AllowedRedirectUris,
    int AuthorizationCodeLifetimeSeconds,
    int AccessTokenLifetimeSeconds,
    int RefreshTokenLifetimeSeconds,
    bool RequireHttpsPublicBaseUrl,
    bool EnableDevOnlyInMemoryTokenStore,
    string DevOnlyBridgeAccountId,
    string DevOnlyMaskedYandexUserId)
{
    public static GreeAliceYandexOAuthOptions DevOnlyDefaults { get; } = new(
        "PrivateSkillDevOnly",
        GreeAliceYandexOAuthPilotBoundary.ProviderMode,
        "http://localhost:5005",
        "dev-yandex-client",
        "dev-yandex-client-secret",
        ["http://localhost:5005/oauth/callback"],
        GreeAliceYandexOAuthPilotBoundary.DefaultAuthorizationCodeLifetimeSeconds,
        GreeAliceYandexOAuthPilotBoundary.DefaultAccessTokenLifetimeSeconds,
        GreeAliceYandexOAuthPilotBoundary.DefaultRefreshTokenLifetimeSeconds,
        RequireHttpsPublicBaseUrl: false,
        EnableDevOnlyInMemoryTokenStore: true,
        "dummy-account-001",
        "masked-yandex-user-dev-001");

    public bool RequiresBearerForProviderEndpoints =>
        string.Equals(PilotMode, "PrivateSkillDevOnly", StringComparison.Ordinal);
}
