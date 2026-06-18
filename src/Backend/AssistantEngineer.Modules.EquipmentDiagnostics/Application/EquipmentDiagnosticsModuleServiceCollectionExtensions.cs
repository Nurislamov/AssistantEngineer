using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
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
        services.AddSingleton<ITelegramUserStore, InMemoryTelegramUserStore>();
        services.AddSingleton<ITelegramConversationSessionStore, InMemoryTelegramConversationSessionStore>();
        services.AddSingleton<ITelegramDiagnosticCaseStore, InMemoryTelegramDiagnosticCaseStore>();
        services.AddSingleton<ITelegramServiceRequestStore, InMemoryTelegramServiceRequestStore>();
        services.AddSingleton<TelegramDisplayTimeFormatter>();
        services.AddSingleton<TelegramDiagnosticHistoryService>();
        services.AddSingleton<TelegramServiceRequestService>();
        services.AddSingleton<TelegramServiceRequestQueueService>();
        services.AddSingleton<ITelegramUserAccessService, TelegramUserAccessService>();
        services.AddSingleton<EquipmentDiagnosticTelegramMessageParser>();
        services.AddSingleton<EquipmentDiagnosticTelegramResponseFormatter>();
        services.AddSingleton<TelegramDiagnosticConversationService>();
        services.AddSingleton<IEquipmentDiagnosticTelegramAdapter, EquipmentDiagnosticTelegramAdapter>();
        services.AddSingleton(new EquipmentDiagnosticTelegramWebhookOptions());
        services.AddSingleton<EquipmentDiagnosticTelegramWebhookSecurityPolicy>();
        services.AddSingleton<EquipmentDiagnosticTelegramOperationalCounters>();
        services.AddSingleton<IEquipmentDiagnosticTelegramOutboundClient, DisabledEquipmentDiagnosticTelegramOutboundClient>();
        services.AddTransient<IEquipmentDiagnosticTelegramWebhookHandler, EquipmentDiagnosticTelegramWebhookHandler>();

        return services;
    }
}
