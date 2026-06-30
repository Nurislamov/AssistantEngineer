using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Broadcasts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public sealed class TelegramBroadcastCampaignConfiguration : IEntityTypeConfiguration<TelegramBroadcastCampaignEntity>
{
    public void Configure(EntityTypeBuilder<TelegramBroadcastCampaignEntity> builder)
    {
        builder.ToTable("TelegramBroadcastCampaigns");
        builder.HasKey(campaign => campaign.Id);
        builder.Property(campaign => campaign.Id).ValueGeneratedOnAdd();
        builder.Property(campaign => campaign.CreatedByTelegramUserId).IsRequired();
        builder.Property(campaign => campaign.CreatedByTelegramChatId);
        builder.Property(campaign => campaign.AudienceKind)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(campaign => campaign.AudienceRole)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(campaign => campaign.Text)
            .HasMaxLength(4000)
            .IsRequired();
        builder.Property(campaign => campaign.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(campaign => campaign.CreatedAt).IsRequired();
        builder.Property(campaign => campaign.LastError).HasMaxLength(512);
        builder.HasIndex(campaign => campaign.CreatedAt);
        builder.HasIndex(campaign => campaign.Status);
        builder.HasIndex(campaign => new { campaign.AudienceKind, campaign.AudienceRole });
        builder.HasOne<TelegramUserEntity>()
            .WithMany()
            .HasForeignKey(campaign => campaign.CreatedByTelegramUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
