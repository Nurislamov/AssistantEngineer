using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class SystemEnergyGeneratorLoadSplitter : ISystemEnergyGeneratorLoadSplitter
{
    private const string Source = "SystemEnergyGeneratorLoadSplitter";

    public SystemEnergyGeneratorLoadSplitResult SplitLoads(
        SystemEnergyGenerationHandoff handoff,
        SystemEnergyGeneratorSet generatorSet)
    {
        ArgumentNullException.ThrowIfNull(handoff);
        ArgumentNullException.ThrowIfNull(generatorSet);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        var byGenerator = generatorSet.Generators.ToDictionary(
            generator => generator.GeneratorId,
            _ => new Dictionary<SystemEnergyEndUse, double[]>(),
            StringComparer.Ordinal);

        var unassignedByEndUse = new Dictionary<SystemEnergyEndUse, IReadOnlyList<double>>();

        foreach (var endUseEntry in handoff.HourlySystemLoadBeforeGenerationByEndUseKWh8760)
        {
            var endUse = endUseEntry.Key;
            var requested = SystemEnergyProfileHelper.Ensure8760(endUseEntry.Value);
            var servingGenerators = generatorSet.Generators
                .Where(generator => generator.ServedEndUses.Contains(endUse))
                .OrderBy(generator => generator.Priority)
                .ThenBy(generator => generator.GeneratorId, StringComparer.Ordinal)
                .ToArray();

            if (servingGenerators.Length == 0)
            {
                diagnostics.Add(CreateWarning(
                    "AE-SYS-GEN-ENDUSE-NO-GENERATOR",
                    $"No generator serves end use '{endUse}'."));
                unassignedByEndUse[endUse] = requested;
                continue;
            }

            var unassigned = requested.ToArray();
            switch (generatorSet.LoadSplitMode)
            {
                case SystemEnergyLoadSplitMode.SingleGenerator:
                    ApplySingleGenerator(endUse, requested, unassigned, servingGenerators, byGenerator);
                    break;

                case SystemEnergyLoadSplitMode.FixedFraction:
                    ApplyFixedFraction(endUse, requested, unassigned, servingGenerators, byGenerator, diagnostics);
                    break;

                case SystemEnergyLoadSplitMode.PriorityOrder:
                    ApplyPriority(endUse, requested, unassigned, servingGenerators, byGenerator, diagnostics, capacityLimited: false);
                    diagnostics.Add(CreateInfo("AE-SYS-GEN-PRIORITY-SPLIT-USED", $"Priority-order split applied for end use '{endUse}'."));
                    break;

                case SystemEnergyLoadSplitMode.CapacityLimitedPriority:
                    ApplyPriority(endUse, requested, unassigned, servingGenerators, byGenerator, diagnostics, capacityLimited: true);
                    diagnostics.Add(CreateInfo("AE-SYS-GEN-PRIORITY-SPLIT-USED", $"Capacity-limited priority split applied for end use '{endUse}'."));
                    break;

                case SystemEnergyLoadSplitMode.CustomHourlyFraction:
                    ApplyCustomHourlyFraction(endUse, requested, unassigned, servingGenerators, byGenerator, diagnostics);
                    diagnostics.Add(CreateInfo("AE-SYS-GEN-CUSTOM-HOURLY-FRACTIONS-USED", $"Custom hourly fractions were used for end use '{endUse}'."));
                    break;

                case SystemEnergyLoadSplitMode.Unknown:
                case SystemEnergyLoadSplitMode.Other:
                default:
                    diagnostics.Add(CreateWarning(
                        "AE-SYS-GEN-SPLIT-MODE-UNKNOWN-NO-FALLBACK",
                        $"Load split mode '{generatorSet.LoadSplitMode}' is unsupported. End use '{endUse}' remained unmet."));
                    break;
            }

            unassignedByEndUse[endUse] = unassigned;
        }

        var assignedLoads = generatorSet.Generators
            .Select(generator =>
            {
                var endUseProfiles = byGenerator[generator.GeneratorId]
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => (IReadOnlyList<double>)kvp.Value,
                        EqualityComparer<SystemEnergyEndUse>.Default);

                return new SystemEnergyGeneratorAssignedLoad(
                    GeneratorId: generator.GeneratorId,
                    HourlyAssignedLoadByEndUseKWh8760: endUseProfiles,
                    Diagnostics: []);
            })
            .ToArray();

        diagnostics.Add(CreateInfo(
            "AE-SYS-GEN-LOAD-SPLIT-CALCULATED",
            "Generator load split was calculated."));

        return new SystemEnergyGeneratorLoadSplitResult(
            AssignedLoads: assignedLoads,
            HourlyUnassignedLoadByEndUseKWh8760: unassignedByEndUse,
            Diagnostics: diagnostics);
    }

    private static void ApplySingleGenerator(
        SystemEnergyEndUse endUse,
        IReadOnlyList<double> requested,
        double[] unassigned,
        IReadOnlyList<SystemEnergyGeneratorInput> servingGenerators,
        IDictionary<string, Dictionary<SystemEnergyEndUse, double[]>> byGenerator)
    {
        var generator = servingGenerators[0];
        AssignProfile(byGenerator, generator.GeneratorId, endUse, requested);

        for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
        {
            unassigned[hour] = 0.0;
        }
    }

    private static void ApplyFixedFraction(
        SystemEnergyEndUse endUse,
        IReadOnlyList<double> requested,
        double[] unassigned,
        IReadOnlyList<SystemEnergyGeneratorInput> servingGenerators,
        IDictionary<string, Dictionary<SystemEnergyEndUse, double[]>> byGenerator,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        var fractions = servingGenerators.Select(generator => Math.Max(0.0, generator.LoadFraction ?? 0.0)).ToArray();
        var fractionSum = fractions.Sum();

        if (fractionSum > 1.0)
        {
            for (var index = 0; index < fractions.Length; index++)
            {
                fractions[index] /= fractionSum;
            }

            diagnostics.Add(CreateWarning(
                "AE-SYS-GEN-FIXED-FRACTIONS-NORMALIZED",
                $"Fixed load fractions for end use '{endUse}' exceeded 1.0 and were normalized."));
        }
        else if (fractionSum < 1.0)
        {
            diagnostics.Add(CreateWarning(
                "AE-SYS-GEN-FIXED-FRACTIONS-INCOMPLETE",
                $"Fixed load fractions for end use '{endUse}' are incomplete; remaining load is unmet."));
        }

        for (var generatorIndex = 0; generatorIndex < servingGenerators.Count; generatorIndex++)
        {
            var assigned = EnsureAssignedProfile(byGenerator, servingGenerators[generatorIndex].GeneratorId, endUse);
            var fraction = fractions[generatorIndex];
            for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
            {
                var assignedValue = requested[hour] * fraction;
                assigned[hour] += assignedValue;
                unassigned[hour] -= assignedValue;
            }
        }

        ClampToZero(unassigned);
    }

    private static void ApplyPriority(
        SystemEnergyEndUse endUse,
        IReadOnlyList<double> requested,
        double[] unassigned,
        IReadOnlyList<SystemEnergyGeneratorInput> servingGenerators,
        IDictionary<string, Dictionary<SystemEnergyEndUse, double[]>> byGenerator,
        ICollection<StandardCalculationDiagnostic> diagnostics,
        bool capacityLimited)
    {
        for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
        {
            var remaining = requested[hour];
            foreach (var generator in servingGenerators)
            {
                if (remaining <= 0.0)
                    break;

                var cap = capacityLimited
                    ? generator.NominalCapacityKWhPerHour ?? double.PositiveInfinity
                    : double.PositiveInfinity;

                var assignedValue = Math.Min(remaining, cap);
                if (assignedValue > 0.0)
                {
                    EnsureAssignedProfile(byGenerator, generator.GeneratorId, endUse)[hour] += assignedValue;
                    remaining -= assignedValue;
                    if (capacityLimited && double.IsFinite(cap) && cap >= 0.0 && assignedValue >= cap)
                    {
                        diagnostics.Add(CreateInfo(
                            "AE-SYS-GEN-CAPACITY-LIMIT-APPLIED",
                            $"Capacity limit applied for generator '{generator.GeneratorId}' on end use '{endUse}' at hour {hour}."));
                    }
                }
            }

            unassigned[hour] = remaining;
        }
    }

    private static void ApplyCustomHourlyFraction(
        SystemEnergyEndUse endUse,
        IReadOnlyList<double> requested,
        double[] unassigned,
        IReadOnlyList<SystemEnergyGeneratorInput> servingGenerators,
        IDictionary<string, Dictionary<SystemEnergyEndUse, double[]>> byGenerator,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        var fractions = servingGenerators
            .Select(generator => SystemEnergyProfileHelper.Ensure8760(generator.HourlyLoadFraction8760))
            .ToArray();

        for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
        {
            var sumFractions = fractions.Sum(profile => Math.Max(0.0, profile[hour]));
            if (sumFractions <= 0.0)
            {
                unassigned[hour] = requested[hour];
                continue;
            }

            var normalization = sumFractions > 1.0 ? 1.0 / sumFractions : 1.0;
            if (sumFractions > 1.0)
            {
                diagnostics.Add(CreateWarning(
                    "AE-SYS-GEN-FIXED-FRACTIONS-NORMALIZED",
                    $"Custom hourly fractions for end use '{endUse}' exceeded 1.0 at hour {hour} and were normalized."));
            }
            else if (sumFractions < 1.0)
            {
                diagnostics.Add(CreateWarning(
                    "AE-SYS-GEN-FIXED-FRACTIONS-INCOMPLETE",
                    $"Custom hourly fractions for end use '{endUse}' were incomplete at hour {hour}; remaining load is unmet."));
            }

            var assignedTotal = 0.0;
            for (var generatorIndex = 0; generatorIndex < servingGenerators.Count; generatorIndex++)
            {
                var fraction = Math.Max(0.0, fractions[generatorIndex][hour]) * normalization;
                var assignedValue = requested[hour] * fraction;
                EnsureAssignedProfile(byGenerator, servingGenerators[generatorIndex].GeneratorId, endUse)[hour] += assignedValue;
                assignedTotal += assignedValue;
            }

            unassigned[hour] = Math.Max(0.0, requested[hour] - assignedTotal);
        }
    }

    private static void AssignProfile(
        IDictionary<string, Dictionary<SystemEnergyEndUse, double[]>> byGenerator,
        string generatorId,
        SystemEnergyEndUse endUse,
        IReadOnlyList<double> values)
    {
        var assigned = EnsureAssignedProfile(byGenerator, generatorId, endUse);
        for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
        {
            assigned[hour] = values[hour];
        }
    }

    private static double[] EnsureAssignedProfile(
        IDictionary<string, Dictionary<SystemEnergyEndUse, double[]>> byGenerator,
        string generatorId,
        SystemEnergyEndUse endUse)
    {
        var map = byGenerator[generatorId];
        if (!map.TryGetValue(endUse, out var profile))
        {
            profile = SystemEnergyProfileHelper.ZeroProfile();
            map[endUse] = profile;
        }

        return profile;
    }

    private static void ClampToZero(double[] source)
    {
        for (var hour = 0; hour < source.Length; hour++)
        {
            source[hour] = Math.Max(0.0, source[hour]);
        }
    }

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            Source);

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            Source);
}
