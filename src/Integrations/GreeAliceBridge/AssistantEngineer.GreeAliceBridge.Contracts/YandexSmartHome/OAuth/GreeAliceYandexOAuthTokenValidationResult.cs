namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.OAuth;

public sealed record GreeAliceYandexOAuthTokenValidationResult(
    bool IsValid,
    string Status,
    GreeAliceYandexOAuthTokenRecord? TokenRecord)
{
    public static GreeAliceYandexOAuthTokenValidationResult Invalid(string status) => new(false, status, null);
    public static GreeAliceYandexOAuthTokenValidationResult Valid(GreeAliceYandexOAuthTokenRecord record) => new(true, "valid", record);
}
