using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.OperatorInbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public sealed class TelegramOperatorInboxThreadConfiguration : IEntityTypeConfiguration<TelegramOperatorInboxThreadEntity>
{
    public void Configure(EntityTypeBuilder<TelegramOperatorInboxThreadEntity> builder)
    {
        builder.ToTable("TelegramOperatorInboxThreads");

        builder.HasKey(thread => thread.Id);
        builder.Property(thread => thread.Id)
            .ValueGeneratedOnAdd();

        builder.Property(thread => thread.TelegramChatId)
            .IsRequired();

        builder.Property(thread => thread.UserDisplayName)
            .HasMaxLength(256);

        builder.Property(thread => thread.Username)
            .HasMaxLength(128);

        builder.Property(thread => thread.UserRole)
            .HasMaxLength(32);

        builder.Property(thread => thread.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasDefaultValue(TelegramOperatorInboxThreadStatus.Open)
            .IsRequired();

        builder.Property(thread => thread.CreatedAt)
            .IsRequired();

        builder.Property(thread => thread.UpdatedAt)
            .IsRequired();

        builder.HasIndex(thread => thread.TelegramChatId);
        builder.HasIndex(thread => thread.CreatedAt);
    }
}
