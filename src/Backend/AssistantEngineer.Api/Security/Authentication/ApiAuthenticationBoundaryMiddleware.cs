using System.Security.Claims;
using AssistantEngineer.Api.Options.Security;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Security.Authentication;

public sealed class ApiAuthenticationBoundaryMiddleware
{
    private const string AuthMethodClaim = "assistant_engineer_auth_method";

    private readonly RequestDelegate _next;
    private readonly ILogger<ApiAuthenticationBoundaryMiddleware> _logger;
    private readonly IOptionsMonitor<ApiAuthenticationOptions> _optionsMonitor;
    private readonly IWebHostEnvironment _environment;

    public ApiAuthenticationBoundaryMiddleware(
        RequestDelegate next,
        ILogger<ApiAuthenticationBoundaryMiddleware> logger,
        IOptionsMonitor<ApiAuthenticationOptions> optionsMonitor,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _optionsMonitor = optionsMonitor;
        _environment = environment;
    }

    public async Task InvokeAsync(
        HttpContext httpContext,
        AuthenticatedPrincipalContext principalContext)
    {
        var options = _optionsMonitor.CurrentValue;
        var principal = ResolvePrincipal(httpContext.User, options);
        principalContext.SetPrincipal(principal);

        _logger.LogDebug(
            "Authentication boundary principal resolved. IsAuthenticated={IsAuthenticated}, Scheme={Scheme}, UserId={UserId}, OrganizationId={OrganizationId}.",
            principal.IsAuthenticated,
            principal.AuthenticationScheme,
            principal.UserId,
            principal.OrganizationId);

        await _next(httpContext);
    }

    private AuthenticatedPrincipal ResolvePrincipal(
        ClaimsPrincipal? user,
        ApiAuthenticationOptions options)
    {
        if (!options.Enabled)
        {
            return AuthenticatedPrincipal.Anonymous;
        }

        if (_environment.IsDevelopment() && options.AllowAnonymousInDevelopment && (user?.Identity?.IsAuthenticated != true))
        {
            return AuthenticatedPrincipal.Anonymous;
        }

        if (user?.Identity?.IsAuthenticated != true)
        {
            return AuthenticatedPrincipal.Anonymous;
        }

        var userId = TryParseIntClaim(user, ClaimTypes.NameIdentifier);
        var organizationId = TryParseIntClaim(user, "assistant_engineer_organization_id");
        var externalSubjectId = user.FindFirstValue("assistant_engineer_external_subject_id");
        var scheme = user.Identity?.AuthenticationType ??
                     user.FindFirstValue(AuthMethodClaim) ??
                     "Authenticated";

        var roles = user.Claims
            .Where(claim => claim.Type is ClaimTypes.Role or "role")
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var permissions = user.Claims
            .Where(claim => claim.Type is "assistant_engineer_permission" or "permission")
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new AuthenticatedPrincipal(
            UserId: userId,
            OrganizationId: organizationId,
            ExternalSubjectId: externalSubjectId,
            AuthenticationScheme: scheme,
            Roles: roles,
            Permissions: permissions,
            IsAuthenticated: true);
    }

    private static int? TryParseIntClaim(
        ClaimsPrincipal principal,
        string claimType)
    {
        var value = principal.FindFirstValue(claimType);
        return int.TryParse(value, out var parsed) ? parsed : null;
    }
}
