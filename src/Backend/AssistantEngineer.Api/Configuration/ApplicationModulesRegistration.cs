using AssistantEngineer.Modules.Benchmarks;
using AssistantEngineer.Modules.Buildings;
using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.EngineeringWorkflow;
using AssistantEngineer.Modules.Equipment;
using AssistantEngineer.Modules.Identity;
using AssistantEngineer.Modules.Reporting;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AssistantEngineer.Api.Configuration;

internal static class ApplicationModulesRegistration
{
    public static IServiceCollection AddAssistantEngineerApplicationModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddBuildingsModule(configuration);
        services.AddCalculationsModule(configuration);
        services.AddEquipmentModule();
        services.AddReportingModule();
        services.AddBenchmarksModule(configuration);
        services.AddEngineeringWorkflowModule(configuration);
        services.AddIdentityModule(configuration);

        return services;
    }
}
