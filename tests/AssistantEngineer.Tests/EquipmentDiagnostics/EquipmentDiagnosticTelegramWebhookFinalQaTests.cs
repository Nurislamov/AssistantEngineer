using System.Reflection;
using AssistantEngineer.Api.Controllers.Equipment;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramWebhookFinalQaTests
{
    [Fact]
    public void ExactlyOneTelegramWebhookEndpointExists()
    {
        var action = typeof(EquipmentDiagnosticsTelegramWebhookController)
            .GetMethod(nameof(EquipmentDiagnosticsTelegramWebhookController.Receive));
        var route = typeof(EquipmentDiagnosticsTelegramWebhookController).GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(action);
        Assert.Equal("api/v{version:apiVersion}/equipment-diagnostics/telegram", route!.Template);
        Assert.Equal("webhook", Assert.Single(action.GetCustomAttributes<HttpPostAttribute>()).Template);
    }

    [Fact]
    public void TelegramTransportIsDisabledByDefaultAndContainsNoCommittedSecrets()
    {
        var defaults = new EquipmentDiagnosticTelegramWebhookOptions();
        var appSettings = File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Api", "appsettings.json"));

        Assert.False(defaults.IsEnabled);
        Assert.Null(defaults.BotToken);
        Assert.Null(defaults.WebhookSecret);
        Assert.Contains("\"IsEnabled\":  false", appSettings, StringComparison.Ordinal);
        Assert.Contains("\"BotToken\":  null", appSettings, StringComparison.Ordinal);
        Assert.Contains("\"WebhookSecret\":  null", appSettings, StringComparison.Ordinal);
    }

    [Fact]
    public void TelegramTransportAddsNoLongPollingOrTelegramPackage()
    {
        var moduleRoot = Path.Combine(
            TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Modules.EquipmentDiagnostics");
        var apiTransportRoot = Path.Combine(
            TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Api", "Services", "EquipmentDiagnostics");
        var source = string.Join(Environment.NewLine,
            Directory.GetFiles(Path.Combine(moduleRoot, "Application", "Telegram"), "*.cs", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(apiTransportRoot, "*.cs", SearchOption.AllDirectories))
                .Select(File.ReadAllText));
        var project = File.ReadAllText(Path.Combine(moduleRoot, "AssistantEngineer.Modules.EquipmentDiagnostics.csproj"));

        Assert.DoesNotContain("Telegram.Bot", project, StringComparison.Ordinal);
        Assert.DoesNotContain("BackgroundService", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetUpdates", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("IHostedService", source, StringComparison.Ordinal);
    }

    [Fact]
    public void WebhookHandlerDoesNotReadKnowledgeOrGeneratedArtifacts()
    {
        var source = File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot,
            "src", "Backend", "AssistantEngineer.Modules.EquipmentDiagnostics",
            "Application", "Telegram", "Webhook", "EquipmentDiagnosticTelegramWebhookHandler.cs"));

        Assert.DoesNotContain("Knowledge", source, StringComparison.Ordinal);
        Assert.DoesNotContain("File" + ".", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Directory" + ".", source, StringComparison.Ordinal);
        Assert.DoesNotContain("manual-" + "codebook", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("staging-candidate-" + "preview", source, StringComparison.OrdinalIgnoreCase);
    }
}
