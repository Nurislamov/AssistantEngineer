using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;

public interface ISo52016MultiZoneBuildingSimulationFacade
{
    MultiZoneCalculationResult Simulate(MultiZoneCalculationInput input);
}
