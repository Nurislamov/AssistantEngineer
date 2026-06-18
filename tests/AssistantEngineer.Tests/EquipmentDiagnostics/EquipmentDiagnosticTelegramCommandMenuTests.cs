using AssistantEngineer.Api.Services.EquipmentDiagnostics;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using Microsoft.Extensions.Logging;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramCommandMenuTests
{
    [Fact]
    public async Task StartupCommandSyncCallsSetMyCommandsWhenEnabledAndTokenConfigured()
    {
        var outbound = new FakeOutbound();
        var service = CreateService(EnabledOptions(), outbound);

        await service.StartAsync(CancellationToken.None);

        Assert.Equal(1, outbound.SetCommandsCallCount);
        Assert.Equal(["start", "new", "phone", "me", "help", "history", "last", "requests"], outbound.Commands.Select(command => command.Command).ToArray());
        Assert.DoesNotContain(outbound.Commands, command => command.Command == "request");
        Assert.DoesNotContain(outbound.Commands, command => command.Command.Contains(' ', StringComparison.Ordinal));
        Assert.DoesNotContain(outbound.Commands, command => command.Command is "admin_help" or "admin" or "admin users" or "admin allow" or "admin block" or "admin role");
    }

    [Fact]
    public async Task StartupCommandSyncDoesNotCallTelegramWhenDisabled()
    {
        var outbound = new FakeOutbound();
        var service = CreateService(EnabledOptions() with { IsEnabled = false }, outbound);

        await service.StartAsync(CancellationToken.None);

        Assert.Equal(0, outbound.SetCommandsCallCount);
    }

    [Fact]
    public async Task StartupCommandSyncFailureLogsWarningButDoesNotThrow()
    {
        var outbound = new FakeOutbound { SetCommandsSucceeded = false };
        var logger = new CapturingLogger<EquipmentDiagnosticTelegramCommandMenuStartupService>();
        var service = CreateService(EnabledOptions() with { BotToken = "secret-token-value" }, outbound, logger);

        await service.StartAsync(CancellationToken.None);

        Assert.Contains(logger.Messages, message => message.Contains("failed", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(logger.Messages.Concat(logger.Scopes), message => message.Contains("secret-token-value", StringComparison.Ordinal));
    }

    [Fact]
    public void StartupCommandSyncCanBeDisabledByConfiguration()
    {
        Assert.False(EquipmentDiagnosticTelegramCommandMenuStartupService.ShouldSync(
            EnabledOptions() with { Commands = new EquipmentDiagnosticTelegramCommandMenuOptions { SyncOnStartup = false } }));
    }

    private static EquipmentDiagnosticTelegramCommandMenuStartupService CreateService(
        EquipmentDiagnosticTelegramWebhookOptions options,
        FakeOutbound outbound,
        ILogger<EquipmentDiagnosticTelegramCommandMenuStartupService>? logger = null) =>
        new(
            options,
            outbound,
            logger ?? new CapturingLogger<EquipmentDiagnosticTelegramCommandMenuStartupService>());

    private static EquipmentDiagnosticTelegramWebhookOptions EnabledOptions() => new()
    {
        IsEnabled = true,
        BotToken = "test-token-value"
    };

    private sealed class FakeOutbound : IEquipmentDiagnosticTelegramOutboundClient
    {
        public bool SetCommandsSucceeded { get; init; } = true;
        public int SetCommandsCallCount { get; private set; }
        public IReadOnlyList<EquipmentDiagnosticTelegramBotCommand> Commands { get; private set; } = [];

        public Task<EquipmentDiagnosticTelegramOutboundResult> SendMessageAsync(
            long chatId,
            string text,
            string? parseMode,
            bool disableWebPagePreview,
            EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(true, "Sent."));

        public Task<EquipmentDiagnosticTelegramSetCommandsResult> SetMyCommandsAsync(
            IReadOnlyList<EquipmentDiagnosticTelegramBotCommand> commands,
            CancellationToken cancellationToken = default)
        {
            SetCommandsCallCount++;
            Commands = commands;
            return Task.FromResult(new EquipmentDiagnosticTelegramSetCommandsResult(SetCommandsSucceeded, SetCommandsSucceeded ? "Synced." : "Failed."));
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public List<string> Scopes { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            Scopes.Add(state.ToString() ?? string.Empty);
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
