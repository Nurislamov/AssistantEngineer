using AssistantEngineer.Api.Options.Security;
using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Api.Security.TenantIsolation;
using AssistantEngineer.Modules.Identity.Application.Services.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Api.Security.TenantIsolation;

public sealed class TenantQueryContextFactoryTests
{
    [Fact]
    public void CreateFromPrincipal_MapsAuthenticatedPrincipalFieldsAndPermissions()
    {
        var factory = CreateFactory(
            principal: AuthenticatedPrincipal.Anonymous,
            projectOptions: new ProjectTenantAccessOptions
            {
                AllowUnscopedProjectsDuringTransition = true,
                EnableStrictTenantMatch = true
            },
            authorizationOptions: new ApiAuthorizationOptions
            {
                ReturnNotFoundForTenantMismatch = true
            });

        var principal = new AuthenticatedPrincipal(
            UserId: 2001,
            OrganizationId: 1001,
            ExternalSubjectId: "principal-a",
            AuthenticationScheme: "ApiKey",
            Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Engineer" },
            Permissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Permission.ProjectsRead.ToString(),
                Permission.BuildingsRead.ToString()
            },
            IsAuthenticated: true);

        var context = factory.CreateFromPrincipal(principal);

        Assert.Equal(2001, context.UserId);
        Assert.Equal(1001, context.OrganizationId);
        Assert.True(context.IsAuthenticated);
        Assert.Contains(Permission.ProjectsRead.ToString(), context.Permissions, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(Permission.BuildingsRead.ToString(), context.Permissions, StringComparer.OrdinalIgnoreCase);
        Assert.True(context.AllowUnscopedResourcesDuringTransition);
        Assert.True(context.StrictTenantMatch);
        Assert.True(context.ReturnNotFoundForTenantMismatch);
        Assert.True(context.IncludeUnscopedResourcesInTenantLists);
    }

    [Fact]
    public void CreateCurrent_MapsAnonymousPrincipalAsUnauthenticated()
    {
        var factory = CreateFactory(
            principal: AuthenticatedPrincipal.Anonymous,
            projectOptions: new ProjectTenantAccessOptions
            {
                AllowUnscopedProjectsDuringTransition = false,
                EnableStrictTenantMatch = true
            },
            authorizationOptions: new ApiAuthorizationOptions());

        var context = factory.CreateCurrent();

        Assert.Null(context.UserId);
        Assert.Null(context.OrganizationId);
        Assert.False(context.IsAuthenticated);
        Assert.Empty(context.Permissions);
        Assert.False(context.AllowUnscopedResourcesDuringTransition);
    }

    [Fact]
    public void CreateCurrent_UsesExplicitIncludeUnscopedOverride()
    {
        var factory = CreateFactory(
            principal: new AuthenticatedPrincipal(
                UserId: 1,
                OrganizationId: 1001,
                ExternalSubjectId: "principal-a",
                AuthenticationScheme: "ApiKey",
                Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                Permissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    Permission.ProjectsRead.ToString()
                },
                IsAuthenticated: true),
            projectOptions: new ProjectTenantAccessOptions
            {
                AllowUnscopedProjectsDuringTransition = false,
                EnableStrictTenantMatch = true
            },
            authorizationOptions: new ApiAuthorizationOptions());

        var context = factory.CreateCurrent(includeUnscopedResourcesInTenantLists: true);

        Assert.True(context.IncludeUnscopedResourcesInTenantLists);
        Assert.False(context.AllowUnscopedResourcesDuringTransition);
    }

    [Fact]
    public void CreateCurrent_UsesExplicitReturnNotFoundOverride()
    {
        var factory = CreateFactory(
            principal: new AuthenticatedPrincipal(
                UserId: 1,
                OrganizationId: 1001,
                ExternalSubjectId: "principal-a",
                AuthenticationScheme: "ApiKey",
                Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                Permissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    Permission.WorkflowsRead.ToString()
                },
                IsAuthenticated: true),
            projectOptions: new ProjectTenantAccessOptions
            {
                AllowUnscopedProjectsDuringTransition = true,
                EnableStrictTenantMatch = true
            },
            authorizationOptions: new ApiAuthorizationOptions
            {
                ReturnNotFoundForTenantMismatch = false
            });

        var context = factory.CreateCurrent(returnNotFoundForTenantMismatch: true);

        Assert.True(context.ReturnNotFoundForTenantMismatch);
    }

    private static TenantQueryContextFactory CreateFactory(
        AuthenticatedPrincipal principal,
        ProjectTenantAccessOptions projectOptions,
        ApiAuthorizationOptions authorizationOptions)
    {
        return new TenantQueryContextFactory(
            new StubPrincipalProvider(principal),
            Options.Create(projectOptions),
            new StaticOptionsMonitor<ApiAuthorizationOptions>(authorizationOptions));
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
