using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

public sealed class Iso52016MultiZoneEnergySimulationService : IIso52016MultiZoneEnergySimulationService
{
    private readonly IIso52016MultiZoneInputValidator _validator;
    private readonly IIso52016MultiZoneGraphBuilder _graphBuilder;
    private readonly IIso52016MultiZoneHourlySolver _hourlySolver;

    public Iso52016MultiZoneEnergySimulationService(
        IIso52016MultiZoneInputValidator validator,
        IIso52016MultiZoneGraphBuilder graphBuilder,
        IIso52016MultiZoneHourlySolver hourlySolver)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _graphBuilder = graphBuilder ?? throw new ArgumentNullException(nameof(graphBuilder));
        _hourlySolver = hourlySolver ?? throw new ArgumentNullException(nameof(hourlySolver));
    }

    public MultiZoneCalculationResult Simulate(MultiZoneCalculationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var validation = _validator.Validate(input);
        var graph = _graphBuilder.BuildGraph(input);

        if (!validation.IsValid || !graph.IsValid)
            return graph;

        return _hourlySolver.Solve(input, graph);
    }
}
