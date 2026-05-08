using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class SystemEnergyFinalEnergyCalculator : ISystemEnergyFinalEnergyCalculator
{
    private const string Source = "SystemEnergyFinalEnergyCalculator";
    private readonly ISystemEnergyGeneratorInputValidator _inputValidator;
    private readonly ISystemEnergyGeneratorLoadSplitter _loadSplitter;
    private readonly ISystemEnergyGeneratorFinalEnergyCalculator _generatorFinalEnergyCalculator;
    private readonly ISystemEnergyFinalEnergyAggregator _finalEnergyAggregator;

    public SystemEnergyFinalEnergyCalculator(
        ISystemEnergyGeneratorInputValidator inputValidator,
        ISystemEnergyGeneratorLoadSplitter loadSplitter,
        ISystemEnergyGeneratorFinalEnergyCalculator generatorFinalEnergyCalculator,
        ISystemEnergyFinalEnergyAggregator finalEnergyAggregator)
    {
        _inputValidator = inputValidator ?? throw new ArgumentNullException(nameof(inputValidator));
        _loadSplitter = loadSplitter ?? throw new ArgumentNullException(nameof(loadSplitter));
        _generatorFinalEnergyCalculator = generatorFinalEnergyCalculator ?? throw new ArgumentNullException(nameof(generatorFinalEnergyCalculator));
        _finalEnergyAggregator = finalEnergyAggregator ?? throw new ArgumentNullException(nameof(finalEnergyAggregator));
    }

    public SystemEnergyFinalEnergyResult Calculate(SystemEnergyGeneratorCalculationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var diagnostics = new List<StandardCalculationDiagnostic>
        {
            CreateInfo("AE-SYS-FINAL-CALCULATION-STARTED", "System-energy final-energy calculation started.")
        };

        var validation = _inputValidator.Validate(input);
        diagnostics.AddRange(validation.Diagnostics);

        var split = _loadSplitter.SplitLoads(input.GenerationHandoff, input.GeneratorSet);
        diagnostics.AddRange(split.Diagnostics);

        var assignedByGenerator = split.AssignedLoads.ToDictionary(
            assigned => assigned.GeneratorId,
            assigned => assigned,
            StringComparer.Ordinal);

        var generatorResults = new List<SystemEnergyGeneratorResult>();
        foreach (var generator in input.GeneratorSet.Generators)
        {
            if (!assignedByGenerator.TryGetValue(generator.GeneratorId, out var assignedLoad))
            {
                assignedLoad = new SystemEnergyGeneratorAssignedLoad(
                    GeneratorId: generator.GeneratorId,
                    HourlyAssignedLoadByEndUseKWh8760: new Dictionary<SystemEnergyEndUse, IReadOnlyList<double>>(),
                    Diagnostics: []);
            }

            var result = _generatorFinalEnergyCalculator.Calculate(generator, assignedLoad);
            generatorResults.Add(result);
            diagnostics.AddRange(result.Diagnostics);
        }

        var aggregated = _finalEnergyAggregator.Aggregate(
            input.CalculationId,
            input.GenerationHandoff,
            generatorResults,
            input.DisclosureOverride ?? input.GeneratorSet.DisclosureOverride);

        diagnostics.AddRange(aggregated.Diagnostics);
        diagnostics.Add(CreateInfo(
            "AE-SYS-FINAL-PRIMARY-ENERGY-DEFERRED",
            "Primary energy calculation is deferred to a later stage."));
        diagnostics.Add(CreateInfo(
            "AE-SYS-FINAL-CALCULATION-COMPLETED",
            "System-energy final-energy calculation completed."));

        return aggregated with
        {
            Diagnostics = diagnostics
        };
    }

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            Source);
}
