namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Broadcasts;

public class InMemoryTelegramBroadcastStore : ITelegramBroadcastStore
{
    private readonly object _sync = new();
    private readonly Dictionary<long, TelegramBroadcastCampaignEntity> _campaigns = [];
    private readonly Dictionary<long, List<TelegramBroadcastRecipientEntity>> _recipients = [];
    private readonly Dictionary<long, List<TelegramBroadcastAttachmentEntity>> _attachments = [];
    private long _lastCampaignId;
    private long _lastRecipientId;
    private long _lastAttachmentId;

    public Task<TelegramBroadcastCampaignSnapshot> CreateDraftAsync(
        long createdByTelegramUserId,
        long? createdByTelegramChatId,
        TelegramBroadcastAudienceKind audienceKind,
        Users.TelegramUserRole? audienceRole,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var campaign = new TelegramBroadcastCampaignEntity
            {
                Id = ++_lastCampaignId,
                CreatedByTelegramUserId = createdByTelegramUserId,
                CreatedByTelegramChatId = createdByTelegramChatId,
                AudienceKind = audienceKind,
                AudienceRole = audienceRole,
                Status = TelegramBroadcastCampaignStatus.Draft,
                CreatedAt = createdAt
            };
            _campaigns.Add(campaign.Id, campaign);
            _recipients.Add(campaign.Id, []);
            _attachments.Add(campaign.Id, []);
            return Task.FromResult(ToSnapshot(campaign));
        }
    }

    public Task<TelegramBroadcastCampaignSnapshot?> GetCampaignAsync(
        long campaignId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_campaigns.TryGetValue(campaignId, out var campaign) ? ToSnapshot(campaign) : null);
        }
    }

    public Task<TelegramBroadcastCampaignSnapshot?> SetReadyAsync(
        long campaignId,
        string text,
        int totalRecipients,
        int skippedCount,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (!_campaigns.TryGetValue(campaignId, out var campaign))
            {
                return Task.FromResult<TelegramBroadcastCampaignSnapshot?>(null);
            }

            campaign.Text = text;
            campaign.Status = TelegramBroadcastCampaignStatus.Ready;
            campaign.TotalRecipients = totalRecipients;
            campaign.SkippedCount = skippedCount;
            return Task.FromResult<TelegramBroadcastCampaignSnapshot?>(ToSnapshot(campaign));
        }
    }

    public Task<TelegramBroadcastCampaignSnapshot?> MarkSendingAsync(
        long campaignId,
        DateTimeOffset confirmedAt,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (!_campaigns.TryGetValue(campaignId, out var campaign))
            {
                return Task.FromResult<TelegramBroadcastCampaignSnapshot?>(null);
            }

            campaign.Status = TelegramBroadcastCampaignStatus.Sending;
            campaign.ConfirmedAt = confirmedAt;
            campaign.StartedAt = confirmedAt;
            return Task.FromResult<TelegramBroadcastCampaignSnapshot?>(ToSnapshot(campaign));
        }
    }

    public Task<TelegramBroadcastCampaignSnapshot?> CompleteAsync(
        long campaignId,
        int sentCount,
        int skippedCount,
        int failedCount,
        string? lastError,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (!_campaigns.TryGetValue(campaignId, out var campaign))
            {
                return Task.FromResult<TelegramBroadcastCampaignSnapshot?>(null);
            }

            campaign.Status = TelegramBroadcastCampaignStatus.Completed;
            campaign.SentCount = sentCount;
            campaign.SkippedCount = skippedCount;
            campaign.FailedCount = failedCount;
            campaign.LastError = lastError;
            campaign.CompletedAt = completedAt;
            return Task.FromResult<TelegramBroadcastCampaignSnapshot?>(ToSnapshot(campaign));
        }
    }

    public Task<TelegramBroadcastCampaignSnapshot?> CancelAsync(
        long campaignId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (!_campaigns.TryGetValue(campaignId, out var campaign))
            {
                return Task.FromResult<TelegramBroadcastCampaignSnapshot?>(null);
            }

            campaign.Status = TelegramBroadcastCampaignStatus.Cancelled;
            return Task.FromResult<TelegramBroadcastCampaignSnapshot?>(ToSnapshot(campaign));
        }
    }

    public Task<IReadOnlyList<TelegramBroadcastRecipientSnapshot>> ReplaceRecipientsAsync(
        long campaignId,
        IReadOnlyList<TelegramBroadcastRecipientCreate> recipients,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var stored = recipients
                .Select(recipient => new TelegramBroadcastRecipientEntity
                {
                    Id = ++_lastRecipientId,
                    CampaignId = campaignId,
                    TelegramUserId = recipient.TelegramUserId,
                    TelegramChatId = recipient.TelegramChatId,
                    Role = recipient.Role,
                    Status = recipient.Status,
                    SkipReason = recipient.SkipReason,
                    CreatedAt = createdAt
                })
                .ToList();
            _recipients[campaignId] = stored;
            return Task.FromResult<IReadOnlyList<TelegramBroadcastRecipientSnapshot>>(stored.Select(ToSnapshot).ToArray());
        }
    }

    public Task<TelegramBroadcastRecipientSnapshot?> UpdateRecipientAsync(
        TelegramBroadcastRecipientUpdate update,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var recipient = _recipients.Values.SelectMany(value => value).FirstOrDefault(item => item.Id == update.RecipientId);
            if (recipient is null)
            {
                return Task.FromResult<TelegramBroadcastRecipientSnapshot?>(null);
            }

            recipient.Status = update.Status;
            recipient.ErrorCode = update.ErrorCode;
            recipient.ErrorMessage = update.ErrorMessage;
            recipient.SentAt = update.SentAt;
            return Task.FromResult<TelegramBroadcastRecipientSnapshot?>(ToSnapshot(recipient));
        }
    }

    public Task<IReadOnlyList<TelegramBroadcastRecipientSnapshot>> ListRecipientsAsync(
        long campaignId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<TelegramBroadcastRecipientSnapshot>>(
                _recipients.GetValueOrDefault(campaignId, []).Select(ToSnapshot).ToArray());
        }
    }

    public Task<TelegramBroadcastAttachmentSnapshot> AddAttachmentAsync(
        long campaignId,
        TelegramBroadcastAttachmentCreate attachment,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (!_attachments.ContainsKey(campaignId))
            {
                _attachments[campaignId] = [];
            }

            var entity = new TelegramBroadcastAttachmentEntity
            {
                Id = ++_lastAttachmentId,
                CampaignId = campaignId,
                AttachmentType = attachment.AttachmentType,
                FileId = attachment.FileId,
                FileUniqueId = attachment.FileUniqueId,
                FileName = attachment.FileName,
                MimeType = attachment.MimeType,
                FileSize = attachment.FileSize,
                SortOrder = attachment.SortOrder,
                CreatedAt = createdAt
            };
            _attachments[campaignId].Add(entity);
            return Task.FromResult(ToSnapshot(entity));
        }
    }

    public Task<IReadOnlyList<TelegramBroadcastAttachmentSnapshot>> ListAttachmentsAsync(
        long campaignId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<TelegramBroadcastAttachmentSnapshot>>(
                _attachments.GetValueOrDefault(campaignId, [])
                    .OrderBy(attachment => attachment.SortOrder)
                    .ThenBy(attachment => attachment.Id)
                    .Select(ToSnapshot)
                    .ToArray());
        }
    }

    private static TelegramBroadcastCampaignSnapshot ToSnapshot(TelegramBroadcastCampaignEntity campaign) =>
        new(
            campaign.Id,
            campaign.CreatedByTelegramUserId,
            campaign.CreatedByTelegramChatId,
            campaign.AudienceKind,
            campaign.AudienceRole,
            campaign.Text,
            campaign.Status,
            campaign.CreatedAt,
            campaign.ConfirmedAt,
            campaign.StartedAt,
            campaign.CompletedAt,
            campaign.TotalRecipients,
            campaign.SentCount,
            campaign.SkippedCount,
            campaign.FailedCount,
            campaign.LastError);

    private static TelegramBroadcastRecipientSnapshot ToSnapshot(TelegramBroadcastRecipientEntity recipient) =>
        new(
            recipient.Id,
            recipient.CampaignId,
            recipient.TelegramUserId,
            recipient.TelegramChatId,
            recipient.Role,
            recipient.Status,
            recipient.SkipReason,
            recipient.ErrorCode,
            recipient.ErrorMessage,
            recipient.SentAt,
            recipient.CreatedAt);

    private static TelegramBroadcastAttachmentSnapshot ToSnapshot(TelegramBroadcastAttachmentEntity attachment) =>
        new(
            attachment.Id,
            attachment.CampaignId,
            attachment.AttachmentType,
            attachment.FileId,
            attachment.FileUniqueId,
            attachment.FileName,
            attachment.MimeType,
            attachment.FileSize,
            attachment.SortOrder,
            attachment.CreatedAt);
}
