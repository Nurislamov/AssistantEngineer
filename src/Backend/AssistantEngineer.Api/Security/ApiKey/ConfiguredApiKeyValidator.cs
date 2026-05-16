using System.Security.Cryptography;
using System.Text;
using AssistantEngineer.Api.Security.Authentication;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Security.ApiKey;

public sealed class ConfiguredApiKeyValidator : IApiKeyValidator
{
    private readonly IOptionsMonitor<ApiKeyAuthenticationSettings> _settingsMonitor;

    public ConfiguredApiKeyValidator(IOptionsMonitor<ApiKeyAuthenticationSettings> settingsMonitor)
    {
        _settingsMonitor = settingsMonitor;
    }

    public Task<ApiKeyValidationResult> ValidateAsync(
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Task.FromResult(ApiKeyValidationResult.Failure("MissingApiKey"));
        }

        var settings = _settingsMonitor.CurrentValue;
        if (string.IsNullOrWhiteSpace(settings.Key))
        {
            return Task.FromResult(ApiKeyValidationResult.Failure("ApiKeyNotConfigured"));
        }

        if (!FixedTimeEquals(apiKey, settings.Key))
        {
            return Task.FromResult(ApiKeyValidationResult.Failure("InvalidApiKey"));
        }

        var principal = new AuthenticatedPrincipal(
            UserId: null,
            OrganizationId: null,
            ExternalSubjectId: "api-key-client",
            AuthenticationScheme: ApiKeyAuthenticationHandler.SchemeName,
            Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            Permissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            IsAuthenticated: true);

        return Task.FromResult(ApiKeyValidationResult.Success(principal));
    }

    private static bool FixedTimeEquals(
        string submittedKey,
        string expectedKey)
    {
        var submittedHash = SHA256.HashData(Encoding.UTF8.GetBytes(submittedKey));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expectedKey));

        return CryptographicOperations.FixedTimeEquals(submittedHash, expectedHash);
    }
}
