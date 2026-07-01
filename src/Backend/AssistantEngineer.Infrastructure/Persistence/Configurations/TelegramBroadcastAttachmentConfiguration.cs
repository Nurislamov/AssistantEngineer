using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Broadcasts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public sealed class TelegramBroadcastAttachmentConfiguration : IEntityTypeConfiguration<TelegramBroadcastAttachmentEntity>
{
    public void Configure(EntityTypeBuilder<TelegramBroadcastAttachmentEntity> builder)
    {
        builder.ToTable("TelegramBroadcastAttachments");
        builder.HasKey(attachment => attachment.Id);
        builder.Property(attachment => attachment.Id).ValueGeneratedOnAdd();
        builder.Property(attachment => attachment.CampaignId).IsRequired();
        builder.Property(attachment => attachment.AttachmentType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(attachment => attachment.FileId)
            .HasMaxLength(512)
            .IsRequired();
        builder.Property(attachment => attachment.FileUniqueId).HasMaxLength(256);
        builder.Property(attachment => attachment.FileName).HasMaxLength(256);
        builder.Property(attachment => attachment.MimeType).HasMaxLength(128);
        builder.Property(attachment => attachment.SortOrder).IsRequired();
        builder.Property(attachment => attachment.CreatedAt).IsRequired();
        builder.HasIndex(attachment => attachment.CampaignId);
        builder.HasIndex(attachment => new { attachment.CampaignId, attachment.SortOrder });
        builder.HasOne<TelegramBroadcastCampaignEntity>()
            .WithMany()
            .HasForeignKey(attachment => attachment.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
