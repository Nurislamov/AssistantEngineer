using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Performance;

public interface IEnergyCarrierFactorProvider
{
    Result<EnergyCarrierFactors> Get(EnergyCarrierType carrierType);
}

public sealed class EnergyCarrierFactorProvider : IEnergyCarrierFactorProvider
{
    private static readonly IReadOnlyDictionary<EnergyCarrierType, EnergyCarrierFactors> Defaults =
        new Dictionary<EnergyCarrierType, EnergyCarrierFactors>
        {
            [EnergyCarrierType.Electricity] = new(2.1, 0.386),
            [EnergyCarrierType.NaturalGas] = new(1.1, 0.202),
            [EnergyCarrierType.DistrictHeat] = new(1.3, 0.18),
            [EnergyCarrierType.Biomass] = new(0.2, 0.025),
            [EnergyCarrierType.FuelOil] = new(1.1, 0.267),
            [EnergyCarrierType.Propane] = new(1.1, 0.214)
        };

    public Result<EnergyCarrierFactors> Get(EnergyCarrierType carrierType) =>
        Defaults.TryGetValue(carrierType, out var factors)
            ? Result<EnergyCarrierFactors>.Success(factors)
            : Result<EnergyCarrierFactors>.Validation($"Energy carrier '{carrierType}' is not supported.");
}

public enum EnergyCarrierType
{
    Electricity,
    NaturalGas,
    DistrictHeat,
    Biomass,
    FuelOil,
    Propane
}

public sealed record EnergyCarrierFactors(
    double PrimaryEnergyFactor,
    double Co2KgPerKWh);
