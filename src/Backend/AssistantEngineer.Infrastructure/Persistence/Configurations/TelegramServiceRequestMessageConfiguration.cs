using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public sealed class TelegramServiceRequestMessageConfiguration : IEntityTypeConfiguration<TelegramServiceRequestMessageEntity>
{
    public void Configure(EntityTypeBuilder<TelegramServiceRequestMessageEntity> builder)
    {
        builder.ToTable("TelegramServiceRequestMessages");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedOnAdd();
        builder.Property(item => item.Direction).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(item => item.SenderRole).HasConversion<string>().HasMaxLength(32);
        builder.Property(item => item.Text).HasMaxLength(4096).IsRequired();
        builder.Property(item => item.CreatedAt).IsRequired();
        builder.HasIndex(item => new { item.ServiceRequestId, item.CreatedAt });
        builder.HasIndex(item => new { item.SenderTelegramUserId, item.CreatedAt });
        builder.HasOne<TelegramServiceRequestEntity>()
            .WithMany()
            .HasForeignKey(item => item.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<TelegramUserEntity>()
            .WithMany()
            .HasForeignKey(item => item.SenderTelegramUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class TelegramServiceRequestPendingConfiguration : IEntityTypeConfiguration<TelegramServiceRequestPendingEntity>
{
    public void Configure(EntityTypeBuilder<TelegramServiceRequestPendingEntity> builder)
    {
        builder.ToTable("TelegramServiceRequestPending");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedOnAdd();
        builder.Property(item => item.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(item => item.PendingText).HasMaxLength(4096);
        builder.Property(item => item.CreatedAt).IsRequired();
        builder.Property(item => item.ExpiresAt).IsRequired();
        builder.HasIndex(item => item.TelegramUserId).IsUnique();
        builder.HasOne<TelegramUserEntity>()
            .WithMany()
            .HasForeignKey(item => item.TelegramUserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<TelegramServiceRequestEntity>()
            .WithMany()
            .HasForeignKey(item => item.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
