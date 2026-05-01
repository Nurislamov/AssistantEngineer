using AssistantEngineer.Modules.Buildings.Domain.Climate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public class AnnualHourlyDataConfiguration : IEntityTypeConfiguration<AnnualHourlyData>
{
    public void Configure(EntityTypeBuilder<AnnualHourlyData> builder)
    {
        builder.ToTable("AnnualHourlyData");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.HourOfYear).IsRequired();

        builder.HasIndex(x => new { x.AnnualClimateDataId, x.HourOfYear }).IsUnique();

        builder.OwnsOne(x => x.Weather, weather =>
        {
            weather.Property(x => x.DryBulbTemperature)
                .HasColumnName("DryBulbTemperature")
                .IsRequired();

            weather.Property(x => x.DirectSolarRadiation)
                .HasColumnName("DirectSolarRadiation")
                .IsRequired();

            weather.Property(x => x.DiffuseSolarRadiation)
                .HasColumnName("DiffuseSolarRadiation")
                .IsRequired();

            weather.Property(x => x.RelativeHumidityPercent)
                .HasColumnName("RelativeHumidityPercent");

            weather.Property(x => x.AtmosphericPressurePa)
                .HasColumnName("AtmosphericPressurePa");

            weather.Property(x => x.WindSpeedMPerS)
                .HasColumnName("WindSpeedMPerS");

            weather.Property(x => x.WindDirectionDegrees)
                .HasColumnName("WindDirectionDegrees");

            weather.Property(x => x.HorizontalInfraredRadiationWPerM2)
                .HasColumnName("HorizontalInfraredRadiationWPerM2");

            weather.Property(x => x.SkyTemperatureC)
                .HasColumnName("SkyTemperatureC");

            weather.Property(x => x.TotalSkyCoverTenths)
                .HasColumnName("TotalSkyCoverTenths");

            weather.Property(x => x.OpaqueSkyCoverTenths)
                .HasColumnName("OpaqueSkyCoverTenths");
        });

        builder.Navigation(x => x.Weather).IsRequired();
    }
}