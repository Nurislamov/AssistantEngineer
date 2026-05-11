using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

public sealed class Iso52016MultiZoneEnergySimulationService : ISo52016MultiZoneEnergySimulationService
{
    private readonly ISo52016MultiZoneInputValidator _validator;
    private readonly ISo52016MultiZoneGraphBuilder _graphBuilder;
    private readonly ISo52016MultiZoneHourlySolver _hourlySolver;

    public Iso52016MultiZoneEnergySimulationService(
        ISo52016MultiZoneInputValidator validator,
        ISo52016MultiZoneGraphBuilder graphBuilder,
        ISo52016MultiZoneHourlySolver hourlySolver)
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
