using System.Security.Claims;
using System.Text.Encodings.Web;
using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Modules.Identity.Application.Abstractions;
using AssistantEngineer.Modules.Identity.Application.Contracts.Audit;
using AssistantEngineer.Modules.Identity.Application.Services.Audit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace AssistantEngineer.Api.Security.ApiKey;

public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "AssistantEngineer.ApiKey";

    private readonly IOptionsMonitor<ApiKeyAuthenticationSettings> _apiKeySettings;
    private readonly IApiKeyValidator _apiKeyValidator;
    private readonly IAuditLogWriter? _auditLogWriter;
    private readonly AuditEventFactory? _auditEventFactory;
    private readonly IOptionsMonitor<AuditLogOptions>? _auditLogOptions;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptionsMonitor<ApiKeyAuthenticationSettings> apiKeySettings,
        IApiKeyValidator apiKeyValidator,
        IAuditLogWriter? auditLogWriter = null,
        AuditEventFactory? auditEventFactory = null,
        IOptionsMonitor<AuditLogOptions>? auditLogOptions = null)
        : base(options, logger, encoder)
    {
        _apiKeySettings = apiKeySettings;
        _apiKeyValidator = apiKeyValidator;
        _auditLogWriter = auditLogWriter;
        _auditEventFactory = auditEventFactory;
        _auditLogOptions = auditLogOptions;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var settings = _apiKeySettings.CurrentValue;
        var headerName = NormalizeHeaderName(settings.HeaderName);

        if (!settings.Enabled)
        {
            return AuthenticateResult.Success(CreateTicket(
                principal: new AuthenticatedPrincipal(
                    UserId: null,
                    OrganizationId: null,
                    ExternalSubjectId: "local-development",
                    AuthenticationScheme: SchemeName,
                    Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    Permissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    IsAuthenticated: true),
                method: "api_key_disabled"));
        }

        if (string.IsNullOrWhiteSpace(settings.Key))
        {
            Logger.LogError(
                "API key authentication is enabled, but Authentication:ApiKey:Key is not configured. " +
                "Set it through environment variable Authentication__ApiKey__Key or user secrets.");
            await TryWriteAuditEventAsync(CreateAuthenticationFailedEvent("ApiKeyNotConfigured"));

            return AuthenticateResult.Fail(
                "API key authentication is enabled, but no API key is configured.");
        }

        if (!Request.Headers.TryGetValue(headerName, out var submittedValues) ||
            StringValues.IsNullOrEmpty(submittedValues))
        {
            await TryWriteAuditEventAsync(CreateAuthenticationFailedEvent("MissingApiKeyHeader"));
            return AuthenticateResult.Fail("Missing API key.");
        }

        if (submittedValues.Count != 1 || string.IsNullOrWhiteSpace(submittedValues[0]))
        {
            await TryWriteAuditEventAsync(CreateAuthenticationFailedEvent("InvalidApiKeyHeader"));
            return AuthenticateResult.Fail("Invalid API key header.");
        }

        var validationResult = await _apiKeyValidator.ValidateAsync(
            submittedValues[0]!,
            Context.RequestAborted);

        if (!validationResult.IsValid || validationResult.Principal is null)
        {
            await TryWriteAuditEventAsync(CreateAuthenticationFailedEvent(
                validationResult.FailureReasonCode ?? "InvalidApiKey"));
            return AuthenticateResult.Fail("Invalid API key.");
        }

        await TryWriteAuditEventAsync(CreateAuthenticationSucceededEvent(validationResult.Principal));

        return AuthenticateResult.Success(CreateTicket(
            validationResult.Principal,
            method: "api_key"));
    }

    private AuthenticationTicket CreateTicket(
        AuthenticatedPrincipal principal,
        string method)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, principal.ExternalSubjectId ?? "api-key-client"),
            new Claim(ClaimTypes.Name, principal.ExternalSubjectId ?? "api-key-client"),
            new Claim("assistant_engineer_auth_method", method)
        };

        if (principal.UserId.HasValue)
        {
            claims.Add(new Claim("assistant_engineer_user_id", principal.UserId.Value.ToString()));
        }

        if (principal.OrganizationId.HasValue)
        {
            claims.Add(new Claim("assistant_engineer_organization_id", principal.OrganizationId.Value.ToString()));
        }

        foreach (var role in principal.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in principal.Permissions)
        {
            claims.Add(new Claim("assistant_engineer_permission", permission));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        return new AuthenticationTicket(claimsPrincipal, Scheme.Name);
    }

    private static string NormalizeHeaderName(string? headerName) =>
        string.IsNullOrWhiteSpace(headerName)
            ? ApiKeyAuthenticationSettings.DefaultHeaderName
            : headerName.Trim();

    private AuditEventWriteRequest CreateAuthenticationSucceededEvent(AuthenticatedPrincipal principal)
    {
        var correlationId = ResolveCorrelationId();
        var mapped = AuthenticatedPrincipalMapper.ToPrincipalAccessContext(principal);
        if (_auditEventFactory is not null)
        {
            return _auditEventFactory.CreateAuthenticationSucceeded(
                mapped,
                correlationId: correlationId,
                requestId: Context.TraceIdentifier);
        }

        return new AuditEventWriteRequest(
            EventType: AuditEventTypes.AuthenticationSucceeded,
            Category: AuditEventCategory.Authentication,
            Outcome: AuditEventOutcome.Succeeded,
            Principal: mapped,
            CorrelationId: correlationId,
            RequestId: Context.TraceIdentifier,
            ResourceType: null,
            ResourceId: null,
            ProjectId: null,
            BuildingId: null,
            WorkflowId: null,
            JobId: null,
            ArtifactId: null,
            Permission: null,
            FailureReason: null,
            Metadata: null);
    }

    private AuditEventWriteRequest CreateAuthenticationFailedEvent(string failureReason)
    {
        var correlationId = ResolveCorrelationId();
        if (_auditEventFactory is not null)
        {
            return _auditEventFactory.CreateAuthenticationFailed(
                failureReason,
                correlationId: correlationId,
                requestId: Context.TraceIdentifier);
        }

        return new AuditEventWriteRequest(
            EventType: AuditEventTypes.AuthenticationFailed,
            Category: AuditEventCategory.Authentication,
            Outcome: AuditEventOutcome.Failed,
            Principal: null,
            CorrelationId: correlationId,
            RequestId: Context.TraceIdentifier,
            ResourceType: null,
            ResourceId: null,
            ProjectId: null,
            BuildingId: null,
            WorkflowId: null,
            JobId: null,
            ArtifactId: null,
            Permission: null,
            FailureReason: string.IsNullOrWhiteSpace(failureReason) ? "UnknownFailure" : failureReason,
            Metadata: null);
    }

    private async Task TryWriteAuditEventAsync(AuditEventWriteRequest request)
    {
        if (_auditLogWriter is null)
        {
            return;
        }

        if (_auditLogOptions is not null && !_auditLogOptions.CurrentValue.Enabled)
        {
            return;
        }

        try
        {
            await _auditLogWriter.WriteAsync(request, Context.RequestAborted);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(
                ex,
                "Audit write failed during API key authentication. EventType={EventType} CorrelationId={CorrelationId}.",
                request.EventType,
                Context.TraceIdentifier);
        }
    }

    private string ResolveCorrelationId()
    {
        if (Request.Headers.TryGetValue("X-Correlation-Id", out var values) &&
            values.Count == 1 &&
            !string.IsNullOrWhiteSpace(values[0]))
        {
            return values[0]!.Trim();
        }

        return Context.TraceIdentifier;
    }

}
