using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public sealed class TelegramServiceRequestEventConfiguration : IEntityTypeConfiguration<TelegramServiceRequestEventEntity>
{
    public void Configure(EntityTypeBuilder<TelegramServiceRequestEventEntity> builder)
    {
        builder.ToTable("TelegramServiceRequestEvents");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedOnAdd();
        builder.Property(item => item.ServiceRequestId).IsRequired();
        builder.Property(item => item.EventType)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(item => item.OldStatus)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(item => item.NewStatus)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(item => item.IsSuccessful)
            .HasDefaultValue(true)
            .IsRequired();
        builder.Property(item => item.Message).HasMaxLength(512);
        builder.Property(item => item.MetadataJson).HasColumnType("jsonb");
        builder.Property(item => item.CreatedAt).IsRequired();

        builder.HasIndex(item => new { item.ServiceRequestId, item.CreatedAt });
        builder.HasIndex(item => new { item.EventType, item.CreatedAt });
        builder.HasIndex(item => new { item.ActorTelegramUserId, item.CreatedAt });

        builder.HasOne<TelegramServiceRequestEntity>()
            .WithMany()
            .HasForeignKey(item => item.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<TelegramUserEntity>()
            .WithMany()
            .HasForeignKey(item => item.ActorTelegramUserId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<TelegramUserEntity>()
            .WithMany()
            .HasForeignKey(item => item.TargetTelegramUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
