using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application;

public static class EquipmentDiagnosticsModuleServiceCollectionExtensions
{
    public static IServiceCollection AddEquipmentDiagnosticsModule(this IServiceCollection services)
    {
        services.AddSingleton<IEquipmentDiagnosticsKnowledgeSource, EquipmentDiagnosticsJsonKnowledgeSource>();
        services.AddSingleton<IEquipmentDiagnosticsService, InMemoryEquipmentDiagnosticsService>();
        services.AddSingleton<IEquipmentDiagnosticBotService, EquipmentDiagnosticBotService>();
        services.AddSingleton<IEquipmentDiagnosticsFacade, EquipmentDiagnosticsFacade>();
        services.AddSingleton<IEquipmentDiagnosticBotFacade, EquipmentDiagnosticBotFacade>();
        services.AddSingleton(new EquipmentDiagnosticTelegramOptions());
        services.AddSingleton<EquipmentDiagnosticTelegramMessageParser>();
        services.AddSingleton<EquipmentDiagnosticTelegramResponseFormatter>();
        services.AddSingleton<IEquipmentDiagnosticTelegramAdapter, EquipmentDiagnosticTelegramAdapter>();

        return services;
    }
}
