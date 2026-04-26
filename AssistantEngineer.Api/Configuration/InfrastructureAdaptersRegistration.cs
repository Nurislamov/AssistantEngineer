using AssistantEngineer.Infrastructure;

namespace AssistantEngineer.Api.Configuration;

internal static class InfrastructureAdaptersRegistration
{
    public static IServiceCollection AddAssistantEngineerInfrastructureAdapters(
        this IServiceCollection services,
        IConfiguration configuration,
        string environmentName)
    {
        services.AddInfrastructure(
            configuration,
            environmentName);

        return services;
    }
}