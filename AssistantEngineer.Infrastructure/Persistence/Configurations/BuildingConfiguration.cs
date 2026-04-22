using AssistantEngineer.Modules.Buildings.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public class BuildingConfiguration : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> builder)
    {
        builder.ToTable("Buildings");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(b => new { b.ProjectId, b.Name }).IsUnique();

        // ������� �������� ��� �������� ����� ClimateZoneId
        builder.Property<int?>("ClimateZoneId");

        builder.HasOne(b => b.Project)
            .WithMany(p => p.Buildings)
            .HasForeignKey(b => b.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.ClimateZone)
            .WithMany()
            .HasForeignKey("ClimateZoneId")
            .OnDelete(DeleteBehavior.SetNull);
    }
}
