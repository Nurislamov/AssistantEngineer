using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public sealed class TelegramConversationSessionConfiguration : IEntityTypeConfiguration<TelegramConversationSessionEntity>
{
    public void Configure(EntityTypeBuilder<TelegramConversationSessionEntity> builder)
    {
        builder.ToTable("TelegramConversationSessions");

        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id)
            .ValueGeneratedOnAdd();

        builder.Property(session => session.TelegramUserId)
            .IsRequired();

        builder.Property(session => session.State)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(session => session.CurrentCode)
            .HasMaxLength(64);

        builder.Property(session => session.SelectedManufacturer)
            .HasMaxLength(128);

        builder.Property(session => session.SelectedEquipmentType)
            .HasMaxLength(128);

        builder.Property(session => session.SelectedDisplayContext)
            .HasMaxLength(128);

        builder.Property(session => session.CandidateOptionsJson)
            .HasColumnType("jsonb");

        builder.Property(session => session.CreatedAt)
            .IsRequired();

        builder.Property(session => session.UpdatedAt)
            .IsRequired();

        builder.HasIndex(session => session.TelegramUserId)
            .IsUnique();

        builder.HasIndex(session => session.State);
        builder.HasIndex(session => session.UpdatedAt);
        builder.HasIndex(session => session.ExpiresAt);

        builder.HasOne<TelegramUserEntity>()
            .WithMany()
            .HasForeignKey(session => session.TelegramUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
