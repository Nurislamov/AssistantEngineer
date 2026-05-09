using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class SystemEnergyGenerationCalculator : ISystemEnergyGenerationCalculator
{
    private const string Source = "SystemEnergyGenerationCalculator";

    public SystemEnergyGenerationStageResult Calculate(SystemEnergyGenerationStageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var assumptions = new List<string>();
        var warnings = new List<string>();
        var diagnostics = new List<StandardCalculationDiagnostic>();

        var requested = request.LoadToGenerationProfileKWh
            .Select(value => double.IsFinite(value) && value > 0.0 ? value : 0.0)
            .ToArray();
        var delivered = new double[requested.Length];
        var generationLosses = new double[requested.Length];
        var auxiliary = new double[requested.Length];
        var finalByCarrier = new Dictionary<SystemEnergyCarrierKind, double[]>(EqualityComparer<SystemEnergyCarrierKind>.Default);

        var generators = request.Generators
            .Where(generator => generator.UseKind == request.UseKind)
            .OrderBy(generator => generator.Priority)
            .ThenBy(generator => generator.GeneratorId, StringComparer.Ordinal)
            .ToArray();

        if (generators.Length == 0)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-GEN-NO-GENERATOR-FOR-USE",
                $"No generators were defined for use '{request.UseKind}'."));
        }

        for (var hour = 0; hour < requested.Length; hour++)
        {
            var remaining = requested[hour];
            var deliveredHour = 0.0;
            var auxiliaryHour = 0.0;

            foreach (var generator in generators)
            {
                if (remaining <= 0.0)
                    break;

                if (generator.GeneratorKind is SystemEnergyGeneratorKind.SolarThermal or SystemEnergyGeneratorKind.SolarThermalContribution)
                {
                    var renewableFraction = Math.Clamp(generator.RenewableContributionFraction ?? 0.0, 0.0, 1.0);
                    var covered = remaining * renewableFraction;
                    deliveredHour += covered;
                    remaining -= covered;
                    if (covered > 0.0)
                    {
                        assumptions.Add("Solar thermal contribution reduces purchased final energy before carrier aggregation.");
                    }

                    continue;
                }

                var stageEfficiency = ResolvePerformanceFactor(generator, diagnostics);
                if (!(stageEfficiency > 0.0))
                    continue;

                var served = remaining;
                var finalPurchased = served / stageEfficiency.Value;

                var carrier = generator.CarrierKind;
                if (!finalByCarrier.TryGetValue(carrier, out var profile))
                {
                    profile = new double[requested.Length];
                    finalByCarrier[carrier] = profile;
                }

                profile[hour] += finalPurchased;
                deliveredHour += served;
                generationLosses[hour] += Math.Max(0.0, finalPurchased - served);

                var auxProfile = SystemEnergyStageCalculatorCore.BuildStageProfile(
                    generator.AuxiliaryEnergyProfile,
                    requested.Length,
                    Source,
                    "AE-SYS-GEN-AUXILIARY-PROFILE-LENGTH-MISMATCH",
                    diagnostics);
                auxiliaryHour += auxProfile[hour];

                remaining -= served;
            }

            if (remaining > 1e-9)
            {
                warnings.Add($"Unmet generation load remained at hour {hour} for use '{request.UseKind}'.");
                diagnostics.Add(CreateWarning(
                    "AE-SYS-GEN-UNMET-LOAD",
                    $"Unmet generation load remained at hour {hour} for use '{request.UseKind}'."));
            }

            delivered[hour] = deliveredHour;
            auxiliary[hour] = auxiliaryHour;
        }

        assumptions.Add("Generation convention: final purchased energy equals delivered load divided by efficiency/COP/SPF.");
        assumptions.Add("Auxiliary generation energy remains separate from thermal generation output.");

        return new SystemEnergyGenerationStageResult(
            UseKind: request.UseKind,
            RequestedLoadProfileKWh: requested,
            DeliveredLoadProfileKWh: delivered,
            FinalEnergyByCarrierKWh: finalByCarrier.ToDictionary(
                item => item.Key,
                item => (IReadOnlyList<double>)item.Value,
                EqualityComparer<SystemEnergyCarrierKind>.Default),
            GenerationLossesProfileKWh: generationLosses,
            AuxiliaryEnergyProfileKWh: auxiliary,
            Diagnostics: SortDiagnostics(diagnostics),
            Assumptions: assumptions.Distinct(StringComparer.Ordinal).ToArray(),
            Warnings: warnings.Distinct(StringComparer.Ordinal).ToArray());
    }

    private static double? ResolvePerformanceFactor(
        SystemEnergyGeneratorDefinition generator,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (generator.SeasonalPerformanceFactor is > 0.0)
            return generator.SeasonalPerformanceFactor.Value;

        if (generator.Cop is > 0.0)
            return generator.Cop.Value;

        if (generator.Efficiency is > 0.0)
            return generator.Efficiency.Value;

        if (generator.GeneratorKind == SystemEnergyGeneratorKind.ElectricResistance)
            return 1.0;

        diagnostics.Add(CreateError(
            "AE-SYS-GEN-FACTOR-MISSING",
            $"Generator '{generator.GeneratorId}' does not provide valid efficiency/COP/SPF."));
        return null;
    }

    private static IReadOnlyList<StandardCalculationDiagnostic> SortDiagnostics(
        IEnumerable<StandardCalculationDiagnostic> diagnostics) =>
        diagnostics
            .OrderByDescending(item => item.Severity)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.Message, StringComparer.Ordinal)
            .ToArray();

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            Source);

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Error,
            code,
            message,
            Source);
}
