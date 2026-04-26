using AssistantEngineer.Modules.Buildings.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public class WallConfiguration : IEntityTypeConfiguration<Wall>
{
    public void Configure(EntityTypeBuilder<Wall> builder)
    {
        builder.ToTable("Walls");
        builder.HasKey(w => w.Id);

        builder.OwnsOne(w => w.Area, area =>
        {
            area.Property(a => a.SquareMeters).HasColumnName("AreaM2").IsRequired();
        });

        builder.OwnsOne(w => w.UValue, u =>
        {
            u.Property(value => value.Value).HasColumnName("UValue").IsRequired();
        });

        builder.Property(w => w.IsExternal).IsRequired();
        builder.Property(w => w.Orientation).IsRequired().HasConversion<string>();
        builder.Property(w => w.BoundaryType).IsRequired().HasConversion<string>();

        builder.HasOne(w => w.Room)
            .WithMany(r => r.Walls)
            .HasForeignKey(w => w.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.AdjacentRoom)
            .WithMany()
            .HasForeignKey(w => w.AdjacentRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.ConstructionAssembly)
            .WithMany()
            .HasForeignKey(w => w.ConstructionAssemblyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}