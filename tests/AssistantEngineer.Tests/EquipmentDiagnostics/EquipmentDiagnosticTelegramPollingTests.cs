using System.Net;
using System.Net.Http.Json;
using AssistantEngineer.Api.Services.EquipmentDiagnostics;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramPollingTests
{
    [Fact]
    public void PollingServiceStartsOnlyForPollingModeOrExplicitPollingFlag()
    {
        Assert.False(EquipmentDiagnosticTelegramPollingBackgroundService.ShouldStart(EnabledOptions()));
        Assert.True(EquipmentDiagnosticTelegramPollingBackgroundService.ShouldStart(
            EnabledOptions() with { InboundMode = EquipmentDiagnosticTelegramInboundMode.Polling }));
        Assert.True(EquipmentDiagnosticTelegramPollingBackgroundService.ShouldStart(
            EnabledOptions() with { Polling = new EquipmentDiagnosticTelegramPollingOptions { Enabled = true } }));
        Assert.False(EquipmentDiagnosticTelegramPollingBackgroundService.ShouldStart(
            EnabledOptions() with { InboundMode = EquipmentDiagnosticTelegramInboundMode.Polling, BotToken = null }));
    }

    [Fact]
    public async Task PollOnceUsesLastProcessedOffsetAndSavesAfterHandlingUpdate()
    {
        var inbound = new FakeInboundClient(
            [Update(11, "private")],
            blockGetUpdates: false);
        var handler = new FakeWebhookHandler();
        var store = new FakeOffsetStore();
        var service = CreateService(
            options: EnabledOptions() with { InboundMode = EquipmentDiagnosticTelegramInboundMode.Polling },
            inbound,
            handler,
            store);

        var lastProcessed = await service.PollOnceAsync(10);

        Assert.Equal(11, lastProcessed);
        Assert.Equal(11, store.LastSavedUpdateId);
        Assert.Equal(11, handler.HandledUpdateIds.Single());
        Assert.Equal(11, inbound.LastOffset);
        Assert.Equal(25, inbound.LastLimit);
        Assert.Equal(50, inbound.LastTimeoutSeconds);
    }

    [Fact]
    public async Task PollingServiceDeletesWebhookOnStartupWhenConfigured()
    {
        var inbound = new FakeInboundClient([], blockGetUpdates: true);
        var service = CreateService(
            options: EnabledOptions() with
            {
                InboundMode = EquipmentDiagnosticTelegramInboundMode.Polling,
                DeleteWebhookOnStartup = true
            },
            inbound,
            new FakeWebhookHandler(),
            new FakeOffsetStore());

        await service.StartAsync(CancellationToken.None);
        await inbound.DeleteWebhookCalled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync(CancellationToken.None);

        Assert.True(inbound.DeleteWebhookDropPendingUpdates);
    }

    [Fact]
    public async Task PollingServiceRetriesAfterFailedBatchWithoutLeakingTokenToLogs()
    {
        var inbound = new FakeInboundClient([], blockGetUpdates: true)
        {
            ThrowOnFirstGetUpdates = true
        };
        var logger = new CapturingLogger<EquipmentDiagnosticTelegramPollingBackgroundService>();
        var service = CreateService(
            options: EnabledOptions() with
            {
                BotToken = "secret-token-value",
                InboundMode = EquipmentDiagnosticTelegramInboundMode.Polling,
                Polling = new EquipmentDiagnosticTelegramPollingOptions
                {
                    DelayAfterErrorSeconds = 1
                }
            },
            inbound,
            new FakeWebhookHandler(),
            new FakeOffsetStore(),
            logger);

        await service.StartAsync(CancellationToken.None);
        await inbound.SecondGetUpdatesCalled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync(CancellationToken.None);

        Assert.Contains(logger.Messages, message => message.Contains("batch failed", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("secret-token-value", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InboundClientSendsGetUpdatesPayloadAndDeleteWebhookPayload()
    {
        var handler = new CapturingTelegramApiHandler();
        var client = new EquipmentDiagnosticTelegramInboundClient(
            new HttpClient(handler),
            EnabledOptions());

        var updates = await client.GetUpdatesAsync(123, 25, 50, ["message"]);
        var delete = await client.DeleteWebhookAsync(dropPendingUpdates: true);

        Assert.Single(updates);
        Assert.True(delete.Succeeded);
        Assert.Contains(handler.Requests, request =>
            request.Path.EndsWith("/bottest-token-value/getUpdates", StringComparison.Ordinal) &&
            request.Body.Contains("\"offset\":123", StringComparison.Ordinal) &&
            request.Body.Contains("\"timeout\":50", StringComparison.Ordinal) &&
            request.Body.Contains("\"allowed_updates\":[\"message\"]", StringComparison.Ordinal));
        Assert.Contains(handler.Requests, request =>
            request.Path.EndsWith("/bottest-token-value/deleteWebhook", StringComparison.Ordinal) &&
            request.Body.Contains("\"drop_pending_updates\":true", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FileOffsetStorePersistsOnlyLastProcessedUpdateId()
    {
        var root = Path.Combine(Path.GetTempPath(), $"assistant-engineer-offset-{Guid.NewGuid():N}");
        try
        {
            var store = new FileEquipmentDiagnosticTelegramUpdateOffsetStore(
                new FakeHostEnvironment(root),
                EnabledOptions() with
                {
                    Polling = new EquipmentDiagnosticTelegramPollingOptions
                    {
                        OffsetStoreFilePath = "operations/offset.txt"
                    }
                });

            Assert.Null(await store.GetLastProcessedUpdateIdAsync());
            await store.SaveLastProcessedUpdateIdAsync(42);

            Assert.Equal(42, await store.GetLastProcessedUpdateIdAsync());
            Assert.Equal("42", await File.ReadAllTextAsync(Path.Combine(root, "operations", "offset.txt")));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static EquipmentDiagnosticTelegramPollingBackgroundService CreateService(
        EquipmentDiagnosticTelegramWebhookOptions options,
        IEquipmentDiagnosticTelegramInboundClient inbound,
        IEquipmentDiagnosticTelegramWebhookHandler handler,
        IEquipmentDiagnosticTelegramUpdateOffsetStore store,
        ILogger<EquipmentDiagnosticTelegramPollingBackgroundService>? logger = null) =>
        new(
            options,
            inbound,
            handler,
            store,
            logger ?? NullLogger<EquipmentDiagnosticTelegramPollingBackgroundService>.Instance);

    private static EquipmentDiagnosticTelegramWebhookOptions EnabledOptions() => new()
    {
        IsEnabled = true,
        BotToken = "test-token-value",
        TelegramApiBaseUrl = "https://api.telegram.org",
        AllowedUpdates = ["message"]
    };

    private static TelegramWebhookUpdateDto Update(long updateId, string chatType) =>
        new(
            updateId,
            new TelegramWebhookMessageDto(
                2,
                "/start",
                new TelegramWebhookChatDto(3, "operator", chatType),
                new TelegramWebhookUserDto(4, "operator"),
                1_700_000_000));

    private sealed class FakeInboundClient(
        IReadOnlyList<TelegramWebhookUpdateDto> updates,
        bool blockGetUpdates) : IEquipmentDiagnosticTelegramInboundClient
    {
        private int _getUpdatesCalls;

        public TaskCompletionSource DeleteWebhookCalled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SecondGetUpdatesCalled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool ThrowOnFirstGetUpdates { get; init; }
        public long? LastOffset { get; private set; }
        public int? LastLimit { get; private set; }
        public int? LastTimeoutSeconds { get; private set; }
        public bool DeleteWebhookDropPendingUpdates { get; private set; }

        public async Task<IReadOnlyList<TelegramWebhookUpdateDto>> GetUpdatesAsync(
            long offset,
            int limit,
            int timeoutSeconds,
            IReadOnlyCollection<string> allowedUpdates,
            CancellationToken cancellationToken = default)
        {
            LastOffset = offset;
            LastLimit = limit;
            LastTimeoutSeconds = timeoutSeconds;
            var call = Interlocked.Increment(ref _getUpdatesCalls);
            if (ThrowOnFirstGetUpdates && call == 1)
            {
                throw new InvalidOperationException("token-bearing details must not be logged: secret-token-value");
            }

            if (call >= 2)
            {
                SecondGetUpdatesCalled.TrySetResult();
            }

            if (blockGetUpdates)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            return updates;
        }

        public Task<EquipmentDiagnosticTelegramDeleteWebhookResult> DeleteWebhookAsync(
            bool dropPendingUpdates,
            CancellationToken cancellationToken = default)
        {
            DeleteWebhookDropPendingUpdates = dropPendingUpdates;
            DeleteWebhookCalled.TrySetResult();
            return Task.FromResult(new EquipmentDiagnosticTelegramDeleteWebhookResult(true, "Deleted."));
        }
    }

    private sealed class FakeWebhookHandler : IEquipmentDiagnosticTelegramWebhookHandler
    {
        public List<long> HandledUpdateIds { get; } = [];

        public Task<EquipmentDiagnosticTelegramWebhookResult> HandleAsync(
            TelegramWebhookUpdateDto update,
            string? suppliedSecret,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Polling tests use trusted handling.");
        }

        public Task<EquipmentDiagnosticTelegramWebhookResult> HandleTrustedAsync(
            TelegramWebhookUpdateDto update,
            CancellationToken cancellationToken = default)
        {
            HandledUpdateIds.Add(update.UpdateId);
            return Task.FromResult(new EquipmentDiagnosticTelegramWebhookResult(
                EquipmentDiagnosticTelegramWebhookStatus.Processed,
                "Processed."));
        }
    }

    private sealed class FakeOffsetStore : IEquipmentDiagnosticTelegramUpdateOffsetStore
    {
        public long? LastSavedUpdateId { get; private set; }

        public Task<long?> GetLastProcessedUpdateIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(LastSavedUpdateId);

        public Task SaveLastProcessedUpdateIdAsync(
            long updateId,
            CancellationToken cancellationToken = default)
        {
            LastSavedUpdateId = updateId;
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingTelegramApiHandler : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            Requests.Add(new CapturedRequest(request.RequestUri!.AbsolutePath, body));
            object content = request.RequestUri.AbsolutePath.EndsWith("/getUpdates", StringComparison.Ordinal)
                ? new
                {
                    ok = true,
                    result = new[]
                    {
                        new
                        {
                            update_id = 123,
                            message = new
                            {
                                message_id = 1,
                                text = "/start",
                                chat = new { id = 3, username = "operator", type = "private" },
                                from = new { id = 4, username = "operator" },
                                date = 1_700_000_000
                            }
                        }
                    }
                }
                : new
                {
                    ok = true,
                    result = true
                };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(content)
            };
        }
    }

    private sealed record CapturedRequest(string Path, string Body);

    private sealed class FakeHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "AssistantEngineer.Tests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull =>
            NullScope.Instance;

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
