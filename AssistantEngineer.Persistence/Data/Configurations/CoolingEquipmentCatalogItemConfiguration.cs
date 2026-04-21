using AssistantEngineer.Modules.Equipment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Persistence.Data.Configurations;

public class CoolingEquipmentCatalogItemConfiguration : IEntityTypeConfiguration<CoolingEquipmentCatalogItem>
{
    public void Configure(EntityTypeBuilder<CoolingEquipmentCatalogItem> builder)
    {
        builder.ToTable("EquipmentCatalogItems");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Manufacturer).IsRequired().HasMaxLength(100);
        builder.Property(e => e.SystemType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.UnitType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ModelName).IsRequired().HasMaxLength(150);
        builder.Property(e => e.IsActive).IsRequired();
        builder.HasIndex(e => new { e.Manufacturer, e.SystemType, e.UnitType, e.ModelName })
            .HasDatabaseName("IX_EquipmentCatalogItems_CatalogIdentity")
            .IsUnique();

        builder.OwnsOne(e => e.NominalCoolingCapacity, p =>
        {
            p.Property(pw => pw.Watts).HasColumnName("NominalCoolingCapacityW").IsRequired();
        });
    }
}
