using AssistantEngineer.Modules.Buildings.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(r => new { r.FloorId, r.Name }).IsUnique();

        // Value objects are mapped as owned types.
        builder.OwnsOne(r => r.Area, area =>
        {
            area.Property(a => a.SquareMeters).HasColumnName("AreaM2").IsRequired();
        });

        builder.OwnsOne(r => r.IndoorTemperature, temp =>
        {
            temp.Property(t => t.Celsius).HasColumnName("IndoorTemperatureC").IsRequired();
        });

        builder.OwnsOne(r => r.OutdoorTemperatureOverride, temp =>
        {
            temp.Property(t => t.Celsius).HasColumnName("OutdoorTemperatureOverrideC");
        });

        builder.OwnsOne(r => r.EquipmentLoad, p =>
        {
            p.Property(pw => pw.Watts).HasColumnName("EquipmentLoadW").IsRequired();
        });

        builder.OwnsOne(r => r.LightingLoad, p =>
        {
            p.Property(pw => pw.Watts).HasColumnName("LightingLoadW").IsRequired();
        });

        builder.OwnsOne(r => r.GroundContactMetadata, ground =>
        {
            ground.Property(g => g.ContactType)
                .HasColumnName("GroundContactType")
                .HasConversion<string>();
            ground.Property(g => g.ExposedPerimeterM).HasColumnName("GroundExposedPerimeterM");
            ground.Property(g => g.BurialDepthM).HasColumnName("GroundBurialDepthM");
            ground.Property(g => g.WallHeightBelowGradeM).HasColumnName("GroundWallHeightBelowGradeM");
            ground.Property(g => g.HorizontalInsulationWidthM).HasColumnName("GroundHorizontalInsulationWidthM");
            ground.Property(g => g.PerimeterInsulationDepthM).HasColumnName("GroundPerimeterInsulationDepthM");
            ground.Property(g => g.UnderfloorVentilationAirChangesPerHour)
                .HasColumnName("GroundUnderfloorVentilationAirChangesPerHour");
        });

        builder.Property(r => r.HeightM).IsRequired();
        builder.Property(r => r.PeopleCount).IsRequired();
        builder.Property(r => r.Type).IsRequired();
        builder.Property(r => r.Type).HasConversion<string>();

        builder.HasOne(r => r.Floor)
            .WithMany(f => f.Rooms)
            .HasForeignKey(r => r.FloorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.OccupancySchedule)
            .WithMany()
            .HasForeignKey(r => r.OccupancyScheduleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.EquipmentSchedule)
            .WithMany()
            .HasForeignKey(r => r.EquipmentScheduleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.LightingSchedule)
            .WithMany()
            .HasForeignKey(r => r.LightingScheduleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.VentilationParameters)
            .WithMany()
            .HasForeignKey(r => r.VentilationParametersId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
