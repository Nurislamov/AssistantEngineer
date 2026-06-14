using AssistantEngineer.Api.Services.OperationalDiagnostics;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Operations;

public sealed class OperationalDiagnosticsTests
{
    [Fact]
    public void SnapshotContainsSafeConfigurationBooleansAndCountersOnly()
    {
        using var services = CreateServices();
        var diagnostics = services.GetRequiredService<IOperationalDiagnosticsService>();

        var snapshot = diagnostics.GetSnapshot();
        var serialized = System.Text.Json.JsonSerializer.Serialize(snapshot);

        Assert.True(snapshot.EquipmentDiagnostics.BotEndpointAvailable);
        Assert.False(snapshot.EquipmentDiagnostics.TelegramWebhookEnabled);
        Assert.True(snapshot.EquipmentDiagnostics.TelegramWebhookConfigured);
        Assert.True(snapshot.EquipmentDiagnostics.AllowedChatIdsConfigured);
        Assert.False(snapshot.EquipmentDiagnostics.ChatIdDiscoveryEnabled);
        Assert.True(snapshot.CorrelationEnabled);
        Assert.Equal("X-Correlation-ID", snapshot.CorrelationHeaderName);
        Assert.DoesNotContain("test-token-value", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("test_webhook_secret", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("123456789", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("artifacts/verification", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("manual-codebook", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("WebhookSecret=my_secret", "WebhookSecret=[REDACTED]")]
    [InlineData("X-Telegram-Bot-Api-Secret-Token: my_secret", "X-Telegram-Bot-Api-Secret-Token: [REDACTED]")]
    [InlineData("Authorization: Bearer private", "Authorization: [REDACTED]")]
    [InlineData("Password=private;Server=localhost", "Password=[REDACTED];Server=localhost")]
    public void RedactorMasksSensitiveValues(string input, string expected)
    {
        Assert.Equal(expected, OperationalSecretRedactor.Redact(input));
    }

    [Fact]
    public void RedactorMasksTelegramTokenLikeValue()
    {
        var tokenLikeValue = $"123456789:{new string('A', 35)}";

        Assert.Equal("[REDACTED]", OperationalSecretRedactor.Redact(tokenLikeValue));
    }

    [Fact]
    public void RedactorPreservesNormalOperationalMessage()
    {
        const string message = "Operational diagnostics are ready; Telegram webhook transport is disabled.";

        Assert.Equal(message, OperationalSecretRedactor.Redact(message));
    }

    [Theory]
    [InlineData("AllowedChatIds=123,456", "AllowedChatIds=[REDACTED]")]
    [InlineData("DeniedChatIds: 789", "DeniedChatIds:[REDACTED]")]
    [InlineData("{\"chat_id\":123456}", "{\"chat_id\":\"[REDACTED]\"}")]
    [InlineData("{\"text\":\"raw Telegram message\"}", "{\"text\":\"[REDACTED]\"}")]
    public void RedactorMasksOperationalIncidentFields(string input, string expected)
    {
        Assert.Equal(expected, OperationalSecretRedactor.Redact(input));
    }

    [Fact]
    public void OperationsDocumentationStatesObservabilityNonClaims()
    {
        var docs = string.Join(Environment.NewLine,
            Directory.GetFiles(Path.Combine(TestPaths.RepoRoot, "docs", "operations"), "*.md")
                .Select(File.ReadAllText));

        Assert.Contains("No Prometheus", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No alerting", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("audit log", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("database persistence", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not expose a broad", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("public diagnostics endpoint", docs, StringComparison.OrdinalIgnoreCase);
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        services.AddEquipmentDiagnosticsModule();
        services.RemoveAll<EquipmentDiagnosticTelegramWebhookOptions>();
        services.AddSingleton(new EquipmentDiagnosticTelegramWebhookOptions
        {
            IsEnabled = false,
            BotToken = "test-token-value",
            WebhookSecret = "test_webhook_secret",
            AllowedChatIds = [123456789],
            DeniedChatIds = [987654321]
        });
        services.AddSingleton<IOperationalDiagnosticsService, OperationalDiagnosticsService>();
        services.AddSingleton<IOptions<OperationalCorrelationOptions>>(
            Options.Create(new OperationalCorrelationOptions()));
        return services.BuildServiceProvider();
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "AssistantEngineer.Api.Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
