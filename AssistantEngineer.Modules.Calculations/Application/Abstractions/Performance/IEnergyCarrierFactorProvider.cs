using AssistantEngineer.Modules.Calculations.Application.Contracts.Performance;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Performance;

public interface IEnergyCarrierFactorProvider
{
    Result<EnergyCarrierFactors> Get(EnergyCarrierType carrierType);
}