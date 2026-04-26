using AssistantEngineer.Modules.Benchmarks;
using AssistantEngineer.Modules.Buildings;
using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Equipment;
using AssistantEngineer.Modules.Reporting;

namespace AssistantEngineer.Api.Configuration;

internal static class ApplicationModulesRegistration
{
    public static IServiceCollection AddAssistantEngineerApplicationModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingsModule(configuration);
        services.AddCalculationsModule(configuration);
        services.AddEquipmentModule();
        services.AddReportingModule();
        services.AddBenchmarksModule(configuration);

        return services;
    }
}