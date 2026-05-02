using AssistantEngineer.Modules.Buildings.Domain.Climate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public class AnnualClimateDataConfiguration : IEntityTypeConfiguration<AnnualClimateData>
{
    public void Configure(EntityTypeBuilder<AnnualClimateData> builder)
    {
        builder.ToTable("AnnualClimateData");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Year).IsRequired();

        builder.HasOne(x => x.ClimateZone)
            .WithMany()
            .HasForeignKey(x => x.ClimateZoneId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.HourlyData)
            .WithOne(x => x.AnnualClimateData)
            .HasForeignKey(x => x.AnnualClimateDataId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ClimateZoneId, x.Year }).IsUnique();

        builder.Navigation(x => x.HourlyData)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}