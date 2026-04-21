using System.Text.Json;
using AssistantEngineer.Modules.Buildings.Domain.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Persistence.Data.Configurations;

public class HourlyScheduleConfiguration : IEntityTypeConfiguration<HourlySchedule>
{
    public void Configure(EntityTypeBuilder<HourlySchedule> builder)
    {
        builder.ToTable("HourlySchedules");
        builder.HasKey(schedule => schedule.Id);
        builder.Property(schedule => schedule.Name).IsRequired().HasMaxLength(200);
        builder.Property(schedule => schedule.Factors)
            .HasConversion(
                factors => JsonSerializer.Serialize(factors, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<double[]>(json, (JsonSerializerOptions?)null) ?? Array.Empty<double>())
            .HasColumnType("jsonb")
            .IsRequired();
    }
}
