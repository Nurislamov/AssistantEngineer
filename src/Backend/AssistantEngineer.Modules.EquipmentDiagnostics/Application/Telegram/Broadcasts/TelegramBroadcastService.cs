using System.Collections.Concurrent;
using System.Text;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Broadcasts;

public sealed class TelegramBroadcastService
{
    public const string BroadcastButton = "📣 Рассылка";
    public const string BroadcastMenuCallback = "bc:menu";
    private const string AudiencePrefix = "bc:a:";
    private const string TestPrefix = "bc:test:";
    private const string SendPrefix = "bc:send:";
    private const string AddAttachmentPrefix = "bc:add:";
    private const string PreviewPrefix = "bc:prev:";
    private const string CancelPrefix = "bc:cancel:";
    private const int MaxBroadcastTextLength = 3500;
    private const int UserPageSize = 50;

    private readonly ITelegramBroadcastStore _broadcastStore;
    private readonly ITelegramUserStore _userStore;
    private readonly IEquipmentDiagnosticTelegramOutboundClient _outboundClient;
    private readonly ConcurrentDictionary<long, long> _pendingTextCampaigns = new();
    private readonly ConcurrentDictionary<long, long> _pendingAttachmentCampaigns = new();

    public TelegramBroadcastService(
        ITelegramBroadcastStore broadcastStore,
        ITelegramUserStore userStore,
        IEquipmentDiagnosticTelegramOutboundClient outboundClient)
    {
        _broadcastStore = broadcastStore;
        _userStore = userStore;
        _outboundClient = outboundClient;
    }

    public static bool IsCallback(string? callbackData) =>
        string.Equals(callbackData, BroadcastMenuCallback, StringComparison.Ordinal) ||
        callbackData?.StartsWith(AudiencePrefix, StringComparison.Ordinal) == true ||
        callbackData?.StartsWith(TestPrefix, StringComparison.Ordinal) == true ||
        callbackData?.StartsWith(SendPrefix, StringComparison.Ordinal) == true ||
        callbackData?.StartsWith(AddAttachmentPrefix, StringComparison.Ordinal) == true ||
        callbackData?.StartsWith(PreviewPrefix, StringComparison.Ordinal) == true ||
        callbackData?.StartsWith(CancelPrefix, StringComparison.Ordinal) == true;

