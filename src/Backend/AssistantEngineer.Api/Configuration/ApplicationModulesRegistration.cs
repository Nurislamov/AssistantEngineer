using AssistantEngineer.Modules.Benchmarks;
using AssistantEngineer.Modules.Buildings;
using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.EngineeringWorkflow;
using AssistantEngineer.Modules.Equipment;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using AssistantEngineer.Modules.Identity;
using AssistantEngineer.Modules.Reporting;
using AssistantEngineer.Api.Services.EquipmentDiagnostics;
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
        services.AddEquipmentDiagnosticsModule();
        services.RemoveAll<EquipmentDiagnosticTelegramOptions>();
        services.RemoveAll<EquipmentDiagnosticTelegramWebhookOptions>();
        var telegramOptions = configuration
            .GetSection("AssistantEngineer:EquipmentDiagnostics:Telegram")
            .Get<EquipmentDiagnosticTelegramWebhookOptions>() ?? new EquipmentDiagnosticTelegramWebhookOptions();
        services.AddSingleton(telegramOptions);
        services.AddSingleton(new EquipmentDiagnosticTelegramOptions
        {
            IsEnabled = telegramOptions.IsEnabled,
            AllowedChatIds = telegramOptions.AllowedChatIds,
            AllowedUsernames = telegramOptions.AllowedUsernames,
            DeniedChatIds = telegramOptions.DeniedChatIds,
            DeniedUsernames = telegramOptions.DeniedUsernames,
            EnableChatIdDiscovery = telegramOptions.EnableChatIdDiscovery,
            MaxMessageLength = telegramOptions.MaxMessageLength,
            DefaultManufacturer = telegramOptions.DefaultManufacturer,
            PreferredLanguage = telegramOptions.PreferredLanguage,
            EnableFreeTextParsing = telegramOptions.EnableFreeTextParsing,
            RequireExplicitManufacturer = telegramOptions.RequireExplicitManufacturer
        });
        services.AddHttpClient<IEquipmentDiagnosticTelegramOutboundClient, EquipmentDiagnosticTelegramOutboundClient>(
            client => client.Timeout = TimeSpan.FromSeconds(Math.Clamp(telegramOptions.SendMessageTimeoutSeconds, 1, 60)));
        services.AddReportingModule();
        services.AddBenchmarksModule(configuration);
        services.AddEngineeringWorkflowModule(configuration);
        services.AddIdentityModule(configuration);

        return services;
    }
}
