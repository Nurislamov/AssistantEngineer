using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

namespace AssistantEngineer.Api.Configuration;

internal static class ApplicationModuleRegistration
{
    public static IServiceCollection AddAssistantEngineerModules(
        this IServiceCollection services,
        IConfiguration configuration,
        string environmentName)
    {
        var telegramOptions = configuration
            .GetSection("AssistantEngineer:EquipmentDiagnostics:Telegram")
            .Get<EquipmentDiagnosticTelegramWebhookOptions>() ?? new EquipmentDiagnosticTelegramWebhookOptions();
        ApplicationModulesRegistration.ValidateTelegramProductionAccessPolicy(telegramOptions, environmentName);

        services.AddAssistantEngineerApplicationModules(configuration);
        services.AddAssistantEngineerInfrastructureAdapters(
            configuration,
            environmentName);

        return services;
    }
}