    public async Task<TelegramBroadcastResult> HandleCallbackAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken = default)
    {
        if (!IsOwner(access))
        {
            return new TelegramBroadcastResult("Рассылка доступна только владельцу.", CallbackAnswerText: "Нет доступа");
        }

        if (string.Equals(update.CallbackData, BroadcastMenuCallback, StringComparison.Ordinal))
        {
            return Menu();
        }

        if (update.CallbackData?.StartsWith(AudiencePrefix, StringComparison.Ordinal) == true)
        {
            return await SelectAudienceAsync(update, access.User!, cancellationToken);
        }

        if (TryParseCampaign(update.CallbackData, TestPrefix, out var testCampaignId))
        {
            return await SendTestAsync(testCampaignId, access.User!, cancellationToken);
        }

        if (TryParseCampaign(update.CallbackData, SendPrefix, out var sendCampaignId))
        {
            return await SendAsync(sendCampaignId, access.User!, cancellationToken);
        }

        if (TryParseCampaign(update.CallbackData, AddAttachmentPrefix, out var addAttachmentCampaignId))
        {
            return await AskAttachmentAsync(addAttachmentCampaignId, access.User!, cancellationToken);
        }

        if (TryParseCampaign(update.CallbackData, PreviewPrefix, out var previewCampaignId))
        {
            return await BuildPreviewAsync(previewCampaignId, TestSent: false, cancellationToken);
        }

        if (TryParseCampaign(update.CallbackData, CancelPrefix, out var cancelCampaignId))
        {
            _pendingTextCampaigns.TryRemove(access.User!.TelegramChatId, out _);
            _pendingAttachmentCampaigns.TryRemove(access.User!.TelegramChatId, out _);
            await _broadcastStore.CancelAsync(cancelCampaignId, cancellationToken);
            return new TelegramBroadcastResult("Рассылка отменена.", CallbackAnswerText: "Отменено");
        }

        return new TelegramBroadcastResult("Действие рассылки устарело.", CallbackAnswerText: "Устарело");
    }

    public async Task<TelegramBroadcastResult?> TryHandleTextAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken = default)
    {
        if (access.User is null ||
            !_pendingTextCampaigns.TryGetValue(access.User.TelegramChatId, out var campaignId))
        {
            return await TryHandleAttachmentAsync(update, access, cancellationToken);
        }

        if (!IsOwner(access))
        {
            _pendingTextCampaigns.TryRemove(access.User.TelegramChatId, out _);
            return new TelegramBroadcastResult("Рассылка доступна только владельцу.");
        }

        if (HasAttachmentMedia(update) ||
            HasUnsupportedMedia(update))
        {
            return new TelegramBroadcastResult(
                "Сначала введите текст рассылки. Вложение можно добавить после текста.");
        }

        var text = update.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return new TelegramBroadcastResult("Текст рассылки не должен быть пустым.");
        }

        if (text.StartsWith("/", StringComparison.Ordinal))
        {
            return null;
        }

        if (text.Length > MaxBroadcastTextLength)
        {
            return new TelegramBroadcastResult($"Текст рассылки слишком длинный. Максимум: {MaxBroadcastTextLength} символов.");
        }

        var campaign = await _broadcastStore.GetCampaignAsync(campaignId, cancellationToken);
        if (campaign is null || campaign.Status == TelegramBroadcastCampaignStatus.Cancelled)
        {
            _pendingTextCampaigns.TryRemove(access.User.TelegramChatId, out _);
            return new TelegramBroadcastResult("Черновик рассылки устарел.");
        }

        var audience = await BuildAudienceAsync(campaign, cancellationToken);
        campaign = await _broadcastStore.SetReadyAsync(
            campaign.Id,
            text,
            audience.Reachable.Count,
            audience.Unavailable.Count,
            cancellationToken);
        _pendingTextCampaigns.TryRemove(access.User.TelegramChatId, out _);
        return await PreviewAsync(campaign!, audience, TestSent: false, cancellationToken);
    }

    private async Task<TelegramBroadcastResult> SelectAudienceAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserSnapshot owner,
        CancellationToken cancellationToken)
    {
        if (!TryParseAudience(update.CallbackData![AudiencePrefix.Length..], out var kind, out var role))
        {
            return new TelegramBroadcastResult("Аудитория рассылки устарела.", CallbackAnswerText: "Устарело");
        }

        var campaign = await _broadcastStore.CreateDraftAsync(
            owner.Id,
            owner.TelegramChatId,
            kind,
            role,
            update.ReceivedAt ?? DateTimeOffset.UtcNow,
            cancellationToken);
        _pendingTextCampaigns[owner.TelegramChatId] = campaign.Id;
        return new TelegramBroadcastResult(
            "Введите текст рассылки.",
            CallbackAnswerText: "Введите текст");
    }

    private async Task<TelegramBroadcastResult> AskAttachmentAsync(
        long campaignId,
        TelegramUserSnapshot owner,
        CancellationToken cancellationToken)
    {
        var campaign = await _broadcastStore.GetCampaignAsync(campaignId, cancellationToken);
        if (campaign is null || campaign.Status is not TelegramBroadcastCampaignStatus.Ready)
        {
            return new TelegramBroadcastResult("Черновик рассылки устарел.", CallbackAnswerText: "Устарело");
        }

        _pendingAttachmentCampaigns[owner.TelegramChatId] = campaign.Id;
        return new TelegramBroadcastResult(
            "Пришлите фото, файл или видео для рассылки.\n\nФайл будет сохранён как Telegram file_id, без загрузки содержимого.",
            AttachmentPromptKeyboard(campaign.Id),
            "Жду вложение");
    }

    private async Task<TelegramBroadcastResult?> TryHandleAttachmentAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        if (access.User is null ||
            !_pendingAttachmentCampaigns.TryGetValue(access.User.TelegramChatId, out var campaignId))
        {
            return null;
        }

        if (!IsOwner(access))
        {
            _pendingAttachmentCampaigns.TryRemove(access.User.TelegramChatId, out _);
            return new TelegramBroadcastResult("Рассылка доступна только владельцу.");
        }

        var campaign = await _broadcastStore.GetCampaignAsync(campaignId, cancellationToken);
        if (campaign is null || campaign.Status is not TelegramBroadcastCampaignStatus.Ready)
        {
            _pendingAttachmentCampaigns.TryRemove(access.User.TelegramChatId, out _);
            return new TelegramBroadcastResult("Черновик рассылки устарел.");
        }

        var attachment = ExtractAttachment(update);
        if (attachment is null)
        {
            if (HasUnsupportedMedia(update))
            {
                return new TelegramBroadcastResult(
                    "Это вложение не поддерживается для рассылки. Можно добавить фото, файл или видео.",
                    AttachmentPromptKeyboard(campaign.Id));
            }

            return new TelegramBroadcastResult(
                "Пришлите фото, файл или видео для рассылки.",
                AttachmentPromptKeyboard(campaign.Id));
        }

        var existing = await _broadcastStore.ListAttachmentsAsync(campaign.Id, cancellationToken);
        await _broadcastStore.AddAttachmentAsync(
            campaign.Id,
            attachment with { SortOrder = existing.Count + 1 },
            update.ReceivedAt ?? DateTimeOffset.UtcNow,
            cancellationToken);
        _pendingAttachmentCampaigns.TryRemove(access.User.TelegramChatId, out _);
        return await BuildPreviewAsync(campaign.Id, TestSent: false, cancellationToken, "Вложение добавлено.");
    }

    private async Task<TelegramBroadcastResult> SendTestAsync(
        long campaignId,
        TelegramUserSnapshot owner,
        CancellationToken cancellationToken)
    {
        var campaign = await _broadcastStore.GetCampaignAsync(campaignId, cancellationToken);
        if (campaign is null || campaign.Status is not TelegramBroadcastCampaignStatus.Ready)
        {
            return new TelegramBroadcastResult("Черновик рассылки устарел.", CallbackAnswerText: "Устарело");
        }

        var attachments = await _broadcastStore.ListAttachmentsAsync(campaign.Id, cancellationToken);
        await SendBroadcastPayloadAsync(owner.TelegramChatId, campaign, attachments, cancellationToken);
        var audience = await BuildAudienceAsync(campaign, cancellationToken);
        return await PreviewAsync(campaign, audience, TestSent: true, cancellationToken);
    }

    private async Task<TelegramBroadcastResult> SendAsync(
        long campaignId,
        TelegramUserSnapshot owner,
        CancellationToken cancellationToken)
    {
        var campaign = await _broadcastStore.GetCampaignAsync(campaignId, cancellationToken);
        if (campaign is null || campaign.Status is not TelegramBroadcastCampaignStatus.Ready)
        {
            return new TelegramBroadcastResult("Черновик рассылки устарел.", CallbackAnswerText: "Устарело");
        }

        var audience = await BuildAudienceAsync(campaign, cancellationToken);
        var recipientCreates = audience.Reachable
            .Select(user => new TelegramBroadcastRecipientCreate(
                user.TelegramUserId,
                user.TelegramChatId,
                user.Role,
                TelegramBroadcastRecipientStatus.Pending))
            .Concat(audience.Unavailable.Select(user => new TelegramBroadcastRecipientCreate(
                user.TelegramUserId,
                user.TelegramChatId,
                user.Role,
                TelegramBroadcastRecipientStatus.Skipped,
                UnavailableReason(user))))
            .ToArray();
        var now = DateTimeOffset.UtcNow;
        var recipients = await _broadcastStore.ReplaceRecipientsAsync(campaign.Id, recipientCreates, now, cancellationToken);
        await _broadcastStore.MarkSendingAsync(campaign.Id, now, cancellationToken);

        var sent = 0;
        var failed = 0;
        string? lastError = null;
        foreach (var recipient in recipients.Where(item => item.Status == TelegramBroadcastRecipientStatus.Pending))
        {
            if (recipient.TelegramChatId is null)
            {
                continue;
            }

            var attachments = await _broadcastStore.ListAttachmentsAsync(campaign.Id, cancellationToken);
            var result = await SendBroadcastPayloadAsync(
                recipient.TelegramChatId.Value,
                campaign,
                attachments,
                cancellationToken);
            if (result.Succeeded)
            {
                sent++;
                await _broadcastStore.UpdateRecipientAsync(
                    new TelegramBroadcastRecipientUpdate(
                        recipient.Id,
                        TelegramBroadcastRecipientStatus.Sent,
                        SentAt: DateTimeOffset.UtcNow),
                    cancellationToken);
                continue;
            }

            failed++;
            lastError = SanitizeError(result.Message);
            await _broadcastStore.UpdateRecipientAsync(
                new TelegramBroadcastRecipientUpdate(
                    recipient.Id,
                    TelegramBroadcastRecipientStatus.Failed,
                    ErrorCode: "TelegramSendFailed",
                    ErrorMessage: lastError),
                cancellationToken);
        }

        var skipped = recipients.Count(item => item.Status == TelegramBroadcastRecipientStatus.Skipped);
        var completed = await _broadcastStore.CompleteAsync(
            campaign.Id,
            sent,
            skipped,
            failed,
            lastError,
            DateTimeOffset.UtcNow,
            cancellationToken);

        return Report(completed ?? campaign, audience, sent, skipped, failed);
    }

    private static TelegramBroadcastResult Menu()
    {
        var rows = new[]
        {
            new[] { Button("Всем активным", "bc:a:all") },
            [Button("Owner", "bc:a:own")],
            [Button("Admin", "bc:a:adm")],
            [Button("Engineer", "bc:a:eng")],
            [Button("Installer", "bc:a:ins")],
            [Button("Consumer", "bc:a:con")],
            [Button("Назад", "lib:open")]
        };
        return new TelegramBroadcastResult(
            "📣 Рассылка\n\nВыберите аудиторию:",
            new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard: rows),
            "Рассылка");
    }

    private async Task<TelegramBroadcastResult> BuildPreviewAsync(
        long campaignId,
        bool TestSent,
        CancellationToken cancellationToken,
        string? leadingText = null)
    {
        var campaign = await _broadcastStore.GetCampaignAsync(campaignId, cancellationToken);
        if (campaign is null || campaign.Status is not TelegramBroadcastCampaignStatus.Ready)
        {
            return new TelegramBroadcastResult("Черновик рассылки устарел.", CallbackAnswerText: "Устарело");
        }

        var audience = await BuildAudienceAsync(campaign, cancellationToken);
        var preview = await PreviewAsync(campaign, audience, TestSent, cancellationToken);
        return string.IsNullOrWhiteSpace(leadingText)
            ? preview
            : preview with { Text = $"{leadingText}\n\n{preview.Text}", CallbackAnswerText = leadingText };
    }

    private async Task<TelegramBroadcastResult> PreviewAsync(
        TelegramBroadcastCampaignSnapshot campaign,
        BroadcastAudience audience,
        bool TestSent,
        CancellationToken cancellationToken)
    {
        var attachments = await _broadcastStore.ListAttachmentsAsync(campaign.Id, cancellationToken);
        var text =
            "📣 Предпросмотр рассылки\n\n" +
            $"Аудитория: {AudienceLabel(campaign)}\n" +
            $"Получателей: {audience.Reachable.Count}\n" +
            $"Недоступны: {audience.Unavailable.Count}\n\n" +
            "Текст:\n" +
            campaign.Text +
            "\n\n" +
            AttachmentSummary(attachments) +
            (TestSent ? "\n\nТест отправлен вам." : string.Empty);
        var rows = new[]
        {
            new[]
            {
                Button("🧪 Отправить тест себе", $"{TestPrefix}{campaign.Id}"),
                Button("✅ Отправить", $"{SendPrefix}{campaign.Id}")
            },
            [Button("➕ Добавить вложение", $"{AddAttachmentPrefix}{campaign.Id}")],
            [Button("❌ Отмена", $"{CancelPrefix}{campaign.Id}")]
        };
        return new TelegramBroadcastResult(
            text,
            new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard: rows),
            TestSent ? "Тест отправлен" : "Предпросмотр");
    }

    private static TelegramBroadcastResult Report(
        TelegramBroadcastCampaignSnapshot campaign,
        BroadcastAudience audience,
        int sent,
        int skipped,
        int failed) =>
        new(
            "📣 Рассылка завершена\n\n" +
            $"Аудитория: {AudienceLabel(campaign)}\n" +
            $"Получателей: {audience.Reachable.Count}\n" +
            $"Отправлено: {sent}\n" +
            $"Пропущено: {skipped}\n" +
            $"Ошибок: {failed}",
            CallbackAnswerText: "Рассылка завершена");

    private async Task<EquipmentDiagnosticTelegramOutboundResult> SendBroadcastPayloadAsync(
        long chatId,
        TelegramBroadcastCampaignSnapshot campaign,
        IReadOnlyList<TelegramBroadcastAttachmentSnapshot> attachments,
        CancellationToken cancellationToken)
    {
        var textResult = await _outboundClient.SendMessageAsync(
            chatId,
            campaign.Text,
            null,
            true,
            cancellationToken: cancellationToken);
        if (!textResult.Succeeded)
        {
            return textResult;
        }

        foreach (var attachment in attachments.OrderBy(item => item.SortOrder).ThenBy(item => item.Id))
        {
            var result = attachment.AttachmentType switch
            {
                TelegramBroadcastAttachmentType.Photo => await _outboundClient.SendPhotoAsync(
                    chatId,
                    attachment.FileId,
                    protectContent: true,
                    cancellationToken: cancellationToken),
                TelegramBroadcastAttachmentType.Document => await _outboundClient.SendDocumentAsync(
                    chatId,
                    attachment.FileId,
                    protectContent: true,
                    cancellationToken: cancellationToken),
                TelegramBroadcastAttachmentType.Video => await _outboundClient.SendVideoAsync(
                    chatId,
                    attachment.FileId,
                    protectContent: true,
                    cancellationToken: cancellationToken),
                _ => new EquipmentDiagnosticTelegramOutboundResult(false, "Unsupported broadcast attachment.")
            };
            if (!result.Succeeded)
            {
                return result;
            }
        }

        return textResult;
    }

    private async Task<BroadcastAudience> BuildAudienceAsync(
        TelegramBroadcastCampaignSnapshot campaign,
        CancellationToken cancellationToken)
    {
        var users = campaign.AudienceKind == TelegramBroadcastAudienceKind.Role && campaign.AudienceRole is not null
            ? await LoadRoleAsync(campaign.AudienceRole.Value, cancellationToken)
            : await LoadAllRolesAsync(cancellationToken);
        var reachable = new List<TelegramUserListItem>();
        var unavailable = new List<TelegramUserListItem>();
        var seenChatIds = new HashSet<long>();
        foreach (var user in users)
        {
            if (!user.IsReachableForPrivateMessage ||
                !seenChatIds.Add(user.TelegramChatId))
            {
                unavailable.Add(user);
                continue;
            }

            reachable.Add(user);
        }

        return new BroadcastAudience(reachable, unavailable);
    }

    private async Task<IReadOnlyList<TelegramUserListItem>> LoadAllRolesAsync(CancellationToken cancellationToken)
    {
        var users = new List<TelegramUserListItem>();
        foreach (var role in Enum.GetValues<TelegramUserRole>())
        {
            users.AddRange(await LoadRoleAsync(role, cancellationToken));
        }

        return users;
    }

    private async Task<IReadOnlyList<TelegramUserListItem>> LoadRoleAsync(
        TelegramUserRole role,
        CancellationToken cancellationToken)
    {
        var users = new List<TelegramUserListItem>();
        var page = 0;
        TelegramUserListPage result;
        do
        {
            result = await _userStore.GetUsersByRoleAsync(role, page, UserPageSize, cancellationToken);
            users.AddRange(result.Users);
            page++;
        }
        while (page < result.TotalPages);

        return users;
    }

    private static bool IsOwner(TelegramUserAccessResult access) =>
        access.User is { IsEnabled: true, IsBlocked: false, Role: TelegramUserRole.Owner };

    private static bool HasUnsupportedMedia(EquipmentDiagnosticTelegramUpdate update) =>
        update.HasVoice ||
        update.HasVideoNote ||
        update.HasAudio ||
        update.HasLocation ||
        update.HasAnimation;

    private static bool HasAttachmentMedia(EquipmentDiagnosticTelegramUpdate update) =>
        !string.IsNullOrWhiteSpace(update.DocumentFileId) ||
        !string.IsNullOrWhiteSpace(update.PhotoFileId) ||
        !string.IsNullOrWhiteSpace(update.VideoFileId);

    private static TelegramBroadcastAttachmentCreate? ExtractAttachment(EquipmentDiagnosticTelegramUpdate update)
    {
        if (!string.IsNullOrWhiteSpace(update.DocumentFileId))
        {
            return new TelegramBroadcastAttachmentCreate(
                TelegramBroadcastAttachmentType.Document,
                update.DocumentFileId,
                update.DocumentFileUniqueId,
                SafeFileName(update.DocumentFileName),
                SafeMimeType(update.DocumentMimeType),
                update.DocumentFileSize,
                SortOrder: 0);
        }

        if (!string.IsNullOrWhiteSpace(update.PhotoFileId))
        {
            return new TelegramBroadcastAttachmentCreate(
                TelegramBroadcastAttachmentType.Photo,
                update.PhotoFileId,
                update.PhotoFileUniqueId,
                FileName: null,
                MimeType: null,
                update.PhotoFileSize,
                SortOrder: 0);
        }

        if (!string.IsNullOrWhiteSpace(update.VideoFileId))
        {
            return new TelegramBroadcastAttachmentCreate(
                TelegramBroadcastAttachmentType.Video,
                update.VideoFileId,
                update.VideoFileUniqueId,
                SafeFileName(update.VideoFileName),
                SafeMimeType(update.VideoMimeType),
                update.VideoFileSize,
                SortOrder: 0);
        }

        return null;
    }

    private static EquipmentDiagnosticTelegramReplyMarkup AttachmentPromptKeyboard(long campaignId) =>
        new(InlineKeyboard:
        [
            [Button("👀 Предпросмотр", $"{PreviewPrefix}{campaignId}")],
            [Button("❌ Отмена", $"{CancelPrefix}{campaignId}")]
        ]);

    private static string AttachmentSummary(IReadOnlyList<TelegramBroadcastAttachmentSnapshot> attachments)
    {
        if (attachments.Count == 0)
        {
            return "Вложения: нет";
        }

        var builder = new StringBuilder("Вложения:");
        foreach (var attachment in attachments.OrderBy(item => item.SortOrder).ThenBy(item => item.Id))
        {
            builder.AppendLine();
            builder.Append("- ");
            builder.Append(AttachmentTypeLabel(attachment.AttachmentType));
            var name = SafeFileName(attachment.FileName);
            if (!string.IsNullOrWhiteSpace(name))
            {
                builder.Append(": ");
                builder.Append(name);
            }
            if (attachment.FileSize is > 0)
            {
                builder.Append(" (");
                builder.Append(FormatFileSize(attachment.FileSize.Value));
                builder.Append(')');
            }
        }

        return builder.ToString();
    }

    private static string AttachmentTypeLabel(TelegramBroadcastAttachmentType type) =>
        type switch
        {
            TelegramBroadcastAttachmentType.Photo => "фото",
            TelegramBroadcastAttachmentType.Document => "файл",
            TelegramBroadcastAttachmentType.Video => "видео",
            _ => "вложение"
        };

    private static string FormatFileSize(long fileSize)
    {
        if (fileSize >= 1024 * 1024)
        {
            return $"{fileSize / 1024d / 1024d:0.#} MB";
        }

        if (fileSize >= 1024)
        {
            return $"{fileSize / 1024d:0.#} KB";
        }

        return $"{fileSize} B";
    }

    private static string? SafeFileName(string? fileName) =>
        string.IsNullOrWhiteSpace(fileName)
            ? null
            : Path.GetFileName(fileName.Trim());

    private static string? SafeMimeType(string? mimeType) =>
        string.IsNullOrWhiteSpace(mimeType)
            ? null
            : mimeType.Trim().Length <= 128
                ? mimeType.Trim()
                : mimeType.Trim()[..128];

    private static bool TryParseCampaign(string? callbackData, string prefix, out long campaignId)
    {
        campaignId = 0;
        return callbackData?.StartsWith(prefix, StringComparison.Ordinal) == true &&
            long.TryParse(callbackData[prefix.Length..], out campaignId);
    }

    private static bool TryParseAudience(
        string value,
        out TelegramBroadcastAudienceKind kind,
        out TelegramUserRole? role)
    {
        kind = TelegramBroadcastAudienceKind.Role;
        role = value switch
        {
            "own" => TelegramUserRole.Owner,
            "adm" => TelegramUserRole.Admin,
            "eng" => TelegramUserRole.Engineer,
            "ins" => TelegramUserRole.Installer,
            "con" => TelegramUserRole.Consumer,
            _ => null
        };
        if (value == "all")
        {
            kind = TelegramBroadcastAudienceKind.AllActive;
            return true;
        }

        return role is not null;
    }

    private static string AudienceLabel(TelegramBroadcastCampaignSnapshot campaign) =>
        campaign.AudienceKind == TelegramBroadcastAudienceKind.AllActive
            ? "Все активные"
            : campaign.AudienceRole?.ToString() ?? "Role";

    private static string UnavailableReason(TelegramUserListItem user)
    {
        if (!user.IsEnabled)
        {
            return "inactive";
        }

        if (user.IsBlocked)
        {
            return "blocked";
        }

        if (!user.HasPrivateChat)
        {
            return "no private chat id";
        }

        return "duplicate private chat id";
    }

    private static string SanitizeError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Telegram send failed.";
        }

        var compact = message.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return compact.Length <= 200 ? compact : string.Concat(compact.AsSpan(0, 197), "...");
    }

    private static EquipmentDiagnosticTelegramInlineKeyboardButton Button(string text, string callbackData) =>
        new(text, callbackData);

    private sealed record BroadcastAudience(
        IReadOnlyList<TelegramUserListItem> Reachable,
        IReadOnlyList<TelegramUserListItem> Unavailable);
}

public sealed record TelegramBroadcastResult(
    string Text,
    EquipmentDiagnosticTelegramReplyMarkup? ReplyMarkup = null,
    string? CallbackAnswerText = null);
