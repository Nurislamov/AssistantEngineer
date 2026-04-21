using AssistantEngineer.Modules.Buildings.Domain.Construction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Data.Configurations;

public class ConstructionLayerConfiguration : IEntityTypeConfiguration<ConstructionLayer>
{
    public void Configure(EntityTypeBuilder<ConstructionLayer> builder)
    {
        builder.ToTable("ConstructionLayers");
        builder.HasKey(layer => layer.Id);
        builder.Property(layer => layer.ThicknessM).IsRequired();

        builder.HasOne(layer => layer.Material)
            .WithMany()
            .HasForeignKey(layer => layer.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
