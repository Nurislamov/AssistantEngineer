using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Broadcasts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Infrastructure.Persistence.Repositories;

public sealed class EfTelegramBroadcastStore : ITelegramBroadcastStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EfTelegramBroadcastStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<TelegramBroadcastCampaignSnapshot> CreateDraftAsync(
        long createdByTelegramUserId,
        long? createdByTelegramChatId,
        TelegramBroadcastAudienceKind audienceKind,
        Modules.EquipmentDiagnostics.Application.Telegram.Users.TelegramUserRole? audienceRole,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var campaign = new TelegramBroadcastCampaignEntity
        {
            CreatedByTelegramUserId = createdByTelegramUserId,
            CreatedByTelegramChatId = createdByTelegramChatId,
            AudienceKind = audienceKind,
            AudienceRole = audienceRole,
            Status = TelegramBroadcastCampaignStatus.Draft,
            CreatedAt = createdAt
        };
        await context.TelegramBroadcastCampaigns.AddAsync(campaign, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return ToSnapshot(campaign);
    }

    public async Task<TelegramBroadcastCampaignSnapshot?> GetCampaignAsync(
        long campaignId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var campaign = await context.TelegramBroadcastCampaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == campaignId, cancellationToken);
        return campaign is null ? null : ToSnapshot(campaign);
    }

    public async Task<TelegramBroadcastCampaignSnapshot?> SetReadyAsync(
        long campaignId,
        string text,
        int totalRecipients,
        int skippedCount,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var campaign = await context.TelegramBroadcastCampaigns
            .FirstOrDefaultAsync(item => item.Id == campaignId, cancellationToken);
        if (campaign is null)
        {
            return null;
        }

        campaign.Text = text;
        campaign.Status = TelegramBroadcastCampaignStatus.Ready;
        campaign.TotalRecipients = totalRecipients;
        campaign.SkippedCount = skippedCount;
        await context.SaveChangesAsync(cancellationToken);
        return ToSnapshot(campaign);
    }

    public async Task<TelegramBroadcastCampaignSnapshot?> MarkSendingAsync(
        long campaignId,
        DateTimeOffset confirmedAt,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var campaign = await context.TelegramBroadcastCampaigns
            .FirstOrDefaultAsync(item => item.Id == campaignId, cancellationToken);
        if (campaign is null)
        {
            return null;
        }

        campaign.Status = TelegramBroadcastCampaignStatus.Sending;
        campaign.ConfirmedAt = confirmedAt;
        campaign.StartedAt = confirmedAt;
        await context.SaveChangesAsync(cancellationToken);
        return ToSnapshot(campaign);
    }

    public async Task<TelegramBroadcastCampaignSnapshot?> CompleteAsync(
        long campaignId,
        int sentCount,
        int skippedCount,
        int failedCount,
        string? lastError,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var campaign = await context.TelegramBroadcastCampaigns
            .FirstOrDefaultAsync(item => item.Id == campaignId, cancellationToken);
        if (campaign is null)
        {
            return null;
        }

        campaign.Status = TelegramBroadcastCampaignStatus.Completed;
        campaign.SentCount = sentCount;
        campaign.SkippedCount = skippedCount;
        campaign.FailedCount = failedCount;
        campaign.LastError = lastError;
        campaign.CompletedAt = completedAt;
        await context.SaveChangesAsync(cancellationToken);
        return ToSnapshot(campaign);
    }

    public async Task<TelegramBroadcastCampaignSnapshot?> CancelAsync(
        long campaignId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var campaign = await context.TelegramBroadcastCampaigns
            .FirstOrDefaultAsync(item => item.Id == campaignId, cancellationToken);
        if (campaign is null)
        {
            return null;
        }

        campaign.Status = TelegramBroadcastCampaignStatus.Cancelled;
        await context.SaveChangesAsync(cancellationToken);
        return ToSnapshot(campaign);
    }

    public async Task<IReadOnlyList<TelegramBroadcastRecipientSnapshot>> ReplaceRecipientsAsync(
        long campaignId,
        IReadOnlyList<TelegramBroadcastRecipientCreate> recipients,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.TelegramBroadcastRecipients
            .Where(item => item.CampaignId == campaignId)
            .ExecuteDeleteAsync(cancellationToken);
        var entities = recipients
            .Select(recipient => new TelegramBroadcastRecipientEntity
            {
                CampaignId = campaignId,
                TelegramUserId = recipient.TelegramUserId,
                TelegramChatId = recipient.TelegramChatId,
                Role = recipient.Role,
                Status = recipient.Status,
                SkipReason = recipient.SkipReason,
                CreatedAt = createdAt
            })
            .ToArray();
        await context.TelegramBroadcastRecipients.AddRangeAsync(entities, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entities.Select(ToSnapshot).ToArray();
    }

    public async Task<TelegramBroadcastRecipientSnapshot?> UpdateRecipientAsync(
        TelegramBroadcastRecipientUpdate update,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var recipient = await context.TelegramBroadcastRecipients
            .FirstOrDefaultAsync(item => item.Id == update.RecipientId, cancellationToken);
        if (recipient is null)
        {
            return null;
        }

        recipient.Status = update.Status;
        recipient.ErrorCode = update.ErrorCode;
        recipient.ErrorMessage = update.ErrorMessage;
        recipient.SentAt = update.SentAt;
        await context.SaveChangesAsync(cancellationToken);
        return ToSnapshot(recipient);
    }

    public async Task<IReadOnlyList<TelegramBroadcastRecipientSnapshot>> ListRecipientsAsync(
        long campaignId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var recipients = await context.TelegramBroadcastRecipients
            .AsNoTracking()
            .Where(item => item.CampaignId == campaignId)
            .OrderBy(item => item.Id)
            .ToArrayAsync(cancellationToken);
        return recipients.Select(ToSnapshot).ToArray();
    }

    public async Task<TelegramBroadcastAttachmentSnapshot> AddAttachmentAsync(
        long campaignId,
        TelegramBroadcastAttachmentCreate attachment,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = new TelegramBroadcastAttachmentEntity
        {
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
        await context.TelegramBroadcastAttachments.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return ToSnapshot(entity);
    }

    public async Task<IReadOnlyList<TelegramBroadcastAttachmentSnapshot>> ListAttachmentsAsync(
        long campaignId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var attachments = await context.TelegramBroadcastAttachments
            .AsNoTracking()
            .Where(item => item.CampaignId == campaignId)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .ToArrayAsync(cancellationToken);
        return attachments.Select(ToSnapshot).ToArray();
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
