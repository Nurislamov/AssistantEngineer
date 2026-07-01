using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Broadcasts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramBroadcastTests
{
    private static readonly DateTimeOffset FixedNowUtc = new(2026, 7, 1, 13, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task OwnerLibraryMenuShowsBroadcastButtonButOtherRolesDoNot()
    {
        var harness = await CreateHarnessAsync();

        var owner = await harness.Adapter.HandleAsync(Command(TelegramManualLibraryService.LibraryButton, harness.Owner));
        var admin = await harness.Adapter.HandleAsync(Command(TelegramManualLibraryService.LibraryButton, harness.Admin));
        var engineer = await harness.Adapter.HandleAsync(Command(TelegramManualLibraryService.LibraryButton, harness.Engineer));
        var installer = await harness.Adapter.HandleAsync(Command(TelegramManualLibraryService.LibraryButton, harness.Installer));
        var consumer = await harness.Adapter.HandleAsync(Command(TelegramManualLibraryService.LibraryButton, harness.Consumer));

        Assert.Contains(InlineButtons(owner), button => button.Text == TelegramBroadcastService.BroadcastButton);
        Assert.DoesNotContain(InlineButtons(admin), button => button.Text == TelegramBroadcastService.BroadcastButton);
        Assert.DoesNotContain(InlineButtons(engineer), button => button.Text == TelegramBroadcastService.BroadcastButton);
        Assert.DoesNotContain(InlineButtons(installer), button => button.Text == TelegramBroadcastService.BroadcastButton);
        Assert.DoesNotContain(InlineButtons(consumer), button => button.Text == TelegramBroadcastService.BroadcastButton);
    }

    [Fact]
    public async Task OwnerBroadcastPreviewTestAndConfirmSendsOnlyReachableRoleRecipients()
    {
        var harness = await CreateHarnessAsync();

        var menu = await harness.Adapter.HandleAsync(Callback("bc:menu", harness.Owner));
        var selected = await harness.Adapter.HandleAsync(Callback("bc:a:ins", harness.Owner));
        var preview = await harness.Adapter.HandleAsync(Command("Плановое уведомление", harness.Owner));
        var testCallback = Assert.Single(InlineButtons(preview), button => button.Text == "🧪 Отправить тест себе").CallbackData;
        var sendCallback = Assert.Single(InlineButtons(preview), button => button.Text == "✅ Отправить").CallbackData;
        var test = await harness.Adapter.HandleAsync(Callback(testCallback, harness.Owner));
        var report = await harness.Adapter.HandleAsync(Callback(sendCallback, harness.Owner));

        Assert.Contains("Выберите аудиторию", menu.Text, StringComparison.Ordinal);
        Assert.Contains("Введите текст рассылки", selected.Text, StringComparison.Ordinal);
        Assert.Contains("📣 Предпросмотр рассылки", preview.Text, StringComparison.Ordinal);
        Assert.Contains("Аудитория: Installer", preview.Text, StringComparison.Ordinal);
        Assert.Contains("Получателей: 1", preview.Text, StringComparison.Ordinal);
        Assert.Contains("Недоступны: 2", preview.Text, StringComparison.Ordinal);
        Assert.Contains("Плановое уведомление", preview.Text, StringComparison.Ordinal);
        Assert.Contains("Тест отправлен вам.", test.Text, StringComparison.Ordinal);
        Assert.Contains("Рассылка завершена", report.Text, StringComparison.Ordinal);
        Assert.Contains("Отправлено: 1", report.Text, StringComparison.Ordinal);
        Assert.Contains("Пропущено: 2", report.Text, StringComparison.Ordinal);
        Assert.Contains("Ошибок: 0", report.Text, StringComparison.Ordinal);
        Assert.Equal([(harness.Owner.TelegramChatId, "Плановое уведомление"), (harness.Installer.TelegramChatId, "Плановое уведомление")], harness.Outbound.Messages);
        Assert.Empty(harness.Outbound.Attachments);

        var campaign = Assert.Single(await harness.BroadcastStore.ListCampaignIdsAsync());
        var recipients = await harness.BroadcastStore.ListRecipientsAsync(campaign);
        Assert.Equal(3, recipients.Count);
        Assert.Single(recipients, item => item.Status == TelegramBroadcastRecipientStatus.Sent);
        Assert.Equal(2, recipients.Count(item => item.Status == TelegramBroadcastRecipientStatus.Skipped));
    }

    [Fact]
    public async Task BroadcastRejectsEmptyTooLongAndMediaBeforeDraftText()
    {
        var harness = await CreateHarnessAsync();

        await harness.Adapter.HandleAsync(Callback("bc:a:all", harness.Owner));
        var empty = await harness.Adapter.HandleAsync(Command("   ", harness.Owner));
        var media = await harness.Adapter.HandleAsync(Media(harness.Owner));
        var tooLong = await harness.Adapter.HandleAsync(Command(new string('a', 3501), harness.Owner));

        Assert.Contains("не должен быть пустым", empty.Text, StringComparison.Ordinal);
        Assert.Contains("Сначала введите текст", media.Text, StringComparison.Ordinal);
        Assert.Contains("слишком длинный", tooLong.Text, StringComparison.Ordinal);
        Assert.Empty(harness.Outbound.Messages);
    }

    [Fact]
    public async Task BroadcastPreviewTestAndSendSupportsPhotoDocumentAndVideoAttachments()
    {
        var harness = await CreateHarnessAsync();

        await harness.Adapter.HandleAsync(Callback("bc:a:eng", harness.Owner));
        var firstPreview = await harness.Adapter.HandleAsync(Command("Материалы для инженеров", harness.Owner));
        var addCallback = Assert.Single(InlineButtons(firstPreview), button => button.Text == "➕ Добавить вложение").CallbackData;
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(addCallback) <= 64);

        await harness.Adapter.HandleAsync(Callback(addCallback, harness.Owner));
        var photoPreview = await harness.Adapter.HandleAsync(Photo(harness.Owner));
        await harness.Adapter.HandleAsync(Callback(addCallback, harness.Owner));
        var documentPreview = await harness.Adapter.HandleAsync(Document(harness.Owner));
        await harness.Adapter.HandleAsync(Callback(addCallback, harness.Owner));
        var videoPreview = await harness.Adapter.HandleAsync(Video(harness.Owner));
        var testCallback = Assert.Single(InlineButtons(videoPreview), button => button.Text == "🧪 Отправить тест себе").CallbackData;
        var sendCallback = Assert.Single(InlineButtons(videoPreview), button => button.Text == "✅ Отправить").CallbackData;

        var test = await harness.Adapter.HandleAsync(Callback(testCallback, harness.Owner));
        var report = await harness.Adapter.HandleAsync(Callback(sendCallback, harness.Owner));

        Assert.Contains("Вложение добавлено", photoPreview.Text, StringComparison.Ordinal);
        Assert.Contains("фото", photoPreview.Text, StringComparison.Ordinal);
        Assert.Contains("manual.pdf", documentPreview.Text, StringComparison.Ordinal);
        Assert.Contains("видео", videoPreview.Text, StringComparison.Ordinal);
        Assert.Contains("clip.mp4", videoPreview.Text, StringComparison.Ordinal);
        Assert.Contains("Тест отправлен вам.", test.Text, StringComparison.Ordinal);
        Assert.Contains("Отправлено: 1", report.Text, StringComparison.Ordinal);
        Assert.Equal(
            [
                (harness.Owner.TelegramChatId, "Материалы для инженеров"),
                (harness.Engineer.TelegramChatId, "Материалы для инженеров")
            ],
            harness.Outbound.Messages);
        Assert.Equal(
            [
                (harness.Owner.TelegramChatId, TelegramBroadcastAttachmentType.Photo, "photo-file-id"),
                (harness.Owner.TelegramChatId, TelegramBroadcastAttachmentType.Document, "document-file-id"),
                (harness.Owner.TelegramChatId, TelegramBroadcastAttachmentType.Video, "video-file-id"),
                (harness.Engineer.TelegramChatId, TelegramBroadcastAttachmentType.Photo, "photo-file-id"),
                (harness.Engineer.TelegramChatId, TelegramBroadcastAttachmentType.Document, "document-file-id"),
                (harness.Engineer.TelegramChatId, TelegramBroadcastAttachmentType.Video, "video-file-id")
            ],
            harness.Outbound.Attachments);

        var campaign = Assert.Single(await harness.BroadcastStore.ListCampaignIdsAsync());
        var attachments = await harness.BroadcastStore.ListAttachmentsAsync(campaign);
        Assert.Equal(3, attachments.Count);
        Assert.Equal([TelegramBroadcastAttachmentType.Photo, TelegramBroadcastAttachmentType.Document, TelegramBroadcastAttachmentType.Video], attachments.Select(item => item.AttachmentType));
        Assert.Equal([1, 2, 3], attachments.Select(item => item.SortOrder));
    }

    [Fact]
    public async Task BroadcastAttachmentModeRejectsUnsupportedMediaSafely()
    {
        var harness = await CreateHarnessAsync();

        await harness.Adapter.HandleAsync(Callback("bc:a:eng", harness.Owner));
        var preview = await harness.Adapter.HandleAsync(Command("Текст", harness.Owner));
        var addCallback = Assert.Single(InlineButtons(preview), button => button.Text == "➕ Добавить вложение").CallbackData;
        await harness.Adapter.HandleAsync(Callback(addCallback, harness.Owner));
        var unsupported = await harness.Adapter.HandleAsync(Command(string.Empty, harness.Owner) with { HasAudio = true });

        Assert.Contains("не поддерживается", unsupported.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(harness.Outbound.Messages);
    }

    [Fact]
    public async Task NonOwnerBroadcastCallbacksAreDenied()
    {
        var harness = await CreateHarnessAsync();

        var admin = await harness.Adapter.HandleAsync(Callback("bc:menu", harness.Admin));
        var consumer = await harness.Adapter.HandleAsync(Callback("bc:a:all", harness.Consumer));

        Assert.Equal("Рассылка доступна только владельцу.", admin.Text);
        Assert.Equal("Рассылка доступна только владельцу.", consumer.Text);
        Assert.DoesNotContain("@installer", admin.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BroadcastSendFailureMarksRecipientFailedAndContinues()
    {
        var harness = await CreateHarnessAsync();
        harness.Outbound.FailChatIds.Add(harness.Installer.TelegramChatId);

        await harness.Adapter.HandleAsync(Callback("bc:a:ins", harness.Owner));
        var preview = await harness.Adapter.HandleAsync(Command("Сервисное окно", harness.Owner));
        var sendCallback = Assert.Single(InlineButtons(preview), button => button.Text == "✅ Отправить").CallbackData;
        var report = await harness.Adapter.HandleAsync(Callback(sendCallback, harness.Owner));

        Assert.Contains("Отправлено: 0", report.Text, StringComparison.Ordinal);
        Assert.Contains("Ошибок: 1", report.Text, StringComparison.Ordinal);
        var campaign = Assert.Single(await harness.BroadcastStore.ListCampaignIdsAsync());
        var recipients = await harness.BroadcastStore.ListRecipientsAsync(campaign);
        Assert.Single(recipients, item => item.Status == TelegramBroadcastRecipientStatus.Failed);
        Assert.All(recipients, item => Assert.DoesNotContain("Сервисное окно", item.ErrorMessage ?? string.Empty, StringComparison.Ordinal));
    }

    private static async Task<Harness> CreateHarnessAsync()
    {
        var options = new EquipmentDiagnosticTelegramOptions
        {
            IsEnabled = true,
            BootstrapOwnerChatId = 100,
            MaxMessageLength = 1200
        };
        var users = new InMemoryTelegramUserStore();
        var owner = await users.EnsureBootstrapOwnerAsync(UserUpdate(100, 1000, "owner", "Owner"));
        var admin = await CreateUserAsync(users, 200, 2000, "admin", "Admin", TelegramUserRole.Admin);
        var engineer = await CreateUserAsync(users, 300, 3000, "engineer", "Engineer", TelegramUserRole.Engineer);
        var installer = await CreateUserAsync(users, 400, 4000, "installer", "Installer", TelegramUserRole.Installer);
        var blockedInstaller = await CreateUserAsync(users, 401, 4001, "blockedinstaller", "Blocked Installer", TelegramUserRole.Installer);
        await users.SetBlockedAsync(blockedInstaller.TelegramChatId, true);
        await CreateUserAsync(users, -402, 4002, "groupinstaller", "Group Installer", TelegramUserRole.Installer);
        var consumer = await CreateUserAsync(users, 500, 5000, "consumer", "Consumer", TelegramUserRole.Consumer);

        var outbound = new FakeOutbound();
        var broadcastStore = new TestBroadcastStore();
        var broadcast = new TelegramBroadcastService(broadcastStore, users, outbound);
        var libraryAccess = new InMemoryTelegramLibraryAccessStore();
        var library = new TelegramManualLibraryService(
            options,
            new InMemoryTelegramDiagnosticCaseStore(),
            new EmptyLocalizationSource(),
            new EmptyManualRegistrySource(),
            new FileTelegramManualFileBindingStore(options),
            users,
            libraryAccess,
            new DisabledEquipmentDiagnosticTelegramOutboundClient());
        var adapter = new EquipmentDiagnosticTelegramAdapter(
            new StaticFacade(),
            new EquipmentDiagnosticTelegramMessageParser(),
            new EquipmentDiagnosticTelegramResponseFormatter(),
            options,
            new TelegramUserAccessService(users, options, libraryAccess),
            users,
            userOverviewService: new TelegramUserOverviewService(users),
            broadcastService: broadcast,
            manualLibraryService: library);

        return new Harness(users, broadcastStore, outbound, adapter, owner, admin, engineer, installer, consumer);
    }

    private static async Task<TelegramUserSnapshot> CreateUserAsync(
        InMemoryTelegramUserStore users,
        long chatId,
        long telegramUserId,
        string username,
        string firstName,
        TelegramUserRole role)
    {
        var user = await users.GetOrCreateConsumerAsync(UserUpdate(chatId, telegramUserId, username, firstName));
        await users.SetRoleAsync(chatId, role);
        return (await users.GetByIdAsync(user.Id))!;
    }

    private static EquipmentDiagnosticTelegramUpdate UserUpdate(
        long chatId,
        long telegramUserId,
        string username,
        string firstName) =>
        new(
            1,
            chatId,
            username,
            "/start",
            MessageId: 1,
            ReceivedAt: FixedNowUtc,
            UserId: telegramUserId,
            FirstName: firstName,
            ChatType: chatId > 0 ? "private" : "group");

    private static EquipmentDiagnosticTelegramUpdate Command(
        string text,
        TelegramUserSnapshot actor) =>
        new(
            2,
            actor.TelegramChatId,
            actor.Username,
            text,
            MessageId: 20,
            ReceivedAt: FixedNowUtc,
            UserId: actor.TelegramUserId,
            FirstName: actor.FirstName,
            LastName: actor.LastName,
            ChatType: actor.TelegramChatId > 0 ? "private" : "group");

    private static EquipmentDiagnosticTelegramUpdate Callback(
        string data,
        TelegramUserSnapshot actor) =>
        new(
            3,
            actor.TelegramChatId,
            actor.Username,
            Text: null,
            MessageId: 77,
            ReceivedAt: FixedNowUtc,
            UserId: actor.TelegramUserId,
            FirstName: actor.FirstName,
            LastName: actor.LastName,
            ChatType: actor.TelegramChatId > 0 ? "private" : "group",
            CallbackQueryId: "callback-id",
            CallbackData: data);

    private static EquipmentDiagnosticTelegramUpdate Media(TelegramUserSnapshot actor) =>
        Command(string.Empty, actor) with { DocumentFileId = "telegram-file-id", DocumentFileName = "manual.pdf" };

    private static EquipmentDiagnosticTelegramUpdate Document(TelegramUserSnapshot actor) =>
        Command(string.Empty, actor) with
        {
            DocumentFileId = "document-file-id",
            DocumentFileUniqueId = "document-unique-id",
            DocumentFileName = "manual.pdf",
            DocumentMimeType = "application/pdf",
            DocumentFileSize = 2048
        };

    private static EquipmentDiagnosticTelegramUpdate Photo(TelegramUserSnapshot actor) =>
        Command(string.Empty, actor) with
        {
            PhotoFileId = "photo-file-id",
            PhotoFileUniqueId = "photo-unique-id",
            PhotoFileSize = 1024,
            HasPhoto = true
        };

    private static EquipmentDiagnosticTelegramUpdate Video(TelegramUserSnapshot actor) =>
        Command(string.Empty, actor) with
        {
            VideoFileId = "video-file-id",
            VideoFileUniqueId = "video-unique-id",
            VideoFileName = "clip.mp4",
            VideoMimeType = "video/mp4",
            VideoFileSize = 4096,
            HasVideo = true
        };

    private static IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton> InlineButtons(
        EquipmentDiagnosticTelegramResponse response) =>
        response.OutboundMessages.Single().ReplyMarkup?.InlineKeyboard?.SelectMany(row => row).ToArray() ?? [];

    private sealed record Harness(
        InMemoryTelegramUserStore Users,
        TestBroadcastStore BroadcastStore,
        FakeOutbound Outbound,
        EquipmentDiagnosticTelegramAdapter Adapter,
        TelegramUserSnapshot Owner,
        TelegramUserSnapshot Admin,
        TelegramUserSnapshot Engineer,
        TelegramUserSnapshot Installer,
        TelegramUserSnapshot Consumer);

    private sealed class TestBroadcastStore : InMemoryTelegramBroadcastStore
    {
        public async Task<IReadOnlyList<long>> ListCampaignIdsAsync()
        {
            var campaigns = new List<long>();
            for (var id = 1L; id <= 100; id++)
            {
                if (await GetCampaignAsync(id) is not null)
                {
                    campaigns.Add(id);
                }
            }

            return campaigns;
        }
    }

    private sealed class FakeOutbound : IEquipmentDiagnosticTelegramOutboundClient
    {
        public List<(long ChatId, string Text)> Messages { get; } = [];
        public List<(long ChatId, TelegramBroadcastAttachmentType Type, string FileId)> Attachments { get; } = [];
        public HashSet<long> FailChatIds { get; } = [];

        public Task<EquipmentDiagnosticTelegramOutboundResult> SendMessageAsync(
            long chatId,
            string text,
            string? parseMode,
            bool disableWebPagePreview,
            EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
            CancellationToken cancellationToken = default)
        {
            if (FailChatIds.Contains(chatId))
            {
                return Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(false, "Forbidden: bot was blocked by the user"));
            }

            Messages.Add((chatId, text));
            return Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(true, "Sent.", 900));
        }

        public Task<EquipmentDiagnosticTelegramOutboundResult> SendDocumentAsync(
            long chatId,
            string telegramFileId,
            string? caption = null,
            EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
            bool protectContent = false,
            CancellationToken cancellationToken = default)
        {
            Attachments.Add((chatId, TelegramBroadcastAttachmentType.Document, telegramFileId));
            return Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(true, "Document sent.", 901));
        }

        public Task<EquipmentDiagnosticTelegramOutboundResult> SendPhotoAsync(
            long chatId,
            string telegramFileId,
            string? caption = null,
            EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
            bool protectContent = false,
            CancellationToken cancellationToken = default)
        {
            Attachments.Add((chatId, TelegramBroadcastAttachmentType.Photo, telegramFileId));
            return Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(true, "Photo sent.", 902));
        }

        public Task<EquipmentDiagnosticTelegramOutboundResult> SendVideoAsync(
            long chatId,
            string telegramFileId,
            string? caption = null,
            EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
            bool protectContent = false,
            CancellationToken cancellationToken = default)
        {
            Attachments.Add((chatId, TelegramBroadcastAttachmentType.Video, telegramFileId));
            return Task.FromResult(new EquipmentDiagnosticTelegramOutboundResult(true, "Video sent.", 903));
        }

        public Task<EquipmentDiagnosticTelegramSetCommandsResult> SetMyCommandsAsync(
            IReadOnlyList<EquipmentDiagnosticTelegramBotCommand> commands,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new EquipmentDiagnosticTelegramSetCommandsResult(true, "Synced."));
    }

    private sealed class StaticFacade : IEquipmentDiagnosticBotFacade
    {
        public Task<EquipmentDiagnosticBotResponse> DiagnoseAsync(
            EquipmentDiagnosticBotRequest request,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Broadcast tests must not call diagnostics.");
    }

    private sealed class EmptyLocalizationSource : IErrorKnowledgeLocalizationSource
    {
        public IReadOnlyCollection<ErrorKnowledgeEntryV2> GetEntries() => [];

        public ErrorKnowledgeLocalizationSelection? Select(
            EquipmentDiagnosticBotResponse response,
            string locale,
            ErrorKnowledgeAudience audience) =>
            null;
    }

    private sealed class EmptyManualRegistrySource : ITelegramManualRegistrySource
    {
        public IReadOnlyList<TelegramManualRegistryEntry> GetManuals() => [];
    }
}
