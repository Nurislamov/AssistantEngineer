using AssistantEngineer.Modules.Buildings.Domain.ThermalZones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public class ThermalZoneRoomConfiguration : IEntityTypeConfiguration<ThermalZoneRoom>
{
    public void Configure(EntityTypeBuilder<ThermalZoneRoom> builder)
    {
        builder.ToTable("ThermalZoneRooms");
        builder.HasKey(room => new { room.ThermalZoneId, room.RoomId });
        builder.HasIndex(room => room.RoomId).IsUnique();

        builder.HasOne(room => room.ThermalZone)
            .WithMany(zone => zone.Rooms)
            .HasForeignKey(room => room.ThermalZoneId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(room => room.Room)
            .WithMany()
            .HasForeignKey(room => room.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
