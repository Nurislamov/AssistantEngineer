using AssistantEngineer.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Data.Configurations;

public class CalculationPreferencesConfiguration : IEntityTypeConfiguration<CalculationPreferences>
{
    public void Configure(EntityTypeBuilder<CalculationPreferences> builder)
    {
        builder.ToTable("CalculationPreferences");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.CoolingSafetyFactor).IsRequired();
        builder.Property(p => p.HeatingSafetyFactor).IsRequired();
        builder.Property(p => p.Iso52016InternalHeatCapacityJPerM2K).IsRequired().HasDefaultValue(10_000);
        builder.Property(p => p.Iso52016SolarUtilizationFactor).IsRequired().HasDefaultValue(0.75);
        builder.Property(p => p.Iso52016WindowFrameAreaFraction).IsRequired().HasDefaultValue(0.25);
        builder.Property(p => p.Iso52016DirectSolarShadingReductionFactor).IsRequired().HasDefaultValue(1.0);
        builder.Property(p => p.Iso52016DiffuseSolarShareUnaffectedByShading).IsRequired().HasDefaultValue(0.3);
        builder.Property(p => p.Iso52016DefaultAirChangesPerHour).IsRequired().HasDefaultValue(0.5);

        builder.HasOne(p => p.Project)
            .WithOne(p => p.Preferences)
            .HasForeignKey<CalculationPreferences>(p => p.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
