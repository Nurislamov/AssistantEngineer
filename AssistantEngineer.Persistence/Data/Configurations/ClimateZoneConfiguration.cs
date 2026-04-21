using AssistantEngineer.Modules.Buildings.Domain.Climate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Persistence.Data.Configurations;

public class ClimateZoneConfiguration : IEntityTypeConfiguration<ClimateZone>
{
    public void Configure(EntityTypeBuilder<ClimateZone> builder)
    {
        builder.ToTable("ClimateZones");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);

        builder.OwnsOne(c => c.SummerDesignTemperature, temp =>
        {
            temp.Property(t => t.Celsius).HasColumnName("SummerDesignTemperatureC").IsRequired();
        });

        builder.OwnsOne(c => c.WinterDesignTemperature, temp =>
        {
            temp.Property(t => t.Celsius).HasColumnName("WinterDesignTemperatureC").IsRequired();
        });
    }
}