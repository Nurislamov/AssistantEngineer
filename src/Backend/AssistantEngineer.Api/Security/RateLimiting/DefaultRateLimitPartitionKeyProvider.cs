using System.Security.Cryptography;
using System.Text;
using AssistantEngineer.Api.Options.Security;
using AssistantEngineer.Api.Security.Authentication;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Security.RateLimiting;

public sealed class DefaultRateLimitPartitionKeyProvider : IRateLimitPartitionKeyProvider
{
    private readonly IAuthenticatedPrincipalProvider _principalProvider;
    private readonly IOptionsMonitor<ApiRateLimitingOptions> _rateLimitingOptions;
    private readonly IOptionsMonitor<ApiAuthenticationOptions> _authenticationOptions;

    public DefaultRateLimitPartitionKeyProvider(
        IAuthenticatedPrincipalProvider principalProvider,
        IOptionsMonitor<ApiRateLimitingOptions> rateLimitingOptions,
        IOptionsMonitor<ApiAuthenticationOptions> authenticationOptions)
    {
        _principalProvider = principalProvider;
        _rateLimitingOptions = rateLimitingOptions;
        _authenticationOptions = authenticationOptions;
    }

    public RateLimitPartitionKey GetPartitionKey(HttpContext httpContext)
    {
        var options = _rateLimitingOptions.CurrentValue;
        var principal = _principalProvider.GetCurrentPrincipal();

        if (options.UseOrganizationPartitioning &&
            principal.IsAuthenticated &&
            principal.OrganizationId.HasValue)
        {
            var organizationId = principal.OrganizationId.Value.ToString();
            return new RateLimitPartitionKey(
                PartitionType: RateLimitPartitionTypes.OrganizationId,
                PartitionValue: organizationId,
                SafeDisplayValue: organizationId);
        }

        if (options.UseAuthenticatedPrincipalPartitioning &&
            principal.IsAuthenticated &&
            principal.UserId.HasValue)
        {
            var userId = principal.UserId.Value.ToString();
            return new RateLimitPartitionKey(
                PartitionType: RateLimitPartitionTypes.UserId,
                PartitionValue: userId,
                SafeDisplayValue: userId);
        }

        if (options.UseApiKeyFingerprintPartitioning &&
            TryResolveApiKeyFingerprint(httpContext, out var fingerprint))
        {
            return new RateLimitPartitionKey(
                PartitionType: RateLimitPartitionTypes.ApiKeyFingerprint,
                PartitionValue: fingerprint,
                SafeDisplayValue: fingerprint[..Math.Min(12, fingerprint.Length)]);
        }

        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            return new RateLimitPartitionKey(
                PartitionType: RateLimitPartitionTypes.IpAddress,
                PartitionValue: ipAddress,
                SafeDisplayValue: ipAddress);
        }

        return new RateLimitPartitionKey(
            PartitionType: RateLimitPartitionTypes.AnonymousUnknown,
            PartitionValue: "anonymous-unknown",
            SafeDisplayValue: "anonymous-unknown");
    }

    private bool TryResolveApiKeyFingerprint(HttpContext httpContext, out string fingerprint)
    {
        var headerName = _authenticationOptions.CurrentValue.ApiKeyHeaderName;
        if (string.IsNullOrWhiteSpace(headerName))
        {
            headerName = ApiAuthenticationOptions.DefaultApiKeyHeaderName;
        }

        if (!httpContext.Request.Headers.TryGetValue(headerName, out var headerValue) ||
            headerValue.Count != 1 ||
            string.IsNullOrWhiteSpace(headerValue[0]))
        {
            fingerprint = string.Empty;
            return false;
        }

        var rawKey = headerValue[0]!.Trim();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        fingerprint = Convert.ToHexString(hash).ToLowerInvariant();
        return true;
    }
}
