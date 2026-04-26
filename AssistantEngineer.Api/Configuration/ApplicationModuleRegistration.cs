namespace AssistantEngineer.Api.Configuration;

internal static class ApplicationModuleRegistration
{
    public static IServiceCollection AddAssistantEngineerModules(
        this IServiceCollection services,
        IConfiguration configuration,
        string environmentName)
    {
        services.AddAssistantEngineerApplicationModules(configuration);
        services.AddAssistantEngineerInfrastructureAdapters(
            configuration,
            environmentName);

        return services;
    }
}