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
            .AddOptions<ApiKeyAuthenticationSettings>()
            .Bind(configuration.GetSection(ApiKeyAuthenticationSettings.SectionName))
            .PostConfigure(settings =>
            {
                if (string.IsNullOrWhiteSpace(settings.HeaderName))
                {
                    settings.HeaderName = ApiKeyAuthenticationSettings.DefaultHeaderName;
                }

                if ((environment.IsDevelopment() || environment.IsEnvironment("Testing")) &&
                    !configuration.GetSection(ApiKeyAuthenticationSettings.SectionName).Exists())
                {
                    settings.Enabled = false;
                }
            });

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
}