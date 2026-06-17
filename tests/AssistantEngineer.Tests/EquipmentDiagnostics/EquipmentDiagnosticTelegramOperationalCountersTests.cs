using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramOperationalCountersTests
{
    [Fact]
    public async Task CountersRecordProcessedSecretRejectedAndUnauthorizedChatWithoutPayloadData()
    {
        var counters = new EquipmentDiagnosticTelegramOperationalCounters();
        var accepted = CreateHandler(counters, new FakeAdapter(EquipmentDiagnosticTelegramResponseKind.Reply, "Safe reply"));
        var ignored = CreateHandler(counters, new FakeAdapter(EquipmentDiagnosticTelegramResponseKind.Ignored, string.Empty));

        await accepted.HandleAsync(Update("sensitive operator message"), "valid_secret");
        await accepted.HandleAsync(Update("another sensitive message"), "wrong_secret");
        await ignored.HandleAsync(Update("third sensitive message"), "valid_secret");

        var snapshot = counters.GetSnapshot();
        var serialized = System.Text.Json.JsonSerializer.Serialize(snapshot);

        Assert.Equal(3, snapshot.UpdatesReceived);
        Assert.Equal(1, snapshot.UpdatesProcessed);
        Assert.Equal(1, snapshot.UpdatesRejectedSecret);
        Assert.Equal(1, snapshot.UpdatesRejectedUnauthorized);
        Assert.Equal(1, snapshot.UpdatesIgnored);
        Assert.DoesNotContain("sensitive", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.All(snapshot.GetType().GetProperties(), property =>
        {
            Assert.DoesNotContain("Chat", property.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Message", property.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Text", property.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Token", property.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Value", property.Name, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task CountersRecordInvalidUpdateAndOutboundFailure()
    {
        var counters = new EquipmentDiagnosticTelegramOperationalCounters();
        var invalid = CreateHandler(counters, new FakeAdapter(EquipmentDiagnosticTelegramResponseKind.Reply, "Safe reply"));
        var failed = CreateHandler(
            counters,
            new FakeAdapter(EquipmentDiagnosticTelegramResponseKind.Reply, "Safe reply"),
            outboundSucceeded: false);

        await invalid.HandleAsync(Update(null), "valid_secret");
        await failed.HandleAsync(Update("Gree H5"), "valid_secret");

        var snapshot = counters.GetSnapshot();
        Assert.Equal(2, snapshot.UpdatesReceived);
        Assert.Equal(1, snapshot.InvalidUpdates);
        Assert.Equal(1, snapshot.OutboundSendFailures);
    }

    private static EquipmentDiagnosticTelegramWebhookHandler CreateHandler(
        EquipmentDiagnosticTelegramOperationalCounters counters,
        IEquipmentDiagnosticTelegramAdapter adapter,
        bool outboundSucceeded = true) =>
        new(
            new EquipmentDiagnosticTelegramWebhookOptions
            {
                IsEnabled = true,
                WebhookSecret = "valid_secret",
                BotToken = "test-token-value"
            },
            new EquipmentDiagnosticTelegramWebhookSecurityPolicy(),
            adapter,
            new FakeOutbound(outboundSucceeded),
            counters);

    private static TelegramWebhookUpdateDto Update(string? text) =>
        new(1, new TelegramWebhookMessageDto(2, text, new TelegramWebhookChatDto(3, "operator"), null, 1_700_000_000));

    private sealed class FakeAdapter(EquipmentDiagnosticTelegramResponseKind kind, string text)
        : IEquipmentDiagnosticTelegramAdapter
    {
        public Task<EquipmentDiagnosticTelegramResponse> HandleAsync(
            EquipmentDiagnosticTelegramUpdate update,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new EquipmentDiagnosticTelegramResponse(
                update.ChatId, text, kind, null, true, [], null));
    }

    private sealed class FakeOutbound(bool succeeded) : IEquipmentDiagnosticTelegramOutboundClient
    {
        public Task<EquipmentDiagnosticTelegramOutboundResult> SendMessageAsync(
            long chatId,
            string text,
            string? parseMode,
            bool disableWebPagePreview,
            EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(succeeded, succeeded ? "Sent." : "Failed."));
    }
}
