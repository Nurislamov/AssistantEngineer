using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public sealed class TelegramDiagnosticCaseConfiguration : IEntityTypeConfiguration<TelegramDiagnosticCaseEntity>
{
    public void Configure(EntityTypeBuilder<TelegramDiagnosticCaseEntity> builder)
    {
        builder.ToTable("TelegramDiagnosticCases");

        builder.HasKey(diagnosticCase => diagnosticCase.Id);
        builder.Property(diagnosticCase => diagnosticCase.Id)
            .ValueGeneratedOnAdd();

        builder.Property(diagnosticCase => diagnosticCase.TelegramUserId)
            .IsRequired();

        builder.Property(diagnosticCase => diagnosticCase.Source)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(diagnosticCase => diagnosticCase.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(diagnosticCase => diagnosticCase.UserRoleAtCreation)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(diagnosticCase => diagnosticCase.ResponseMode)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(diagnosticCase => diagnosticCase.Code)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(diagnosticCase => diagnosticCase.Manufacturer)
            .HasMaxLength(128);

        builder.Property(diagnosticCase => diagnosticCase.EquipmentType)
            .HasMaxLength(128);

        builder.Property(diagnosticCase => diagnosticCase.DisplayContext)
            .HasMaxLength(128);

        builder.Property(diagnosticCase => diagnosticCase.ResultSummary)
            .HasMaxLength(512);

        builder.Property(diagnosticCase => diagnosticCase.NormalizedRequestJson)
            .HasColumnType("jsonb");

        builder.Property(diagnosticCase => diagnosticCase.PhoneWasSaved)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(diagnosticCase => diagnosticCase.PhoneNumberSource)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(diagnosticCase => diagnosticCase.CreatedAt)
            .IsRequired();

        builder.HasIndex(diagnosticCase => diagnosticCase.TelegramUserId);
        builder.HasIndex(diagnosticCase => diagnosticCase.CreatedAt);
        builder.HasIndex(diagnosticCase => diagnosticCase.Status);
        builder.HasIndex(diagnosticCase => new { diagnosticCase.TelegramUserId, diagnosticCase.CreatedAt });

        builder.HasOne<TelegramUserEntity>()
            .WithMany()
            .HasForeignKey(diagnosticCase => diagnosticCase.TelegramUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<TelegramConversationSessionEntity>()
            .WithMany()
            .HasForeignKey(diagnosticCase => diagnosticCase.TelegramConversationSessionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
