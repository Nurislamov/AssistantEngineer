using AssistantEngineer.Api.Options.Security;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Api.Security.ApiKey;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace AssistantEngineer.Api.Configuration;

internal static class ApiAuthenticationRegistration
{
    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services
            .AddOptions<ApiAuthenticationOptions>()
            .Bind(configuration.GetSection(ApiAuthenticationOptions.SectionName))
            .PostConfigure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.ApiKeyHeaderName))
                {
                    options.ApiKeyHeaderName = ApiAuthenticationOptions.DefaultApiKeyHeaderName;
                }
            });

        services
            .AddOptions<ApiAuthorizationOptions>()
            .Bind(configuration.GetSection(ApiAuthorizationOptions.SectionName));

        services
            .AddOptions<ApiKeyAuthenticationSettings>()
            .Bind(configuration.GetSection(ApiKeyAuthenticationSettings.SectionName))
            .PostConfigure(settings =>
            {
                var boundarySection = configuration.GetSection(ApiAuthenticationOptions.SectionName);
                var boundaryEnabled = ParseBoolean(boundarySection["Enabled"], defaultValue: false);
                var allowAnonymousInDevelopment = ParseBoolean(boundarySection["AllowAnonymousInDevelopment"], defaultValue: true);
                var enableApiKeyAuthentication = ParseBoolean(boundarySection["EnableApiKeyAuthentication"], defaultValue: true);
                var configuredBoundaryHeader = boundarySection["ApiKeyHeaderName"];

                var legacyApiKeySectionExists = configuration
                    .GetSection(ApiKeyAuthenticationSettings.SectionName)
                    .Exists();

                var effectiveHeaderName = string.IsNullOrWhiteSpace(configuredBoundaryHeader)
                    ? settings.HeaderName
                    : configuredBoundaryHeader;

                settings.HeaderName = string.IsNullOrWhiteSpace(effectiveHeaderName)
                    ? ApiKeyAuthenticationSettings.DefaultHeaderName
                    : effectiveHeaderName.Trim();

                if (!legacyApiKeySectionExists &&
                    (environment.IsDevelopment() || environment.IsEnvironment("Testing")))
                {
                    settings.Enabled = false;
                    return;
                }

                if (!enableApiKeyAuthentication)
                {
                    settings.Enabled = false;
                    return;
                }

                if (!boundaryEnabled)
                {
                    settings.Enabled = false;
                    return;
                }

                if (environment.IsDevelopment() && allowAnonymousInDevelopment)
                {
                    settings.Enabled = false;
                    return;
                }

                settings.Enabled = settings.Enabled && enableApiKeyAuthentication;
            });

        services.AddScoped<AuthenticatedPrincipalContext>();
        services.AddScoped<IAuthenticatedPrincipalProvider, AuthenticatedPrincipalProvider>();
        services.AddScoped<IAssistantEngineerAuthorizationService, AssistantEngineerAuthorizationService>();
        services.AddScoped<IProjectReadAccessScopeResolver, DefaultProjectReadAccessScopeResolver>();
        services.AddScoped<IBuildingReadAccessScopeResolver, DefaultBuildingReadAccessScopeResolver>();
        services.AddScoped<IFloorAccessScopeResolver, DefaultFloorAccessScopeResolver>();
        services.AddScoped<IRoomAccessScopeResolver, DefaultRoomAccessScopeResolver>();
        services.AddScoped<IWorkflowAccessScopeResolver, DefaultWorkflowAccessScopeResolver>();
        services.AddScoped<IProtectedEndpointAuthorizationGate, ProtectedEndpointAuthorizationGate>();
        services.AddSingleton<IApiKeyValidator, ConfiguredApiKeyValidator>();

        services
            .AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationHandler.SchemeName,
                _ => { });

        services.AddAuthorization(options =>
        {
            var authenticatedApiPolicy = new AuthorizationPolicyBuilder(ApiKeyAuthenticationHandler.SchemeName)
                .RequireAuthenticatedUser()
                .Build();

            options.DefaultPolicy = authenticatedApiPolicy;
            options.FallbackPolicy = authenticatedApiPolicy;
        });

        return services;
    }

    private static bool ParseBoolean(string? value, bool defaultValue) =>
        bool.TryParse(value, out var parsed) ? parsed : defaultValue;
}
