using AssistantEngineer.Modules.Buildings.Domain.Construction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Persistence.Data.Configurations;

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("Materials");
        builder.HasKey(material => material.Id);
        builder.Property(material => material.Name).IsRequired().HasMaxLength(200);
        builder.Property(material => material.ThermalConductivityWPerMK).IsRequired();
        builder.Property(material => material.DensityKgPerM3).IsRequired();
        builder.Property(material => material.SpecificHeatJPerKgK).IsRequired();
    }
}
