using System.Text.Json.Serialization;

namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.OAuth;

public sealed record GreeAliceYandexOAuthTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn);
