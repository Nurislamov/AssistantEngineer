using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.OperatorInbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public sealed class TelegramOperatorInboxMessageConfiguration : IEntityTypeConfiguration<TelegramOperatorInboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<TelegramOperatorInboxMessageEntity> builder)
    {
        builder.ToTable("TelegramOperatorInboxMessages");

        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id)
            .ValueGeneratedOnAdd();

        builder.Property(message => message.ThreadId)
            .IsRequired();

        builder.Property(message => message.Direction)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(message => message.MessageKind)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(message => message.Text)
            .HasMaxLength(4000);

        builder.Property(message => message.CreatedAt)
            .IsRequired();

        builder.HasOne<TelegramOperatorInboxThreadEntity>()
            .WithMany()
            .HasForeignKey(message => message.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(message => message.ThreadId);
        builder.HasIndex(message => new { message.OperatorChatId, message.OperatorMessageId });
        builder.HasIndex(message => new { message.OperatorChatId, message.OperatorReplyToMessageId });
        builder.HasIndex(message => new { message.UserChatId, message.UserMessageId });
        builder.HasIndex(message => message.CreatedAt);
    }
}
