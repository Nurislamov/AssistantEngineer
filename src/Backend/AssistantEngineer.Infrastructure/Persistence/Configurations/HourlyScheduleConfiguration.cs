using System.Text.Json;
using AssistantEngineer.Modules.Buildings.Domain.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public class HourlyScheduleConfiguration : IEntityTypeConfiguration<HourlySchedule>
{
    private static readonly ValueComparer<IReadOnlyList<double>> FactorsComparer = new(
        (left, right) => left == null ? right == null : right != null && left.SequenceEqual(right),
        factors => factors.Aggregate(0, (hash, factor) => HashCode.Combine(hash, factor)),
        factors => factors.ToArray());

    public void Configure(EntityTypeBuilder<HourlySchedule> builder)
    {
        builder.ToTable("HourlySchedules");
        builder.HasKey(schedule => schedule.Id);
        builder.Property(schedule => schedule.Name).IsRequired().HasMaxLength(200);
        var factorsProperty = builder.Property(schedule => schedule.Factors)
            .HasConversion(
                factors => JsonSerializer.Serialize(factors, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<double[]>(json, (JsonSerializerOptions?)null) ?? Array.Empty<double>())
            .HasColumnType("jsonb")
            .IsRequired();
        factorsProperty.Metadata.SetValueComparer(FactorsComparer);
    }
}
