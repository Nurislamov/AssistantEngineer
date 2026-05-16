using AssistantEngineer.Api.Options.Security;
using AssistantEngineer.Api.Security.Authentication;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Security.Authorization;

public sealed class AssistantEngineerAuthorizationService : IAssistantEngineerAuthorizationService
{
    private readonly IOptionsMonitor<ApiAuthorizationOptions> _optionsMonitor;
    private readonly IAuthenticatedPrincipalProvider _principalProvider;
    private readonly IWebHostEnvironment _environment;

    public AssistantEngineerAuthorizationService(
        IOptionsMonitor<ApiAuthorizationOptions> optionsMonitor,
        IAuthenticatedPrincipalProvider principalProvider,
        IWebHostEnvironment environment)
    {
        _optionsMonitor = optionsMonitor;
        _principalProvider = principalProvider;
        _environment = environment;
    }

    public AssistantEngineerAuthorizationDecision AuthorizePilotPermission(string requiredPermission)
    {
        if (string.IsNullOrWhiteSpace(requiredPermission))
        {
            throw new ArgumentException("Required permission must not be empty.", nameof(requiredPermission));
        }

        var options = _optionsMonitor.CurrentValue;
        if (!options.Enabled || !options.EnableEndpointProtectionPilot)
        {
            return AssistantEngineerAuthorizationDecision.Allowed;
        }

        if (_environment.IsDevelopment() && options.AllowAnonymousInDevelopment)
        {
            return AssistantEngineerAuthorizationDecision.Allowed;
        }

        var principal = _principalProvider.GetCurrentPrincipal();
        if (!principal.IsAuthenticated)
        {
            return AssistantEngineerAuthorizationDecision.Unauthorized;
        }

        var hasPermission = principal.Permissions.Any(permission =>
            string.Equals(permission, requiredPermission, StringComparison.OrdinalIgnoreCase));

        return hasPermission
            ? AssistantEngineerAuthorizationDecision.Allowed
            : AssistantEngineerAuthorizationDecision.Forbidden;
    }
}
