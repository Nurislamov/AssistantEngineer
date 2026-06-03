using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application;

public static class EquipmentDiagnosticsModuleServiceCollectionExtensions
{
    public static IServiceCollection AddEquipmentDiagnosticsModule(this IServiceCollection services)
    {
        services.AddSingleton<IEquipmentDiagnosticsKnowledgeSource, InMemoryEquipmentDiagnosticsKnowledgeSource>();
        services.AddSingleton<IEquipmentDiagnosticsService, InMemoryEquipmentDiagnosticsService>();
        services.AddSingleton<IEquipmentDiagnosticsFacade, EquipmentDiagnosticsFacade>();

        return services;
    }
}
