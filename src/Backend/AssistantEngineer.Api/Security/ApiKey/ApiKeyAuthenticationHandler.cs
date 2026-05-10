using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace AssistantEngineer.Api.Security.ApiKey;

public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "AssistantEngineer.ApiKey";

    private readonly IOptionsMonitor<ApiKeyAuthenticationSettings> _apiKeySettings;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptionsMonitor<ApiKeyAuthenticationSettings> apiKeySettings)
        : base(options, logger, encoder)
    {
        _apiKeySettings = apiKeySettings;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var settings = _apiKeySettings.CurrentValue;
        var headerName = NormalizeHeaderName(settings.HeaderName);

        if (!settings.Enabled)
        {
            return Task.FromResult(AuthenticateResult.Success(CreateTicket(
                subject: "local-development",
                method: "api_key_disabled")));
        }

        if (string.IsNullOrWhiteSpace(settings.Key))
        {
            Logger.LogError(
                "API key authentication is enabled, but Authentication:ApiKey:Key is not configured. " +
                "Set it through environment variable Authentication__ApiKey__Key or user secrets.");

            return Task.FromResult(AuthenticateResult.Fail(
                "API key authentication is enabled, but no API key is configured."));
        }

        if (!Request.Headers.TryGetValue(headerName, out var submittedValues) ||
            StringValues.IsNullOrEmpty(submittedValues))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing API key."));
        }

        if (submittedValues.Count != 1 || string.IsNullOrWhiteSpace(submittedValues[0]))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key header."));
        }

        if (!FixedTimeEquals(submittedValues[0]!, settings.Key))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        return Task.FromResult(AuthenticateResult.Success(CreateTicket(
            subject: "api-key-client",
            method: "api_key")));
    }

    private AuthenticationTicket CreateTicket(
        string subject,
        string method)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, subject),
            new Claim(ClaimTypes.Name, subject),
            new Claim("assistant_engineer_auth_method", method)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);

        return new AuthenticationTicket(principal, Scheme.Name);
    }

    private static string NormalizeHeaderName(string? headerName) =>
        string.IsNullOrWhiteSpace(headerName)
            ? ApiKeyAuthenticationSettings.DefaultHeaderName
            : headerName.Trim();

    private static bool FixedTimeEquals(
        string submittedKey,
        string expectedKey)
    {
        var submittedHash = SHA256.HashData(Encoding.UTF8.GetBytes(submittedKey));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expectedKey));

        return CryptographicOperations.FixedTimeEquals(submittedHash, expectedHash);
    }
}