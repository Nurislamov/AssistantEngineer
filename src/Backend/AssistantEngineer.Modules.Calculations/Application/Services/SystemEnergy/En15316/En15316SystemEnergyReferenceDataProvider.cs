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
}

public sealed record En15316GenerationDefaults(
    double? GenerationEfficiency,
    double? GenerationCop,
    double TypicalAuxiliaryFraction);
