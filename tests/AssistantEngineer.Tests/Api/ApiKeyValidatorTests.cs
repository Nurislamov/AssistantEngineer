using AssistantEngineer.Api.Security.ApiKey;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Api;

public sealed class ApiKeyValidatorTests
{
    [Fact]
    public async Task EmptyApiKey_IsInvalid()
    {
        var validator = CreateValidator(enabled: true, key: "configured-key");

        var result = await validator.ValidateAsync(string.Empty);

        Assert.False(result.IsValid);
        Assert.Equal("MissingApiKey", result.FailureReasonCode);
    }

    [Fact]
    public async Task ConfiguredKey_IsAccepted()
    {
        var validator = CreateValidator(enabled: true, key: "configured-key");

        var result = await validator.ValidateAsync("configured-key");

        Assert.True(result.IsValid);
        Assert.NotNull(result.Principal);
        Assert.True(result.Principal!.IsAuthenticated);
    }

    [Fact]
    public async Task InvalidKey_IsRejected()
    {
        var validator = CreateValidator(enabled: true, key: "configured-key");

        var result = await validator.ValidateAsync("invalid-key");

        Assert.False(result.IsValid);
        Assert.Equal("InvalidApiKey", result.FailureReasonCode);
    }

    [Fact]
    public async Task MissingConfiguredKey_IsRejected()
    {
        var validator = CreateValidator(enabled: true, key: null);

        var result = await validator.ValidateAsync("candidate");

        Assert.False(result.IsValid);
        Assert.Equal("ApiKeyNotConfigured", result.FailureReasonCode);
    }

    private static ConfiguredApiKeyValidator CreateValidator(
        bool enabled,
        string? key)
    {
        var options = new ApiKeyAuthenticationSettings
        {
            Enabled = enabled,
            HeaderName = ApiKeyAuthenticationSettings.DefaultHeaderName,
            Key = key
        };

        return new ConfiguredApiKeyValidator(new StaticOptionsMonitor<ApiKeyAuthenticationSettings>(options));
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
}
