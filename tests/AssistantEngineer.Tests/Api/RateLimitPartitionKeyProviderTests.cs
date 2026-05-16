using System.Net;
using System.Security.Cryptography;
using System.Text;
using AssistantEngineer.Api.Options.Security;
using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Api.Security.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Api;

public sealed class RateLimitPartitionKeyProviderTests
{
    [Fact]
    public void OrganizationId_WinsOverUserId()
    {
        var provider = CreateProvider(new AuthenticatedPrincipal(
            UserId: 42,
            OrganizationId: 77,
            ExternalSubjectId: "subject",
            AuthenticationScheme: "ApiKey",
            Roles: new HashSet<string>(),
            Permissions: new HashSet<string>(),
            IsAuthenticated: true));
        var context = CreateHttpContext(apiKey: "secret-key", ipAddress: "203.0.113.10");

        var partition = provider.GetPartitionKey(context);

        Assert.Equal(RateLimitPartitionTypes.OrganizationId, partition.PartitionType);
        Assert.Equal("77", partition.PartitionValue);
    }

    [Fact]
    public void UserId_WinsOverApiKeyAndIp()
    {
        var provider = CreateProvider(new AuthenticatedPrincipal(
            UserId: 42,
            OrganizationId: null,
            ExternalSubjectId: "subject",
            AuthenticationScheme: "ApiKey",
            Roles: new HashSet<string>(),
            Permissions: new HashSet<string>(),
            IsAuthenticated: true));
        var context = CreateHttpContext(apiKey: "secret-key", ipAddress: "203.0.113.10");

        var partition = provider.GetPartitionKey(context);

        Assert.Equal(RateLimitPartitionTypes.UserId, partition.PartitionType);
        Assert.Equal("42", partition.PartitionValue);
    }

    [Fact]
    public void ApiKeyFingerprint_WinsOverIp_WhenNoUserOrOrganization()
    {
        var provider = CreateProvider(AuthenticatedPrincipal.Anonymous);
        var context = CreateHttpContext(apiKey: "secret-key", ipAddress: "203.0.113.10");

        var partition = provider.GetPartitionKey(context);

        Assert.Equal(RateLimitPartitionTypes.ApiKeyFingerprint, partition.PartitionType);
        Assert.Equal(CreateSha256("secret-key"), partition.PartitionValue);
    }

    [Fact]
    public void IpAddress_IsFallback_WhenNoPrincipalOrApiKey()
    {
        var provider = CreateProvider(AuthenticatedPrincipal.Anonymous);
        var context = CreateHttpContext(apiKey: null, ipAddress: "203.0.113.10");

        var partition = provider.GetPartitionKey(context);

        Assert.Equal(RateLimitPartitionTypes.IpAddress, partition.PartitionType);
        Assert.Equal("203.0.113.10", partition.PartitionValue);
    }

    [Fact]
    public void AnonymousUnknown_IsFallback_WhenNoPrincipalApiKeyOrIp()
    {
        var provider = CreateProvider(AuthenticatedPrincipal.Anonymous);
        var context = new DefaultHttpContext();

        var partition = provider.GetPartitionKey(context);

        Assert.Equal(RateLimitPartitionTypes.AnonymousUnknown, partition.PartitionType);
        Assert.Equal("anonymous-unknown", partition.PartitionValue);
    }

    [Fact]
    public void RawApiKey_IsNeverUsedAsPartitionValue()
    {
        var provider = CreateProvider(AuthenticatedPrincipal.Anonymous);
        var rawApiKey = "raw-top-secret-key";
        var context = CreateHttpContext(apiKey: rawApiKey, ipAddress: "203.0.113.10");

        var partition = provider.GetPartitionKey(context);

        Assert.Equal(RateLimitPartitionTypes.ApiKeyFingerprint, partition.PartitionType);
        Assert.NotEqual(rawApiKey, partition.PartitionValue);
        Assert.DoesNotContain(rawApiKey, partition.PartitionValue, StringComparison.Ordinal);
    }

    private static IRateLimitPartitionKeyProvider CreateProvider(AuthenticatedPrincipal principal)
    {
        return new DefaultRateLimitPartitionKeyProvider(
            new StubPrincipalProvider(principal),
            new StaticOptionsMonitor<ApiRateLimitingOptions>(new ApiRateLimitingOptions()),
            new StaticOptionsMonitor<ApiAuthenticationOptions>(new ApiAuthenticationOptions()));
    }

    private static HttpContext CreateHttpContext(string? apiKey, string ipAddress)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            context.Request.Headers[ApiAuthenticationOptions.DefaultApiKeyHeaderName] = apiKey;
        }

        return context;
    }

    private static string CreateSha256(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash).ToLowerInvariant();
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
}
