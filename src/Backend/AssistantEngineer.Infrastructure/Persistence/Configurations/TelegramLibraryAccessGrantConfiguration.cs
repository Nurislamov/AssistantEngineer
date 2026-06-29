using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public sealed class TelegramLibraryAccessGrantConfiguration : IEntityTypeConfiguration<TelegramLibraryAccessGrantEntity>
{
    public void Configure(EntityTypeBuilder<TelegramLibraryAccessGrantEntity> builder)
    {
        builder.ToTable("TelegramLibraryAccessGrants");

        builder.HasKey(grant => grant.Id);
        builder.Property(grant => grant.Id)
            .ValueGeneratedOnAdd();

        builder.Property(grant => grant.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(grant => grant.Reason)
            .HasMaxLength(256);

        builder.Property(grant => grant.CreatedAt)
            .IsRequired();

        builder.Property(grant => grant.UpdatedAt)
            .IsRequired();

        builder.HasIndex(grant => new { grant.TelegramUserId, grant.IsActive });
        builder.HasIndex(grant => grant.GrantedByTelegramUserId);
    }
}
