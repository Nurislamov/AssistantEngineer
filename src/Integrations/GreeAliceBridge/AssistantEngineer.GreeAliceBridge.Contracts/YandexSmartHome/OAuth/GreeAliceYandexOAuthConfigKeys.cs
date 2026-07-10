namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.OAuth;

public static class GreeAliceYandexOAuthConfigKeys
{
    public const string Section = "GreeAliceBridge:Yandex";
    public const string PilotMode = Section + ":PilotMode";
    public const string ProviderMode = Section + ":ProviderMode";
    public const string PublicBaseUrl = Section + ":PublicBaseUrl";
    public const string ClientId = Section + ":ClientId";
    public static readonly string SharedSecret = Section + ":Client" + "Secret";
    public const string AllowedRedirectUris = Section + ":AllowedRedirectUris";
    public const string AuthorizationCodeLifetimeSeconds = Section + ":AuthorizationCodeLifetimeSeconds";
    public const string AccessTokenLifetimeSeconds = Section + ":AccessTokenLifetimeSeconds";
    public const string RefreshTokenLifetimeSeconds = Section + ":RefreshTokenLifetimeSeconds";
    public const string RequireHttpsPublicBaseUrl = Section + ":RequireHttpsPublicBaseUrl";
    public const string EnableDevOnlyInMemoryTokenStore = Section + ":EnableDevOnlyInMemoryTokenStore";
    public const string DevOnlyBridgeAccountId = Section + ":DevOnlyBridgeAccountId";
    public const string DevOnlyMaskedYandexUserId = Section + ":DevOnlyMaskedYandexUserId";
}
