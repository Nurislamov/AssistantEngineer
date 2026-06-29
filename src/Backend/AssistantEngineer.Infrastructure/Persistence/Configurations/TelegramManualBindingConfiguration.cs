using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public sealed class TelegramManualBindingConfiguration : IEntityTypeConfiguration<TelegramManualBindingEntity>
{
    public void Configure(EntityTypeBuilder<TelegramManualBindingEntity> builder)
    {
        builder.ToTable("TelegramManualBindings");

        builder.HasKey(binding => binding.Id);
        builder.Property(binding => binding.Id)
            .ValueGeneratedOnAdd();

        builder.Property(binding => binding.ManualId)
            .HasMaxLength(160);

        builder.Property(binding => binding.Brand)
            .HasMaxLength(64);

        builder.Property(binding => binding.Series)
            .HasMaxLength(128);

        builder.Property(binding => binding.TelegramFileId)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(binding => binding.TelegramFileUniqueId)
            .HasMaxLength(256);

        builder.Property(binding => binding.FileName)
            .HasMaxLength(256);

        builder.Property(binding => binding.ContentType)
            .HasMaxLength(128);

        builder.Property(binding => binding.RegisteredByRole)
            .HasMaxLength(32);

        builder.Property(binding => binding.Source)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(binding => binding.Title)
            .HasMaxLength(256);

        builder.Property(binding => binding.DocumentType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasDefaultValue(TelegramLibraryDocumentType.ServiceManual)
            .IsRequired();

        builder.Property(binding => binding.MinRole)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasDefaultValue(TelegramUserRole.Engineer)
            .IsRequired();

        builder.Property(binding => binding.IsLibraryVisible)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(binding => binding.CanUseForDiagnostics)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(binding => binding.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(binding => binding.CreatedAt)
            .IsRequired();

        builder.HasIndex(binding => binding.ManualId);
        builder.HasIndex(binding => new { binding.Brand, binding.Series, binding.IsActive });
        builder.HasIndex(binding => new { binding.Brand, binding.Series, binding.DocumentType, binding.CanUseForDiagnostics, binding.IsActive });
    }
}
