using AssistantEngineer.Api.Configuration;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramProductionAccessPolicyTests
{
    [Fact]
    public void ProductionEnabledTelegramWithoutAllowlistFailsFast()
    {
        var options = TelegramOptions(enableChatIdDiscovery: false);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplicationModulesRegistration.ValidateTelegramProductionAccessPolicy(options, "Production"));

        Assert.Contains("requires BootstrapOwnerChatId, AllowedChatIds, or AllowedUsernames", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionEnabledTelegramWithAllowedUsernameIsAccepted()
    {
        var options = TelegramOptions(
            enableChatIdDiscovery: false,
            allowedUsername: "operator");

        ApplicationModulesRegistration.ValidateTelegramProductionAccessPolicy(options, "Production");
    }

    [Fact]
    public void ProductionEnabledTelegramWithBootstrapOwnerIsAccepted()
    {
        var options = TelegramOptions(enableChatIdDiscovery: false) with
        {
            BootstrapOwnerChatId = 123456789
        };

        ApplicationModulesRegistration.ValidateTelegramProductionAccessPolicy(options, "Production");
    }

    [Fact]
    public void ProductionEnabledTelegramWithChatIdDiscoveryIsAcceptedForInitialSetup()
    {
        var options = TelegramOptions(enableChatIdDiscovery: true);

        ApplicationModulesRegistration.ValidateTelegramProductionAccessPolicy(options, "Production");
    }

    private static EquipmentDiagnosticTelegramWebhookOptions TelegramOptions(
        bool enableChatIdDiscovery,
        string? allowedUsername = null) =>
        new()
    {
        IsEnabled = true,
        InboundMode = EquipmentDiagnosticTelegramInboundMode.Polling,
        Polling = new EquipmentDiagnosticTelegramPollingOptions { Enabled = true },
        BotToken = "test-token-value",
        EnableChatIdDiscovery = enableChatIdDiscovery,
        AllowedUsernames = string.IsNullOrWhiteSpace(allowedUsername) ? [] : [allowedUsername]
    };
}
