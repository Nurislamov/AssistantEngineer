using AssistantEngineer.Modules.Buildings.Domain.Ventilation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssistantEngineer.Infrastructure.Persistence.Configurations;

public class VentilationParametersConfiguration : IEntityTypeConfiguration<VentilationParameters>
{
    public void Configure(EntityTypeBuilder<VentilationParameters> builder)
    {
        builder.ToTable("VentilationParameters");
        builder.HasKey(parameters => parameters.Id);
        builder.Property(parameters => parameters.AirChangesPerHour).IsRequired();
        builder.Property(parameters => parameters.HeatRecoveryEfficiency).IsRequired();
        builder.Property(parameters => parameters.InfiltrationAirChangesPerHour).IsRequired();
        builder.Property(parameters => parameters.WindExposureFactor).IsRequired();
        builder.Property(parameters => parameters.StackCoefficient).IsRequired();
        builder.Property(parameters => parameters.WindCoefficient).IsRequired();
    }
}
