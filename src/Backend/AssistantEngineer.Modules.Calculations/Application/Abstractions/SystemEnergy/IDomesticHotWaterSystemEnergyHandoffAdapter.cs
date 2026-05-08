using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;

public interface IDomesticHotWaterSystemEnergyHandoffAdapter
{
    SystemEnergyUsefulLoadSet BuildUsefulLoadSet(DomesticHotWaterEn15316Handoff handoff);
}
