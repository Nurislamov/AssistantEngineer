using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.MultiZone;

public interface ISo52016MultiZoneEnergySimulationService
{
    MultiZoneCalculationResult Simulate(MultiZoneCalculationInput input);
}
