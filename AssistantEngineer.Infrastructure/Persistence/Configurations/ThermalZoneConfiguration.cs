using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.ThermalZones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public class ThermalZoneConfiguration : IEntityTypeConfiguration<ThermalZone>
{
    public void Configure(EntityTypeBuilder<ThermalZone> builder)
    {
        builder.ToTable("ThermalZones");

        builder.HasKey(zone => zone.Id);

        builder.Property(zone => zone.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Ignore(zone => zone.AssignedRooms);

        builder.HasOne(zone => zone.Building)
            .WithMany(building => building.ThermalZones)
            .HasForeignKey(zone => zone.BuildingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(zone => zone.Rooms)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "ThermalZoneRooms",
                right => right
                    .HasOne<Room>()
                    .WithMany()
                    .HasForeignKey("RoomId")
                    .OnDelete(DeleteBehavior.Cascade),
                left => left
                    .HasOne<ThermalZone>()
                    .WithMany()
                    .HasForeignKey("ThermalZoneId")
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("ThermalZoneRooms");
                    join.HasKey("ThermalZoneId", "RoomId");
                    join.HasIndex("RoomId").IsUnique();
                });

        builder.Navigation(zone => zone.Rooms)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}