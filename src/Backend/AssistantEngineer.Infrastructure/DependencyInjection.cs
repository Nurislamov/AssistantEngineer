using AssistantEngineer.Infrastructure.Composition;
using AssistantEngineer.Infrastructure.Configuration;
using AssistantEngineer.Infrastructure.Seeding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string environmentName)
    {
        ConfigurationSecurityValidator.Validate(configuration, environmentName);

        services.AddPersistence(configuration, environmentName);
        services.AddRepositoryAdapters();
        services.AddApplicationProviders();
        ReportExporterRegistration.AddReportExporters(services);
        services.AddEnergyPlusIntegration(configuration);
        services.AddScoped<IDevelopmentDemoDataSeeder, DevelopmentDemoDataSeeder>();

        return services;
    }
}
