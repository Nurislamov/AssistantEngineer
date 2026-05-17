using AssistantEngineer.Modules.Buildings.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.OrganizationId).IsRequired(false);
        builder.Property(p => p.OwnerUserId).IsRequired(false);
        builder.HasIndex(p => p.Name).IsUnique();
        builder.HasIndex(p => p.OrganizationId);
        builder.HasIndex(p => p.OwnerUserId);
        builder.HasIndex(p => new { p.OrganizationId, p.Id });

        // Project-to-preferences mapping is configured in CalculationPreferencesConfiguration.
    }
}
