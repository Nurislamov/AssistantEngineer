using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public sealed class TelegramServiceRequestConfiguration : IEntityTypeConfiguration<TelegramServiceRequestEntity>
{
    public void Configure(EntityTypeBuilder<TelegramServiceRequestEntity> builder)
    {
        builder.ToTable("TelegramServiceRequests");

        builder.HasKey(request => request.Id);
        builder.Property(request => request.Id)
            .ValueGeneratedOnAdd();

        builder.Property(request => request.TelegramUserId).IsRequired();
        builder.Property(request => request.DiagnosticCaseId).IsRequired();

        builder.Property(request => request.Source)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(request => request.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(request => request.Code)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(request => request.Manufacturer).HasMaxLength(128);
        builder.Property(request => request.EquipmentType).HasMaxLength(128);
        builder.Property(request => request.DisplayContext).HasMaxLength(128);

        builder.Property(request => request.PhoneWasSaved)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(request => request.PhoneNumberSource)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(request => request.ContactPhoneLast4)
            .HasMaxLength(4);

        builder.Property(request => request.UserRoleAtCreation)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(request => request.AssignedTelegramUserId);
        builder.Property(request => request.AssignedByTelegramUserId);
        builder.Property(request => request.StatusUpdatedByTelegramUserId);

        builder.Property(request => request.CreatedAt).IsRequired();
        builder.Property(request => request.NotificationChatId);
        builder.Property(request => request.NotificationMessageId);

        builder.HasIndex(request => request.TelegramUserId);
        builder.HasIndex(request => new { request.DiagnosticCaseId, request.Status });
        builder.HasIndex(request => request.Status);
        builder.HasIndex(request => request.AssignedTelegramUserId);
        builder.HasIndex(request => new { request.TelegramUserId, request.CreatedAt });
        builder.HasIndex(request => request.DiagnosticCaseId)
            .HasDatabaseName("IX_TelegramServiceRequests_ActiveDiagnosticCase")
            .HasFilter("\"Status\" IN ('New', 'InProgress')")
            .IsUnique();

        builder.HasOne<TelegramUserEntity>()
            .WithMany()
            .HasForeignKey(request => request.TelegramUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<TelegramDiagnosticCaseEntity>()
            .WithMany()
            .HasForeignKey(request => request.DiagnosticCaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<TelegramUserEntity>()
            .WithMany()
            .HasForeignKey(request => request.AssignedTelegramUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<TelegramUserEntity>()
            .WithMany()
            .HasForeignKey(request => request.AssignedByTelegramUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<TelegramUserEntity>()
            .WithMany()
            .HasForeignKey(request => request.StatusUpdatedByTelegramUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
