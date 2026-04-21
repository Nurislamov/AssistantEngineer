using AssistantEngineer.Modules.Buildings.Domain.Construction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Data.Configurations;

public class ConstructionAssemblyConfiguration : IEntityTypeConfiguration<ConstructionAssembly>
{
    public void Configure(EntityTypeBuilder<ConstructionAssembly> builder)
    {
        builder.ToTable("ConstructionAssemblies");
        builder.HasKey(assembly => assembly.Id);
        builder.Property(assembly => assembly.Name).IsRequired().HasMaxLength(200);

        builder.HasMany(assembly => assembly.Layers)
            .WithOne(layer => layer.ConstructionAssembly)
            .HasForeignKey(layer => layer.ConstructionAssemblyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
