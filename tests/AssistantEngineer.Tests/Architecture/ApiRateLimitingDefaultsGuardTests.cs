using System.Net;
using System.Text.Json;
using AssistantEngineer.Api.Options.Security;
using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Api.Security.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Architecture;

public sealed class ApiRateLimitingDefaultsGuardTests
{
    [Fact]
    public void AppsettingsKeepApiRateLimitingCompatibilityDefaults()
    {
        using var production = JsonDocument.Parse(File.ReadAllText(AppSettingsPath));
        using var development = JsonDocument.Parse(File.ReadAllText(AppSettingsDevelopmentPath));

        Assert.True(production.RootElement.TryGetProperty("ApiRateLimiting", out var productionSection));
        Assert.True(development.RootElement.TryGetProperty("ApiRateLimiting", out var developmentSection));

        Assert.False(productionSection.GetProperty("Enabled").GetBoolean());
        Assert.False(developmentSection.GetProperty("Enabled").GetBoolean());
    }

    [Fact]
    public void RateLimitingPolicyRegistryContainsNonClaims()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(RateLimitingPolicyRegistryPath));
        Assert.True(document.RootElement.TryGetProperty("nonClaims", out var nonClaims));
        Assert.Equal(JsonValueKind.Array, nonClaims.ValueKind);
        Assert.True(nonClaims.GetArrayLength() > 0);
    }

    [Fact]
    public void PartitionKeyProviderNeverUsesRawApiKey()
    {
        var provider = new DefaultRateLimitPartitionKeyProvider(
            new StubPrincipalProvider(AuthenticatedPrincipal.Anonymous),
            new StaticOptionsMonitor<ApiRateLimitingOptions>(new ApiRateLimitingOptions
            {
                Enabled = true,
                UseApiKeyFingerprintPartitioning = true
            }),
            new StaticOptionsMonitor<ApiAuthenticationOptions>(new ApiAuthenticationOptions
            {
                Enabled = true,
                ApiKeyHeaderName = ApiAuthenticationOptions.DefaultApiKeyHeaderName
            }));

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");
        context.Request.Headers[ApiAuthenticationOptions.DefaultApiKeyHeaderName] = "raw-test-api-key-value";

        var partition = provider.GetPartitionKey(context);

        Assert.Equal(RateLimitPartitionTypes.ApiKeyFingerprint, partition.PartitionType);
        Assert.NotEqual("raw-test-api-key-value", partition.PartitionValue);
        Assert.DoesNotContain("raw-test-api-key-value", partition.PartitionValue, StringComparison.Ordinal);
    }

    private sealed class StubPrincipalProvider : IAuthenticatedPrincipalProvider
    {
        private readonly AuthenticatedPrincipal _principal;

        public StubPrincipalProvider(AuthenticatedPrincipal principal)
        {
            _principal = principal;
        }

        public AuthenticatedPrincipal GetCurrentPrincipal() => _principal;
    }

    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T> where T : class
    {
        public StaticOptionsMonitor(T value)
        {
            CurrentValue = value;
        }

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private static string AppSettingsPath =>
        Path.Combine(TestPaths.ApiProjectPath, "appsettings.json");

    private static string AppSettingsDevelopmentPath =>
        Path.Combine(TestPaths.ApiProjectPath, "appsettings.Development.json");

    private static string RateLimitingPolicyRegistryPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "rate-limiting-policy-registry.json");
}
