using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016MultiZoneBuildingSimulationFacade : IIso52016MultiZoneBuildingSimulationFacade
{
    private readonly IIso52016MultiZoneEnergySimulationService _service;

    public Iso52016MultiZoneBuildingSimulationFacade(
        IIso52016MultiZoneEnergySimulationService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public MultiZoneCalculationResult Simulate(MultiZoneCalculationInput input) =>
        _service.Simulate(input);
}
