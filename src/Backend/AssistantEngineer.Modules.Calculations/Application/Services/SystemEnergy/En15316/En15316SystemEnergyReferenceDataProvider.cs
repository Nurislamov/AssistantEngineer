using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy.En15316;

public sealed class En15316SystemEnergyReferenceDataProvider
{
    public En15316GenerationDefaults ResolveGenerationDefaults(
        En15316GenerationTechnology technology)
    {
        return technology switch
        {
            En15316GenerationTechnology.Boiler => new En15316GenerationDefaults(
                GenerationEfficiency: 0.88,
                GenerationCop: null,
                TypicalAuxiliaryFraction: 0.03),
            En15316GenerationTechnology.CondensingBoiler => new En15316GenerationDefaults(
                GenerationEfficiency: 0.94,
                GenerationCop: null,
                TypicalAuxiliaryFraction: 0.03),
            En15316GenerationTechnology.HeatPump => new En15316GenerationDefaults(
                GenerationEfficiency: null,
                GenerationCop: 3.2,
                TypicalAuxiliaryFraction: 0.04),
            En15316GenerationTechnology.Chiller => new En15316GenerationDefaults(
                GenerationEfficiency: null,
                GenerationCop: 2.8,
                TypicalAuxiliaryFraction: 0.05),
            En15316GenerationTechnology.DistrictHeatingSubstation => new En15316GenerationDefaults(
                GenerationEfficiency: 0.98,
                GenerationCop: null,
                TypicalAuxiliaryFraction: 0.01),
            En15316GenerationTechnology.DistrictCoolingSubstation => new En15316GenerationDefaults(
                GenerationEfficiency: 0.97,
                GenerationCop: null,
                TypicalAuxiliaryFraction: 0.01),
            En15316GenerationTechnology.ElectricResistance => new En15316GenerationDefaults(
                GenerationEfficiency: 0.99,
                GenerationCop: null,
                TypicalAuxiliaryFraction: 0.0),
            En15316GenerationTechnology.DirectElectric => new En15316GenerationDefaults(
                GenerationEfficiency: 1.0,
                GenerationCop: null,
                TypicalAuxiliaryFraction: 0.0),
            _ => new En15316GenerationDefaults(
                GenerationEfficiency: null,
                GenerationCop: null,
                TypicalAuxiliaryFraction: 0.0)
        };
    }

    public En15316HeatingCircuitDefaults ResolveHeatingCircuitDefaults(
        HeatingCircuitType circuitType,
        En15316EmissionModelKind emissionModelKind)
    {
        var emissionEfficiency = emissionModelKind switch
        {
            En15316EmissionModelKind.C3 => 0.95,
            En15316EmissionModelKind.Simplified => 0.97,
            _ => 0.96
        };

        var distributionEfficiency = circuitType switch
        {
            HeatingCircuitType.Underfloor => 0.94,
            HeatingCircuitType.AirSystem => 0.92,
            _ => 0.93
        };

        var storageEfficiency = 0.98;
        var distributionAuxiliaryFraction = circuitType == HeatingCircuitType.AirSystem ? 0.03 : 0.02;

        return new En15316HeatingCircuitDefaults(
            Emission: new En15316EmissionDefaults(
                ModelKind: emissionModelKind,
                Efficiency: emissionEfficiency,
                LossFactor: null),
            Distribution: new En15316DistributionDefaults(
                Efficiency: distributionEfficiency,
                LossFactor: null,
                AuxiliaryEnergyFraction: distributionAuxiliaryFraction),
            Storage: new En15316StorageDefaults(
                Efficiency: storageEfficiency,
                LossFactor: null));
    }
}

public sealed record En15316GenerationDefaults(
    double? GenerationEfficiency,
    double? GenerationCop,
    double TypicalAuxiliaryFraction);

public sealed record En15316EmissionDefaults(
    En15316EmissionModelKind ModelKind,
    double Efficiency,
    double? LossFactor);

public sealed record En15316DistributionDefaults(
    double Efficiency,
    double? LossFactor,
    double AuxiliaryEnergyFraction);

public sealed record En15316StorageDefaults(
    double Efficiency,
    double? LossFactor);

public sealed record En15316HeatingCircuitDefaults(
    En15316EmissionDefaults Emission,
    En15316DistributionDefaults Distribution,
    En15316StorageDefaults Storage);
