using System.Net;
using System.Net.Http.Json;
using System.Text;
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
            store,
            new FakeProcessedMessageStore());

        var lastProcessed = await service.PollOnceAsync(10);

        Assert.Equal(11, lastProcessed);
        Assert.Equal(11, store.LastSavedUpdateId);
        Assert.Equal(11, handler.HandledUpdateIds.Single());
        Assert.Equal(11, inbound.LastOffset);
        Assert.Equal(25, inbound.LastLimit);
        Assert.Equal(50, inbound.LastTimeoutSeconds);
    }

    [Fact]
    public async Task PollOnceRoutesCallbackQueryThroughDurableUpdateOffset()
    {
        var callback = new TelegramWebhookUpdateDto(
            12,
            Message: null,
            new TelegramWebhookCallbackQueryDto(
                "callback-12",
                new TelegramWebhookUserDto(4, "engineer"),
                new TelegramWebhookMessageDto(
                    7,
                    Text: null,
                    new TelegramWebhookChatDto(-1001, null, "supergroup"),
                    From: null,
                    Date: null),
                "sr:s:1"));
        var inbound = new FakeInboundClient([callback], blockGetUpdates: false);
        var handler = new FakeWebhookHandler();
        var offsetStore = new FakeOffsetStore();
        var service = CreateService(
            EnabledOptions() with
            {
                InboundMode = EquipmentDiagnosticTelegramInboundMode.Polling,
                AllowedUpdates = ["message", "callback_query"]
            },
            inbound,
            handler,
            offsetStore,
            new FakeProcessedMessageStore());

        var lastProcessed = await service.PollOnceAsync(11);

        Assert.Equal(12, lastProcessed);
        Assert.Equal(12, offsetStore.LastSavedUpdateId);
        Assert.Equal([12], handler.HandledUpdateIds);
        Assert.Equal(["message", "callback_query"], inbound.LastAllowedUpdates);
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
            new FakeOffsetStore(),
            new FakeProcessedMessageStore());

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
            new FakeProcessedMessageStore(),
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
            var path = Path.Combine(root, "operations", "offset.txt");
            Assert.Equal("42", await File.ReadAllTextAsync(path));
            Assert.False(await StartsWithUtf8BomAsync(path));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task FileOffsetStoreReadsExistingBomPrefixedOffset()
    {
        var root = Path.Combine(Path.GetTempPath(), $"assistant-engineer-offset-{Guid.NewGuid():N}");
        try
        {
            var path = Path.Combine(root, "operations", "offset.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, [0xEF, 0xBB, 0xBF, (byte)'4', (byte)'2']);
            var store = new FileEquipmentDiagnosticTelegramUpdateOffsetStore(
                new FakeHostEnvironment(root),
                EnabledOptions() with
                {
                    Polling = new EquipmentDiagnosticTelegramPollingOptions
                    {
                        OffsetStoreFilePath = "operations/offset.txt"
                    }
                });

            Assert.Equal(42, await store.GetLastProcessedUpdateIdAsync());
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task FileOffsetStoreFailsDeterministicallyForInvalidOffset()
    {
        var root = Path.Combine(Path.GetTempPath(), $"assistant-engineer-offset-{Guid.NewGuid():N}");
        try
        {
            var path = Path.Combine(root, "operations", "offset.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, "not-a-number", Encoding.UTF8);
            var store = new FileEquipmentDiagnosticTelegramUpdateOffsetStore(
                new FakeHostEnvironment(root),
                EnabledOptions() with
                {
                    Polling = new EquipmentDiagnosticTelegramPollingOptions
                    {
                        OffsetStoreFilePath = "operations/offset.txt"
                    }
                });

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => store.GetLastProcessedUpdateIdAsync());
            Assert.Contains("invalid value", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task PollOnceSkipsDuplicateMessagesInSameBatchAndAdvancesOffset()
    {
        var inbound = new FakeInboundClient(
            [
                Update(11, "private", chatId: 3, messageId: 7, text: "/start"),
                Update(12, "private", chatId: 3, messageId: 7, text: "/start")
            ],
            blockGetUpdates: false);
        var handler = new FakeWebhookHandler();
        var offsetStore = new FakeOffsetStore();
        var logger = new CapturingLogger<EquipmentDiagnosticTelegramPollingBackgroundService>();
        var service = CreateService(
            EnabledOptions() with { InboundMode = EquipmentDiagnosticTelegramInboundMode.Polling },
            inbound,
            handler,
            offsetStore,
            new FakeProcessedMessageStore(),
            logger);

        var lastProcessed = await service.PollOnceAsync(10);

        Assert.Equal(12, lastProcessed);
        Assert.Equal(12, offsetStore.LastSavedUpdateId);
        Assert.Equal([11], handler.HandledUpdateIds);
        Assert.Contains(logger.Messages, message => message.Contains("duplicate message skipped", StringComparison.OrdinalIgnoreCase));
        var logged = string.Join(Environment.NewLine, logger.Messages);
        Assert.DoesNotContain("3", logged, StringComparison.Ordinal);
        Assert.DoesNotContain("/start", logged, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PollOnceSkipsDuplicateMessagesAcrossStoreInstancesAndAdvancesOffset()
    {
        var root = Path.Combine(Path.GetTempPath(), $"assistant-engineer-processed-{Guid.NewGuid():N}");
        try
        {
            var options = EnabledOptions() with
            {
                InboundMode = EquipmentDiagnosticTelegramInboundMode.Polling,
                Polling = new EquipmentDiagnosticTelegramPollingOptions
                {
                    ProcessedMessageStoreFilePath = "operations/processed.txt"
                }
            };
            var firstHandler = new FakeWebhookHandler();
            var firstService = CreateService(
                options,
                new FakeInboundClient([Update(11, "private", chatId: 3, messageId: 7, text: "/start")], false),
                firstHandler,
                new FakeOffsetStore(),
                CreateProcessedStore(root, options));
            await firstService.PollOnceAsync(10);

            var secondOffsetStore = new FakeOffsetStore();
            var secondHandler = new FakeWebhookHandler();
            var secondService = CreateService(
                options,
                new FakeInboundClient([Update(12, "private", chatId: 3, messageId: 7, text: "/start")], false),
                secondHandler,
                secondOffsetStore,
                CreateProcessedStore(root, options));

            var lastProcessed = await secondService.PollOnceAsync(11);

            Assert.Equal([11], firstHandler.HandledUpdateIds);
            Assert.Empty(secondHandler.HandledUpdateIds);
            Assert.Equal(12, lastProcessed);
            Assert.Equal(12, secondOffsetStore.LastSavedUpdateId);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task PollOnceProcessesDifferentMessageIdsAndDifferentChats()
    {
        var handler = new FakeWebhookHandler();
        var service = CreateService(
            EnabledOptions() with { InboundMode = EquipmentDiagnosticTelegramInboundMode.Polling },
            new FakeInboundClient(
                [
                    Update(11, "private", chatId: 3, messageId: 7, text: "/start"),
                    Update(12, "private", chatId: 3, messageId: 8, text: "/start"),
                    Update(13, "private", chatId: 4, messageId: 7, text: "/start")
                ],
                blockGetUpdates: false),
            handler,
            new FakeOffsetStore(),
            new FakeProcessedMessageStore());

        await service.PollOnceAsync(10);

        Assert.Equal([11, 12, 13], handler.HandledUpdateIds);
    }

    [Fact]
    public async Task FailedProcessingIsNotMarkedDuplicateAndRetryCanSucceed()
    {
        var inbound = new FakeInboundClient(
            [Update(11, "private", chatId: 3, messageId: 7, text: "Gree H5")],
            blockGetUpdates: false);
        var handler = new FakeWebhookHandler { ThrowOnFirstHandle = true };
        var offsetStore = new FakeOffsetStore();
        var processedStore = new FakeProcessedMessageStore();
        var service = CreateService(
            EnabledOptions() with { InboundMode = EquipmentDiagnosticTelegramInboundMode.Polling },
            inbound,
            handler,
            offsetStore,
            processedStore);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PollOnceAsync(10));

        Assert.Null(offsetStore.LastSavedUpdateId);
        Assert.False(await processedStore.IsProcessedMessageAsync(3, 7));

        var lastProcessed = await service.PollOnceAsync(10);

        Assert.Equal(11, lastProcessed);
        Assert.Equal([11], handler.HandledUpdateIds);
        Assert.True(await processedStore.IsProcessedMessageAsync(3, 7));
    }

    [Fact]
    public async Task RetryableFailureStatusAdvancesOffsetAndMarksMessageAsSkipped()
    {
        var processedStore = new FakeProcessedMessageStore();
        var offsetStore = new FakeOffsetStore();
        var handler = new FakeWebhookHandler
        {
            ResultStatus = EquipmentDiagnosticTelegramWebhookStatus.OutboundFailed
        };
        var service = CreateService(
            EnabledOptions() with { InboundMode = EquipmentDiagnosticTelegramInboundMode.Polling },
            new FakeInboundClient([Update(11, "private", chatId: 3, messageId: 7)], false),
            handler,
            offsetStore,
            processedStore);

        var lastProcessed = await service.PollOnceAsync(10);

        Assert.Equal(11, lastProcessed);
        Assert.Equal(11, offsetStore.LastSavedUpdateId);
        Assert.Equal([11], handler.HandledUpdateIds);
        Assert.True(await processedStore.IsProcessedMessageAsync(3, 7));
    }

    [Fact]
    public async Task PollOnceContinuesAfterOutboundFailedUpdateAndProcessesNextUpdate()
    {
        var processedStore = new FakeProcessedMessageStore();
        var offsetStore = new FakeOffsetStore();
        var handler = new FakeWebhookHandler();
        handler.ResultStatusByUpdateId[11] = EquipmentDiagnosticTelegramWebhookStatus.OutboundFailed;
        handler.ResultStatusByUpdateId[12] = EquipmentDiagnosticTelegramWebhookStatus.Processed;

        var service = CreateService(
            EnabledOptions() with { InboundMode = EquipmentDiagnosticTelegramInboundMode.Polling },
            new FakeInboundClient(
                [
                    Update(11, "group", chatId: -100, messageId: 700, text: "Gree H5"),
                    Update(12, "private", chatId: 3, messageId: 701, text: "Gree H5")
                ],
                false),
            handler,
            offsetStore,
            processedStore);

        var lastProcessed = await service.PollOnceAsync(10);

        Assert.Equal(12, lastProcessed);
        Assert.Equal(12, offsetStore.LastSavedUpdateId);
        Assert.Equal([11, 12], handler.HandledUpdateIds);
        Assert.True(await processedStore.IsProcessedMessageAsync(-100, 700));
        Assert.True(await processedStore.IsProcessedMessageAsync(3, 701));
    }

    [Fact]
    public async Task FileProcessedMessageStoreWritesHashesWithoutBomAndTrimsOldEntries()
    {
        var root = Path.Combine(Path.GetTempPath(), $"assistant-engineer-processed-{Guid.NewGuid():N}");
        try
        {
            var options = EnabledOptions() with
            {
                Polling = new EquipmentDiagnosticTelegramPollingOptions
                {
                    ProcessedMessageStoreFilePath = "operations/processed.txt",
                    ProcessedMessageStoreMaxEntries = 2
                }
            };
            var store = CreateProcessedStore(root, options);

            Assert.False(await store.IsProcessedMessageAsync(100, 1));
            await store.MarkProcessedMessageAsync(100, 1, 11);
            await store.MarkProcessedMessageAsync(100, 2, 12);
            await store.MarkProcessedMessageAsync(100, 3, 13);
            await store.MarkProcessedMessageAsync(100, 3, 14);
            Assert.True(await store.IsProcessedMessageAsync(100, 3));

            var path = Path.Combine(root, "operations", "processed.txt");
            var text = await File.ReadAllTextAsync(path);
            var lines = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(2, lines.Length);
            Assert.All(lines, line => Assert.Matches("^[A-F0-9]{64}\\|\\d+$", line));
            Assert.DoesNotContain("100", text, StringComparison.Ordinal);
            Assert.False(await StartsWithUtf8BomAsync(path));
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
        IEquipmentDiagnosticTelegramProcessedMessageStore processedMessageStore,
        ILogger<EquipmentDiagnosticTelegramPollingBackgroundService>? logger = null) =>
        new(
            options,
            inbound,
            handler,
            store,
            processedMessageStore,
            logger ?? NullLogger<EquipmentDiagnosticTelegramPollingBackgroundService>.Instance);

    private static EquipmentDiagnosticTelegramWebhookOptions EnabledOptions() => new()
    {
        IsEnabled = true,
        BotToken = "test-token-value",
        TelegramApiBaseUrl = "https://api.telegram.org",
        AllowedUpdates = ["message"]
    };

    private static TelegramWebhookUpdateDto Update(
        long updateId,
        string chatType,
        long chatId = 3,
        long messageId = 2,
        string text = "/start") =>
        new(
            updateId,
            new TelegramWebhookMessageDto(
                messageId,
                text,
                new TelegramWebhookChatDto(chatId, "operator", chatType),
                new TelegramWebhookUserDto(4, "operator"),
                1_700_000_000));

    private static FileEquipmentDiagnosticTelegramProcessedMessageStore CreateProcessedStore(
        string root,
        EquipmentDiagnosticTelegramWebhookOptions options) =>
        new(new FakeHostEnvironment(root), options);

    private static async Task<bool> StartsWithUtf8BomAsync(string path)
    {
        var bytes = await File.ReadAllBytesAsync(path);
        return bytes is [0xEF, 0xBB, 0xBF, ..];
    }

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
        public IReadOnlyCollection<string> LastAllowedUpdates { get; private set; } = [];
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
            LastAllowedUpdates = allowedUpdates;
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
        private int _handleCalls;

        public List<long> HandledUpdateIds { get; } = [];
        public bool ThrowOnFirstHandle { get; init; }
        public EquipmentDiagnosticTelegramWebhookStatus ResultStatus { get; init; } =
            EquipmentDiagnosticTelegramWebhookStatus.Processed;
        public Dictionary<long, EquipmentDiagnosticTelegramWebhookStatus> ResultStatusByUpdateId { get; } = [];

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
            if (ThrowOnFirstHandle && Interlocked.Increment(ref _handleCalls) == 1)
            {
                throw new InvalidOperationException(
                    "No embedded error knowledge JSON resources were found in production assembly.");
            }

            HandledUpdateIds.Add(update.UpdateId);
            var status = ResultStatusByUpdateId.TryGetValue(update.UpdateId, out var configuredStatus)
                ? configuredStatus
                : ResultStatus;
            return Task.FromResult(new EquipmentDiagnosticTelegramWebhookResult(
                status,
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

    private sealed class FakeProcessedMessageStore : IEquipmentDiagnosticTelegramProcessedMessageStore
    {
        private readonly HashSet<(long ChatId, long MessageId)> _processed = [];

        public Task<bool> IsProcessedMessageAsync(
            long chatId,
            long messageId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_processed.Contains((chatId, messageId)));

        public Task MarkProcessedMessageAsync(
            long chatId,
            long messageId,
            long updateId,
            CancellationToken cancellationToken = default)
        {
            _processed.Add((chatId, messageId));
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
