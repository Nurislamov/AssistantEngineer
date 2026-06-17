using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public sealed class TelegramUserConfiguration : IEntityTypeConfiguration<TelegramUserEntity>
{
    public void Configure(EntityTypeBuilder<TelegramUserEntity> builder)
    {
        builder.ToTable("TelegramUsers");

        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id)
            .ValueGeneratedOnAdd();

        builder.Property(user => user.TelegramChatId)
            .IsRequired();

        builder.Property(user => user.TelegramUserId);

        builder.Property(user => user.Username)
            .HasMaxLength(128);

        builder.Property(user => user.FirstName)
            .HasMaxLength(128);

        builder.Property(user => user.LastName)
            .HasMaxLength(128);

        builder.Property(user => user.Role)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(user => user.IsEnabled)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(user => user.IsBlocked)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(user => user.PhoneNumber)
            .HasMaxLength(64);

        builder.Property(user => user.PhoneNumberVerified)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(user => user.CreatedAt)
            .IsRequired();

        builder.HasIndex(user => user.TelegramChatId)
            .IsUnique();

        builder.HasIndex(user => user.TelegramUserId);
        builder.HasIndex(user => user.Role);
        builder.HasIndex(user => new { user.IsEnabled, user.IsBlocked });
    }
}
