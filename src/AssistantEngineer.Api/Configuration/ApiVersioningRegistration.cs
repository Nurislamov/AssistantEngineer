using Asp.Versioning;

namespace AssistantEngineer.Api.Configuration;

internal static class ApiVersioningRegistration
{
    public static IServiceCollection AddApiVersioningSupport(
        this IServiceCollection services)
    {
        services.ConfigureOptions<ApiVersioningOptionsSetup>();

        services
            .AddApiVersioning()
            .AddMvc();

        return services;
    }
}