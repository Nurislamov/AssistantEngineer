using AssistantEngineer.Api.Options.Security;
using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Api;

public sealed class AssistantEngineerAuthorizationServiceTests
{
    [Fact]
    public void DisabledAuthorization_AllowsRequest()
    {
        var service = CreateService(
            options: new ApiAuthorizationOptions
            {
                Enabled = false,
                EnableEndpointProtectionPilot = false,
                AllowAnonymousInDevelopment = true
            },
            principal: AuthenticatedPrincipal.Anonymous,
            environmentName: "Development");

        var decision = service.AuthorizePilotPermission(Permission.AdministrationManage.ToString());

        Assert.Equal(AssistantEngineerAuthorizationDecision.Allowed, decision);
    }

    [Fact]
    public void EnabledPilot_AllowsAnonymousInDevelopment_WhenConfigured()
    {
        var service = CreateService(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableEndpointProtectionPilot = true,
                AllowAnonymousInDevelopment = true
            },
            principal: AuthenticatedPrincipal.Anonymous,
            environmentName: "Development");

        var decision = service.AuthorizePilotPermission(Permission.AdministrationManage.ToString());

        Assert.Equal(AssistantEngineerAuthorizationDecision.Allowed, decision);
    }

    [Fact]
    public void EnabledPilot_UnauthenticatedPrincipal_ReturnsUnauthorized()
    {
        var service = CreateService(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableEndpointProtectionPilot = true,
                AllowAnonymousInDevelopment = false
            },
            principal: AuthenticatedPrincipal.Anonymous,
            environmentName: "Development");

        var decision = service.AuthorizePilotPermission(Permission.AdministrationManage.ToString());

        Assert.Equal(AssistantEngineerAuthorizationDecision.Unauthorized, decision);
    }

    [Fact]
    public void EnabledPilot_MissingPermission_ReturnsForbidden()
    {
        var principal = new AuthenticatedPrincipal(
            UserId: 1,
            OrganizationId: 1,
            ExternalSubjectId: "principal-without-admin",
            AuthenticationScheme: "Test",
            Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            Permissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Permission.ProjectsRead.ToString() },
            IsAuthenticated: true);

        var service = CreateService(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableEndpointProtectionPilot = true,
                AllowAnonymousInDevelopment = false
            },
            principal: principal,
            environmentName: "Development");

        var decision = service.AuthorizePilotPermission(Permission.AdministrationManage.ToString());

        Assert.Equal(AssistantEngineerAuthorizationDecision.Forbidden, decision);
    }

    [Fact]
    public void EnabledPilot_WithRequiredPermission_ReturnsAllowed()
    {
        var principal = new AuthenticatedPrincipal(
            UserId: 1,
            OrganizationId: 1,
            ExternalSubjectId: "principal-with-admin",
            AuthenticationScheme: "Test",
            Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            Permissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Permission.AdministrationManage.ToString() },
            IsAuthenticated: true);

        var service = CreateService(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableEndpointProtectionPilot = true,
                AllowAnonymousInDevelopment = false
            },
            principal: principal,
            environmentName: "Development");

        var decision = service.AuthorizePilotPermission(Permission.AdministrationManage.ToString());

        Assert.Equal(AssistantEngineerAuthorizationDecision.Allowed, decision);
    }

    [Fact]
    public void EmptyPermission_ThrowsArgumentException()
    {
        var service = CreateService(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableEndpointProtectionPilot = true,
                AllowAnonymousInDevelopment = false
            },
            principal: AuthenticatedPrincipal.Anonymous,
            environmentName: "Development");

        Assert.Throws<ArgumentException>(() => service.AuthorizePilotPermission(string.Empty));
    }

    private static AssistantEngineerAuthorizationService CreateService(
        ApiAuthorizationOptions options,
        AuthenticatedPrincipal principal,
        string environmentName)
    {
        return new AssistantEngineerAuthorizationService(
            new StaticOptionsMonitor<ApiAuthorizationOptions>(options),
            new StubPrincipalProvider(principal),
            new StubEnvironment(environmentName));
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

    private sealed class StubEnvironment : IWebHostEnvironment
    {
        public StubEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string ApplicationName { get; set; } = "AssistantEngineer.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; }
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
