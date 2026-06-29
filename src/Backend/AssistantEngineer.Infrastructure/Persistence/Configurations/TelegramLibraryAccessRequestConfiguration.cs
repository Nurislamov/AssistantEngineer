using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public sealed class TelegramLibraryAccessRequestConfiguration : IEntityTypeConfiguration<TelegramLibraryAccessRequestEntity>
{
    public void Configure(EntityTypeBuilder<TelegramLibraryAccessRequestEntity> builder)
    {
        builder.ToTable("TelegramLibraryAccessRequests");

        builder.HasKey(request => request.Id);
        builder.Property(request => request.Id)
            .ValueGeneratedOnAdd();

        builder.Property(request => request.RequestedRole)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasDefaultValue(TelegramUserRole.Installer)
            .IsRequired();

        builder.Property(request => request.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasDefaultValue(TelegramLibraryAccessRequestStatus.Pending)
            .IsRequired();

        builder.Property(request => request.Message)
            .HasMaxLength(512);

        builder.Property(request => request.CreatedAt)
            .IsRequired();

        builder.HasIndex(request => new { request.TelegramUserId, request.Status });
        builder.HasIndex(request => new { request.Status, request.CreatedAt });
        builder.HasIndex(request => request.TelegramChatId);
    }
}
