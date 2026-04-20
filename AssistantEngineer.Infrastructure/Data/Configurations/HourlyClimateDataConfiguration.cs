using AssistantEngineer.Domain.Models.Climate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Data.Configurations;

public class HourlyClimateDataConfiguration : IEntityTypeConfiguration<HourlyClimateData>
{
    public void Configure(EntityTypeBuilder<HourlyClimateData> builder)
    {
        builder.ToTable("HourlyClimateData");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DryBulbTemperature).IsRequired();
        builder.Property(x => x.DirectSolarRadiation).IsRequired();
        builder.Property(x => x.DiffuseSolarRadiation).IsRequired();
        builder.Property(x => x.RelativeHumidityPercent);
        builder.Property(x => x.AtmosphericPressurePa);
        builder.Property(x => x.WindSpeedMPerS);
        builder.Property(x => x.WindDirectionDegrees);
        builder.Property(x => x.HorizontalInfraredRadiationWPerM2);
        builder.Property(x => x.SkyTemperatureC);
        builder.Property(x => x.TotalSkyCoverTenths);
        builder.Property(x => x.OpaqueSkyCoverTenths);

        // Связь с месячными данными (опционально)
        builder.HasOne(x => x.ClimateData)
            .WithMany(d => d.HourlyData)
            .HasForeignKey(x => x.ClimateDataId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        // Связь с годовыми данными (опционально)
        builder.HasOne(x => x.AnnualClimateData)
            .WithMany(a => a.HourlyData)
            .HasForeignKey(x => x.AnnualClimateDataId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        // Индексы с фильтром
        builder.HasIndex(x => new { x.ClimateDataId, x.Hour })
            .IsUnique()
            .HasFilter("[ClimateDataId] IS NOT NULL AND [Hour] IS NOT NULL");

        builder.HasIndex(x => new { x.AnnualClimateDataId, x.HourOfYear })
            .IsUnique()
            .HasFilter("[AnnualClimateDataId] IS NOT NULL AND [HourOfYear] IS NOT NULL");
    }
}
