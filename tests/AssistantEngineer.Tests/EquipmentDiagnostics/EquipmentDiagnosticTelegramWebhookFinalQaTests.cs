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
        Assert.False(defaults.EnableChatIdDiscovery);
        Assert.Null(defaults.BotToken);
        Assert.Null(defaults.WebhookSecret);
        Assert.Contains("\"IsEnabled\":  false", appSettings, StringComparison.Ordinal);
        Assert.Contains("\"BotToken\":  null", appSettings, StringComparison.Ordinal);
        Assert.Contains("\"WebhookSecret\":  null", appSettings, StringComparison.Ordinal);
        Assert.Contains("\"EnableChatIdDiscovery\":  false", appSettings, StringComparison.Ordinal);
        Assert.Contains("\"DeniedChatIds\":  []", appSettings, StringComparison.Ordinal);
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

    [Fact]
    public void TelegramOperationsScriptsExistAndDoNotPrintToken()
    {
        var scriptsRoot = Path.Combine(TestPaths.RepoRoot, "scripts", "equipment-diagnostics");
        foreach (var name in new[]
                 {
                     "set-telegram-webhook.ps1",
                     "get-telegram-webhook-info.ps1",
                     "delete-telegram-webhook.ps1"
                 })
        {
            var source = File.ReadAllText(Path.Combine(scriptsRoot, name));
            Assert.Contains("$BotToken", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Write-Host $BotToken", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Write-Output $BotToken", source, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void OperationsReadinessDocumentsDenylistAndDiscoveryDefaults()
    {
        var docsRoot = Path.Combine(TestPaths.RepoRoot, "docs", "equipment-diagnostics");
        var operations = File.ReadAllText(Path.Combine(docsRoot, "telegram-operations-checklist.md"));
        var deployment = File.ReadAllText(Path.Combine(docsRoot, "telegram-webhook-deployment.md"));

        Assert.Contains("DeniedChatIds", operations, StringComparison.Ordinal);
        Assert.Contains("Deny wins over allow", operations, StringComparison.Ordinal);
        Assert.Contains("EnableChatIdDiscovery=false", operations, StringComparison.Ordinal);
        Assert.Contains("Generate the webhook secret", operations, StringComparison.Ordinal);
        Assert.Contains("No real token or webhook secret", operations, StringComparison.Ordinal);
        Assert.Contains("get-telegram-webhook-info.ps1", deployment, StringComparison.Ordinal);
        Assert.Contains("delete-telegram-webhook.ps1", deployment, StringComparison.Ordinal);
    }

    [Fact]
    public void ChangedTelegramSourcesContainNoProductionLikeCredentials()
    {
        var roots = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Modules.EquipmentDiagnostics", "Application", "Telegram"),
            Path.Combine(TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Api", "Services", "EquipmentDiagnostics"),
            Path.Combine(TestPaths.RepoRoot, "scripts", "equipment-diagnostics"),
            Path.Combine(TestPaths.RepoRoot, "docs", "equipment-diagnostics")
        };
        var content = string.Join(Environment.NewLine, roots
            .Where(Directory.Exists)
            .SelectMany(root => Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            .Where(path => Path.GetExtension(path) is ".cs" or ".ps1" or ".md")
            .Select(File.ReadAllText));

        Assert.DoesNotMatch(@"\b\d{8,10}:[A-Za-z0-9_-]{30,}\b", content);
        Assert.DoesNotContain("WebhookSecret\": \"", content, StringComparison.Ordinal);
        Assert.DoesNotContain("BotToken\": \"", content, StringComparison.Ordinal);
    }
}
