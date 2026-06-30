using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Broadcasts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public sealed class TelegramBroadcastRecipientConfiguration : IEntityTypeConfiguration<TelegramBroadcastRecipientEntity>
{
    public void Configure(EntityTypeBuilder<TelegramBroadcastRecipientEntity> builder)
    {
        builder.ToTable("TelegramBroadcastRecipients");
        builder.HasKey(recipient => recipient.Id);
        builder.Property(recipient => recipient.Id).ValueGeneratedOnAdd();
        builder.Property(recipient => recipient.CampaignId).IsRequired();
        builder.Property(recipient => recipient.TelegramUserId).IsRequired();
        builder.Property(recipient => recipient.TelegramChatId);
        builder.Property(recipient => recipient.Role)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(recipient => recipient.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(recipient => recipient.SkipReason).HasMaxLength(128);
        builder.Property(recipient => recipient.ErrorCode).HasMaxLength(64);
        builder.Property(recipient => recipient.ErrorMessage).HasMaxLength(512);
        builder.Property(recipient => recipient.CreatedAt).IsRequired();
        builder.HasIndex(recipient => recipient.CampaignId);
        builder.HasIndex(recipient => new { recipient.CampaignId, recipient.Status });
        builder.HasIndex(recipient => recipient.TelegramUserId);
        builder.HasOne<TelegramBroadcastCampaignEntity>()
            .WithMany()
            .HasForeignKey(recipient => recipient.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<TelegramUserEntity>()
            .WithMany()
            .HasForeignKey(recipient => recipient.TelegramUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
