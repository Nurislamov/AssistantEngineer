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
        builder.Property(zone => zone.Name).IsRequired().HasMaxLength(200);
        builder.Ignore(zone => zone.RoomIds);
        builder.Ignore(zone => zone.AssignedRooms);

        builder.HasOne(zone => zone.Building)
            .WithMany(building => building.ThermalZones)
            .HasForeignKey(zone => zone.BuildingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(zone => zone.Rooms)
            .WithOne(room => room.ThermalZone)
            .HasForeignKey(room => room.ThermalZoneId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(zone => zone.Rooms)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
