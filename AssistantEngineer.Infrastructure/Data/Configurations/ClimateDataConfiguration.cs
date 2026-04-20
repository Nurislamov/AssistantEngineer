using AssistantEngineer.Domain.Models.Climate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Data.Configurations;

public class ClimateDataConfiguration : IEntityTypeConfiguration<ClimateData>
{
    public void Configure(EntityTypeBuilder<ClimateData> builder)
    {
        builder.ToTable("ClimateData");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Month).IsRequired();
        builder.Property(x => x.DayOfMonth).IsRequired();
        builder.Property(x => x.DailyTemperatureRange).IsRequired();

        builder.HasOne(x => x.ClimateZone)
            .WithMany()
            .HasForeignKey(x => x.ClimateZoneId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.HourlyData)
            .WithOne(h => h.ClimateData)
            .HasForeignKey(h => h.ClimateDataId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ClimateZoneId, x.Month }).IsUnique();
    }
}