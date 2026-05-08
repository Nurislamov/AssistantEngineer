using AssistantEngineer.Modules.Calculations.Application.Abstractions.Standards;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class SystemEnergyFinalEnergyAggregator : ISystemEnergyFinalEnergyAggregator
{
    private static readonly IReadOnlyList<string> RequiredForbiddenClaims =
    [
        "Full ISO compliance",
        "Full EN compliance",
        "pyBuildingEnergy parity",
        "EnergyPlus parity",
        "ASHRAE 140 validation"
    ];

    private const string Source = "SystemEnergyFinalEnergyAggregator";
    private readonly IStandardCalculationDisclosureFactory _disclosureFactory;

    public SystemEnergyFinalEnergyAggregator(IStandardCalculationDisclosureFactory disclosureFactory)
    {
        _disclosureFactory = disclosureFactory ?? throw new ArgumentNullException(nameof(disclosureFactory));
    }

    public SystemEnergyFinalEnergyResult Aggregate(
        string calculationId,
        SystemEnergyGenerationHandoff handoff,
        IReadOnlyList<SystemEnergyGeneratorResult> generatorResults,
        StandardCalculationDisclosure? disclosureOverride)
    {
        ArgumentNullException.ThrowIfNull(handoff);
        ArgumentNullException.ThrowIfNull(generatorResults);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        var endUseResults = new List<SystemEnergyEndUseFinalEnergyResult>();
        var hourlyFinalByCarrier = new Dictionary<SystemEnergyCarrier, double[]>();
        var totalHourlyFinal = SystemEnergyProfileHelper.ZeroProfile();
        var totalHourlyAux = SystemEnergyProfileHelper.ZeroProfile();

        foreach (var generatorResult in generatorResults)
        {
            diagnostics.AddRange(generatorResult.Diagnostics);
        }

        foreach (var endUseEntry in handoff.HourlySystemLoadBeforeGenerationByEndUseKWh8760)
        {
            var endUse = endUseEntry.Key;
            var requested = SystemEnergyProfileHelper.Ensure8760(endUseEntry.Value);
            var supplied = SystemEnergyProfileHelper.ZeroProfile();
            var endUseAux = SystemEnergyProfileHelper.ZeroProfile();
            var endUseCarrierProfiles = new Dictionary<SystemEnergyCarrier, double[]>();

            var endUseGeneratorResults = generatorResults
                .Where(result => result.ServedEndUses.Contains(endUse))
                .ToArray();

            foreach (var generatorResult in endUseGeneratorResults)
            {
                if (!generatorResult.HourlySuppliedSystemLoadByEndUseKWh8760.TryGetValue(endUse, out var suppliedProfile))
                    continue;

                var suppliedValues = SystemEnergyProfileHelper.Ensure8760(suppliedProfile);
                var finalByEndUse = generatorResult.HourlyFinalEnergyByEndUseKWh8760.TryGetValue(endUse, out var finalProfile)
                    ? SystemEnergyProfileHelper.Ensure8760(finalProfile)
                    : SystemEnergyProfileHelper.ZeroProfile();

                if (!endUseCarrierProfiles.TryGetValue(generatorResult.FinalEnergyCarrier, out var endUseCarrier))
                {
                    endUseCarrier = SystemEnergyProfileHelper.ZeroProfile();
                    endUseCarrierProfiles[generatorResult.FinalEnergyCarrier] = endUseCarrier;
                }

                for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
                {
                    supplied[hour] += suppliedValues[hour];
                    endUseCarrier[hour] += finalByEndUse[hour];

                    var generatorHourSuppliedTotal = generatorResult.HourlySuppliedSystemLoadByEndUseKWh8760
                        .Values
                        .Where(profile => profile.Count == SystemEnergyProfileHelper.HoursPerYear)
                        .Sum(profile => profile[hour]);
                    if (generatorHourSuppliedTotal > 0.0)
                    {
                        endUseAux[hour] += generatorResult.HourlyTotalAuxiliaryElectricityKWh8760[hour] *
                                           (suppliedValues[hour] / generatorHourSuppliedTotal);
                    }
                }
            }

            foreach (var auxiliaryLoad in handoff.AuxiliaryLoads.Where(load => load.EndUse == endUse))
            {
                var profile = SystemEnergyProfileHelper.Ensure8760(auxiliaryLoad.HourlyAuxiliaryEnergyKWh8760);
                for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
                {
                    endUseAux[hour] += profile[hour];
                }
            }

            if (endUseAux.Any(value => value > 0.0))
            {
                if (!endUseCarrierProfiles.TryGetValue(SystemEnergyCarrier.Electricity, out var electricityCarrier))
                {
                    electricityCarrier = SystemEnergyProfileHelper.ZeroProfile();
                    endUseCarrierProfiles[SystemEnergyCarrier.Electricity] = electricityCarrier;
                }

                for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
                {
                    electricityCarrier[hour] += endUseAux[hour];
                }

                diagnostics.Add(CreateInfo(
                    "AE-SYS-FINAL-ENERGY-AUXILIARY-INCLUDED",
                    $"Auxiliary electricity was included for end use '{endUse}'."));
            }

            var unmet = requested
                .Select((value, index) => Math.Max(0.0, value - supplied[index]))
                .ToArray();
            if (unmet.Any(value => value > 0.0))
            {
                diagnostics.Add(CreateWarning(
                    "AE-SYS-FINAL-ENERGY-UNMET-LOAD-DETECTED",
                    $"Unmet system load detected for end use '{endUse}'."));
            }

            foreach (var carrierEntry in endUseCarrierProfiles)
            {
                if (!hourlyFinalByCarrier.TryGetValue(carrierEntry.Key, out var carrierTotals))
                {
                    carrierTotals = SystemEnergyProfileHelper.ZeroProfile();
                    hourlyFinalByCarrier[carrierEntry.Key] = carrierTotals;
                }

                for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
                {
                    carrierTotals[hour] += carrierEntry.Value[hour];
                }
            }

            for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
            {
                totalHourlyAux[hour] += endUseAux[hour];
                totalHourlyFinal[hour] += endUseCarrierProfiles.Values.Sum(profile => profile[hour]);
            }

            var monthlyFinalByCarrier = endUseCarrierProfiles.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<double>)SystemEnergyProfileHelper.AggregateMonthly(kvp.Value),
                EqualityComparer<SystemEnergyCarrier>.Default);

            endUseResults.Add(new SystemEnergyEndUseFinalEnergyResult(
                EndUse: endUse,
                HourlySystemLoadBeforeGenerationKWh8760: requested,
                HourlySuppliedSystemLoadKWh8760: supplied,
                HourlyUnmetSystemLoadKWh8760: unmet,
                HourlyFinalEnergyByCarrierKWh8760: endUseCarrierProfiles.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (IReadOnlyList<double>)kvp.Value,
                    EqualityComparer<SystemEnergyCarrier>.Default),
                HourlyAuxiliaryElectricityKWh8760: endUseAux,
                AnnualSystemLoadBeforeGenerationKWh: requested.Sum(),
                AnnualSuppliedSystemLoadKWh: supplied.Sum(),
                AnnualUnmetSystemLoadKWh: unmet.Sum(),
                AnnualFinalEnergyByCarrierKWh: endUseCarrierProfiles.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Sum(),
                    EqualityComparer<SystemEnergyCarrier>.Default),
                AnnualAuxiliaryElectricityKWh: endUseAux.Sum(),
                MonthlySuppliedSystemLoadKWh: SystemEnergyProfileHelper.AggregateMonthly(supplied),
                MonthlyUnmetSystemLoadKWh: SystemEnergyProfileHelper.AggregateMonthly(unmet),
                MonthlyFinalEnergyByCarrierKWh: monthlyFinalByCarrier,
                Diagnostics: []));
        }

        var monthlyByCarrier = hourlyFinalByCarrier.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<double>)SystemEnergyProfileHelper.AggregateMonthly(kvp.Value),
            EqualityComparer<SystemEnergyCarrier>.Default);

        var annualByCarrier = hourlyFinalByCarrier.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Sum(),
            EqualityComparer<SystemEnergyCarrier>.Default);

        var annualUnmet = endUseResults.Sum(result => result.AnnualUnmetSystemLoadKWh);
        var status = ResolveStatus(generatorResults, annualUnmet);
        diagnostics.Add(CreateInfo("AE-SYS-FINAL-ENERGY-BY-CARRIER-AGGREGATED", "Final energy by carrier was aggregated."));
        diagnostics.Add(CreateInfo("AE-SYS-FINAL-ENERGY-AGGREGATED", "Final energy result was aggregated from generator dispatch results."));
        diagnostics.Add(CreateInfo(
            status switch
            {
                SystemEnergyFinalEnergyStatus.Calculated => "AE-SYS-FINAL-ENERGY-STATUS-CALCULATED",
                SystemEnergyFinalEnergyStatus.PartiallyCalculated => "AE-SYS-FINAL-ENERGY-STATUS-PARTIAL",
                _ => "AE-SYS-FINAL-ENERGY-STATUS-NOT-CALCULABLE"
            },
            $"Final energy aggregation status resolved to '{status}'."));

        var disclosure = MergeDisclosure(
            _disclosureFactory.CreateSystemEnergyEn15316Disclosure(),
            disclosureOverride,
            diagnostics);

        return new SystemEnergyFinalEnergyResult(
            CalculationId: calculationId,
            Generators: generatorResults,
            EndUses: endUseResults,
            HourlyFinalEnergyByCarrierKWh8760: hourlyFinalByCarrier.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<double>)kvp.Value,
                EqualityComparer<SystemEnergyCarrier>.Default),
            AnnualFinalEnergyByCarrierKWh: annualByCarrier,
            MonthlyFinalEnergyByCarrierKWh: monthlyByCarrier,
            HourlyTotalFinalEnergyKWh8760: totalHourlyFinal,
            HourlyTotalAuxiliaryElectricityKWh8760: totalHourlyAux,
            AnnualTotalFinalEnergyKWh: annualByCarrier.Values.Sum(),
            AnnualTotalAuxiliaryElectricityKWh: totalHourlyAux.Sum(),
            AnnualTotalUnmetSystemLoadKWh: annualUnmet,
            Status: status,
            Disclosure: disclosure,
            Diagnostics: diagnostics);
    }

    private static SystemEnergyFinalEnergyStatus ResolveStatus(
        IReadOnlyList<SystemEnergyGeneratorResult> generatorResults,
        double annualUnmetKWh)
    {
        if (generatorResults.Count == 0)
            return SystemEnergyFinalEnergyStatus.NotCalculable;

        if (generatorResults.All(result => result.Status == SystemEnergyFinalEnergyStatus.HandoffOnly))
            return SystemEnergyFinalEnergyStatus.HandoffOnly;

        if (annualUnmetKWh > 0.0)
            return SystemEnergyFinalEnergyStatus.PartiallyCalculated;

        if (generatorResults.All(result => result.Status == SystemEnergyFinalEnergyStatus.Disabled))
            return SystemEnergyFinalEnergyStatus.Disabled;

        if (generatorResults.All(result => result.AnnualFinalEnergyKWh <= 0.0))
            return SystemEnergyFinalEnergyStatus.NotCalculable;

        return SystemEnergyFinalEnergyStatus.Calculated;
    }

    private static StandardCalculationDisclosure MergeDisclosure(
        StandardCalculationDisclosure baseDisclosure,
        StandardCalculationDisclosure? disclosureOverride,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (disclosureOverride is null)
            return baseDisclosure;

        var baseBoundary = baseDisclosure.ClaimBoundary;
        var overrideBoundary = disclosureOverride.ClaimBoundary ?? baseBoundary;

        var forbiddenClaims = overrideBoundary.ForbiddenClaims
            .Where(claim => !string.IsNullOrWhiteSpace(claim))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        foreach (var requiredClaim in RequiredForbiddenClaims)
        {
            if (!forbiddenClaims.Contains(requiredClaim, StringComparer.Ordinal))
                forbiddenClaims.Add(requiredClaim);
        }

        var removedAllowedClaims = new List<string>();
        var allowedClaims = (overrideBoundary.AllowedClaims ?? [])
            .Where(claim => !string.IsNullOrWhiteSpace(claim))
            .Where(claim =>
            {
                var containsForbidden = forbiddenClaims.Any(forbidden =>
                    claim.Contains(forbidden, StringComparison.Ordinal));
                if (containsForbidden)
                    removedAllowedClaims.Add(claim);
                return !containsForbidden;
            })
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (removedAllowedClaims.Count > 0)
        {
            diagnostics.Add(CreateWarning(
                "AE-SYS-DISCLOSURE-OVERRIDE-SANITIZED",
                $"Disclosure override removed forbidden allowed-claim entries: {string.Join(", ", removedAllowedClaims)}."));
        }

        var mergedBoundary = new StandardClaimBoundary(
            AllowedClaims: allowedClaims,
            ForbiddenClaims: forbiddenClaims,
            Limitations: overrideBoundary.Limitations ?? baseBoundary.Limitations,
            Assumptions: overrideBoundary.Assumptions ?? baseBoundary.Assumptions);

        return disclosureOverride with
        {
            CalculationPath = string.IsNullOrWhiteSpace(disclosureOverride.CalculationPath)
                ? baseDisclosure.CalculationPath
                : disclosureOverride.CalculationPath,
            ClaimBoundary = mergedBoundary
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

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            Source);
}
