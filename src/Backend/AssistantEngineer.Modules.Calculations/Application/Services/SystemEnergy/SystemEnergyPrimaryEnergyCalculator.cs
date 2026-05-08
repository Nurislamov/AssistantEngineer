using AssistantEngineer.Modules.Calculations.Application.Abstractions.Standards;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class SystemEnergyPrimaryEnergyCalculator : ISystemEnergyPrimaryEnergyCalculator
{
    private static readonly IReadOnlyList<string> RequiredForbiddenClaims =
    [
        "Full ISO compliance",
        "Full EN compliance",
        "pyBuildingEnergy parity",
        "EnergyPlus parity",
        "ASHRAE 140 validation"
    ];

    private const string Source = "SystemEnergyPrimaryEnergyCalculator";
    private readonly ISystemEnergyFactorSetValidator _factorSetValidator;
    private readonly ISystemEnergyEmissionCalculator _emissionCalculator;
    private readonly IStandardCalculationDisclosureFactory _disclosureFactory;

    public SystemEnergyPrimaryEnergyCalculator(
        ISystemEnergyFactorSetValidator factorSetValidator,
        ISystemEnergyEmissionCalculator emissionCalculator,
        IStandardCalculationDisclosureFactory disclosureFactory)
    {
        _factorSetValidator = factorSetValidator ?? throw new ArgumentNullException(nameof(factorSetValidator));
        _emissionCalculator = emissionCalculator ?? throw new ArgumentNullException(nameof(emissionCalculator));
        _disclosureFactory = disclosureFactory ?? throw new ArgumentNullException(nameof(disclosureFactory));
    }

    public SystemEnergyPrimaryEnergyResult Calculate(
        SystemEnergyFinalEnergyResult finalEnergyResult,
        SystemEnergyFactorSet factorSet)
    {
        ArgumentNullException.ThrowIfNull(finalEnergyResult);
        ArgumentNullException.ThrowIfNull(factorSet);

        var diagnostics = new List<StandardCalculationDiagnostic>();

        if (string.IsNullOrWhiteSpace(finalEnergyResult.CalculationId))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-PRIMARY-FINAL-PROFILE-INVALID",
                "Final-energy result calculation id is required for primary-energy calculation."));
        }

        foreach (var carrierProfile in finalEnergyResult.HourlyFinalEnergyByCarrierKWh8760)
        {
            if (!SystemEnergyProfileHelper.IsValidProfile(
                    carrierProfile.Value,
                    SystemEnergyProfileHelper.HoursPerYear))
            {
                diagnostics.Add(CreateError(
                    "AE-SYS-PRIMARY-FINAL-PROFILE-INVALID",
                    $"Final-energy hourly profile for carrier '{carrierProfile.Key}' must contain 8760 finite non-negative values."));
            }
        }

        var factorValidation = _factorSetValidator.Validate(factorSet);
        diagnostics.AddRange(factorValidation.Diagnostics);

        var factorByCarrier = factorSet.PrimaryEnergyFactors
            .GroupBy(factor => factor.Carrier)
            .ToDictionary(group => group.Key, group => group.First(), EqualityComparer<SystemEnergyCarrier>.Default);

        var carrierResults = new List<SystemEnergyCarrierPrimaryEnergyResult>();
        foreach (var carrierEntry in finalEnergyResult.HourlyFinalEnergyByCarrierKWh8760)
        {
            var carrier = carrierEntry.Key;
            var hourlyFinal = SystemEnergyProfileHelper.Ensure8760(carrierEntry.Value);
            var carrierDiagnostics = new List<StandardCalculationDiagnostic>();

            if (!factorByCarrier.TryGetValue(carrier, out var factor))
            {
                carrierDiagnostics.Add(CreateWarning(
                    "AE-SYS-PRIMARY-FACTOR-MISSING",
                    $"No primary-energy factor found for carrier '{carrier}'. Zero primary energy was used."));

                factor = new SystemEnergyPrimaryEnergyFactor(
                    Carrier: carrier,
                    RenewableFactor: 0.0,
                    NonRenewableFactor: 0.0,
                    TotalFactor: 0.0,
                    SourceKind: SystemEnergyFactorSourceKind.Unknown,
                    Source: "MissingPrimaryFactor",
                    Region: factorSet.Region,
                    Year: factorSet.Year,
                    Diagnostics: []);
            }

            carrierDiagnostics.AddRange(factor.Diagnostics);

            var hourlyRenewable = new double[SystemEnergyProfileHelper.HoursPerYear];
            var hourlyNonRenewable = new double[SystemEnergyProfileHelper.HoursPerYear];
            var hourlyTotal = new double[SystemEnergyProfileHelper.HoursPerYear];
            for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
            {
                hourlyRenewable[hour] = hourlyFinal[hour] * factor.RenewableFactor;
                hourlyNonRenewable[hour] = hourlyFinal[hour] * factor.NonRenewableFactor;
                hourlyTotal[hour] = hourlyFinal[hour] * factor.TotalFactor;
            }

            carrierDiagnostics.Add(CreateInfo(
                "AE-SYS-PRIMARY-CARRIER-CALCULATED",
                $"Primary energy was calculated for carrier '{carrier}'."));

            carrierResults.Add(new SystemEnergyCarrierPrimaryEnergyResult(
                Carrier: carrier,
                HourlyFinalEnergyKWh8760: hourlyFinal,
                HourlyRenewablePrimaryEnergyKWh8760: hourlyRenewable,
                HourlyNonRenewablePrimaryEnergyKWh8760: hourlyNonRenewable,
                HourlyTotalPrimaryEnergyKWh8760: hourlyTotal,
                AnnualFinalEnergyKWh: hourlyFinal.Sum(),
                AnnualRenewablePrimaryEnergyKWh: hourlyRenewable.Sum(),
                AnnualNonRenewablePrimaryEnergyKWh: hourlyNonRenewable.Sum(),
                AnnualTotalPrimaryEnergyKWh: hourlyTotal.Sum(),
                MonthlyFinalEnergyKWh: SystemEnergyProfileHelper.AggregateMonthly(hourlyFinal),
                MonthlyRenewablePrimaryEnergyKWh: SystemEnergyProfileHelper.AggregateMonthly(hourlyRenewable),
                MonthlyNonRenewablePrimaryEnergyKWh: SystemEnergyProfileHelper.AggregateMonthly(hourlyNonRenewable),
                MonthlyTotalPrimaryEnergyKWh: SystemEnergyProfileHelper.AggregateMonthly(hourlyTotal),
                Factor: factor,
                Diagnostics: carrierDiagnostics));
        }

        var endUseResults = new List<SystemEnergyEndUsePrimaryEnergyResult>();
        foreach (var endUseResult in finalEnergyResult.EndUses)
        {
            var endUseDiagnostics = new List<StandardCalculationDiagnostic>();

            var hourlyFinalByCarrier = new Dictionary<SystemEnergyCarrier, IReadOnlyList<double>>();
            var hourlyPrimaryByCarrier = new Dictionary<SystemEnergyCarrier, IReadOnlyList<double>>();
            var annualFinalByCarrierForEndUse = new Dictionary<SystemEnergyCarrier, double>();
            var annualPrimaryByCarrier = new Dictionary<SystemEnergyCarrier, double>();

            var hourlyFinalTotal = new double[SystemEnergyProfileHelper.HoursPerYear];
            var hourlyRenewableTotal = new double[SystemEnergyProfileHelper.HoursPerYear];
            var hourlyNonRenewableTotal = new double[SystemEnergyProfileHelper.HoursPerYear];
            var hourlyPrimaryTotal = new double[SystemEnergyProfileHelper.HoursPerYear];

            foreach (var carrierEntry in endUseResult.HourlyFinalEnergyByCarrierKWh8760)
            {
                var carrier = carrierEntry.Key;
                var hourlyFinal = SystemEnergyProfileHelper.Ensure8760(carrierEntry.Value);
                hourlyFinalByCarrier[carrier] = hourlyFinal;
                annualFinalByCarrierForEndUse[carrier] = hourlyFinal.Sum();

                if (!factorByCarrier.TryGetValue(carrier, out var factor))
                {
                    endUseDiagnostics.Add(CreateWarning(
                        "AE-SYS-PRIMARY-FACTOR-MISSING",
                        $"No primary-energy factor found for end use '{endUseResult.EndUse}' carrier '{carrier}'. Zero primary energy was used."));

                    factor = new SystemEnergyPrimaryEnergyFactor(
                        Carrier: carrier,
                        RenewableFactor: 0.0,
                        NonRenewableFactor: 0.0,
                        TotalFactor: 0.0,
                        SourceKind: SystemEnergyFactorSourceKind.Unknown,
                        Source: "MissingPrimaryFactor",
                        Region: factorSet.Region,
                        Year: factorSet.Year,
                        Diagnostics: []);
                }

                var hourlyPrimary = new double[SystemEnergyProfileHelper.HoursPerYear];
                for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
                {
                    hourlyPrimary[hour] = hourlyFinal[hour] * factor.TotalFactor;
                    hourlyFinalTotal[hour] += hourlyFinal[hour];
                    hourlyRenewableTotal[hour] += hourlyFinal[hour] * factor.RenewableFactor;
                    hourlyNonRenewableTotal[hour] += hourlyFinal[hour] * factor.NonRenewableFactor;
                    hourlyPrimaryTotal[hour] += hourlyPrimary[hour];
                }

                hourlyPrimaryByCarrier[carrier] = hourlyPrimary;
                annualPrimaryByCarrier[carrier] = hourlyPrimary.Sum();
            }

            endUseDiagnostics.Add(CreateInfo(
                "AE-SYS-PRIMARY-ENDUSE-CALCULATED",
                $"Primary energy was calculated for end use '{endUseResult.EndUse}'."));

            endUseResults.Add(new SystemEnergyEndUsePrimaryEnergyResult(
                EndUse: endUseResult.EndUse,
                HourlyFinalEnergyByCarrierKWh8760: hourlyFinalByCarrier,
                HourlyTotalPrimaryEnergyByCarrierKWh8760: hourlyPrimaryByCarrier,
                AnnualFinalEnergyByCarrierKWh: annualFinalByCarrierForEndUse,
                AnnualTotalPrimaryEnergyByCarrierKWh: annualPrimaryByCarrier,
                AnnualFinalEnergyKWh: hourlyFinalTotal.Sum(),
                AnnualRenewablePrimaryEnergyKWh: hourlyRenewableTotal.Sum(),
                AnnualNonRenewablePrimaryEnergyKWh: hourlyNonRenewableTotal.Sum(),
                AnnualTotalPrimaryEnergyKWh: hourlyPrimaryTotal.Sum(),
                MonthlyFinalEnergyKWh: SystemEnergyProfileHelper.AggregateMonthly(hourlyFinalTotal),
                MonthlyRenewablePrimaryEnergyKWh: SystemEnergyProfileHelper.AggregateMonthly(hourlyRenewableTotal),
                MonthlyNonRenewablePrimaryEnergyKWh: SystemEnergyProfileHelper.AggregateMonthly(hourlyNonRenewableTotal),
                MonthlyTotalPrimaryEnergyKWh: SystemEnergyProfileHelper.AggregateMonthly(hourlyPrimaryTotal),
                Diagnostics: endUseDiagnostics));
        }

        var emissionResults = _emissionCalculator.Calculate(finalEnergyResult, factorSet);
        foreach (var emissionResult in emissionResults)
        {
            diagnostics.AddRange(emissionResult.Diagnostics);
        }

        if (factorSet.EmissionFactors.Count == 0)
        {
            diagnostics.Add(CreateWarning(
                "AE-SYS-EMISSION-FACTORS-NOT-PROVIDED",
                "No emission factors were provided. Emission results are omitted."));
        }
        else
        {
            var emissionCarriers = factorSet.EmissionFactors
                .Select(factor => factor.Carrier)
                .ToHashSet();

            foreach (var carrier in finalEnergyResult.HourlyFinalEnergyByCarrierKWh8760.Keys)
            {
                if (!emissionCarriers.Contains(carrier))
                {
                    diagnostics.Add(CreateWarning(
                        "AE-SYS-EMISSION-FACTOR-MISSING",
                        $"No emission factor provided for carrier '{carrier}'."));
                }
            }
        }

        var annualFinalByCarrier = carrierResults.ToDictionary(
            result => result.Carrier,
            result => result.AnnualFinalEnergyKWh,
            EqualityComparer<SystemEnergyCarrier>.Default);
        var annualRenewableByCarrier = carrierResults.ToDictionary(
            result => result.Carrier,
            result => result.AnnualRenewablePrimaryEnergyKWh,
            EqualityComparer<SystemEnergyCarrier>.Default);
        var annualNonRenewableByCarrier = carrierResults.ToDictionary(
            result => result.Carrier,
            result => result.AnnualNonRenewablePrimaryEnergyKWh,
            EqualityComparer<SystemEnergyCarrier>.Default);
        var annualTotalByCarrier = carrierResults.ToDictionary(
            result => result.Carrier,
            result => result.AnnualTotalPrimaryEnergyKWh,
            EqualityComparer<SystemEnergyCarrier>.Default);

        var monthlyFinalTotals = SumMonthly(carrierResults.Select(result => result.MonthlyFinalEnergyKWh));
        var monthlyRenewableTotals = SumMonthly(carrierResults.Select(result => result.MonthlyRenewablePrimaryEnergyKWh));
        var monthlyNonRenewableTotals = SumMonthly(carrierResults.Select(result => result.MonthlyNonRenewablePrimaryEnergyKWh));
        var monthlyPrimaryTotals = SumMonthly(carrierResults.Select(result => result.MonthlyTotalPrimaryEnergyKWh));

        diagnostics.Add(CreateInfo(
            "AE-SYS-PRIMARY-TOTALS-AGGREGATED",
            "Primary-energy totals were aggregated by carrier, end use, month, and year."));

        diagnostics.AddRange(carrierResults.SelectMany(result => result.Diagnostics));
        diagnostics.AddRange(endUseResults.SelectMany(result => result.Diagnostics));

        diagnostics.Add(CreateWarning(
            "AE-SYS-PRIMARY-FACTORS-NOT-COMPLIANCE-DATA",
            "Primary-energy factors are user/project/reference inputs and are not national-annex compliance data without external verification."));

        var disclosure = BuildDisclosure();

        return new SystemEnergyPrimaryEnergyResult(
            CalculationId: finalEnergyResult.CalculationId,
            FinalEnergyResult: finalEnergyResult,
            FactorSet: factorSet,
            Carriers: carrierResults,
            EndUses: endUseResults,
            Emissions: emissionResults,
            AnnualFinalEnergyByCarrierKWh: annualFinalByCarrier,
            AnnualRenewablePrimaryEnergyByCarrierKWh: annualRenewableByCarrier,
            AnnualNonRenewablePrimaryEnergyByCarrierKWh: annualNonRenewableByCarrier,
            AnnualTotalPrimaryEnergyByCarrierKWh: annualTotalByCarrier,
            AnnualTotalFinalEnergyKWh: carrierResults.Sum(result => result.AnnualFinalEnergyKWh),
            AnnualTotalRenewablePrimaryEnergyKWh: carrierResults.Sum(result => result.AnnualRenewablePrimaryEnergyKWh),
            AnnualTotalNonRenewablePrimaryEnergyKWh: carrierResults.Sum(result => result.AnnualNonRenewablePrimaryEnergyKWh),
            AnnualTotalPrimaryEnergyKWh: carrierResults.Sum(result => result.AnnualTotalPrimaryEnergyKWh),
            AnnualTotalEmissionsKg: emissionResults.Count > 0 ? emissionResults.Sum(result => result.AnnualEmissionsKg) : null,
            MonthlyTotalFinalEnergyKWh: monthlyFinalTotals,
            MonthlyTotalRenewablePrimaryEnergyKWh: monthlyRenewableTotals,
            MonthlyTotalNonRenewablePrimaryEnergyKWh: monthlyNonRenewableTotals,
            MonthlyTotalPrimaryEnergyKWh: monthlyPrimaryTotals,
            Disclosure: disclosure,
            Diagnostics: diagnostics);
    }

    private StandardCalculationDisclosure BuildDisclosure()
    {
        var baseDisclosure = _disclosureFactory.CreateSystemEnergyEn15316Disclosure();
        var boundary = baseDisclosure.ClaimBoundary;

        var forbiddenClaims = boundary.ForbiddenClaims
            .Where(claim => !string.IsNullOrWhiteSpace(claim))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        foreach (var required in RequiredForbiddenClaims)
        {
            if (!forbiddenClaims.Contains(required, StringComparer.Ordinal))
                forbiddenClaims.Add(required);
        }

        var allowedClaims = boundary.AllowedClaims
            .Where(claim => !string.IsNullOrWhiteSpace(claim))
            .Where(claim => forbiddenClaims.All(forbidden => !claim.Contains(forbidden, StringComparison.Ordinal)))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var assumptions = boundary.Assumptions
            .Concat(
            [
                "Primary-energy factors are user/project/reference inputs and must be explicitly disclosed.",
                "National-annex compliance requires externally provided and verified factor datasets."
            ])
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var limitations = boundary.Limitations
            .Concat(
            [
                "This stage is deterministic EN15316-style primary-energy and reporting-summary preparation only.",
                "Results are not a certification-grade EPB compliance report."
            ])
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return baseDisclosure with
        {
            ClaimBoundary = new StandardClaimBoundary(
                AllowedClaims: allowedClaims,
                ForbiddenClaims: forbiddenClaims,
                Limitations: limitations,
                Assumptions: assumptions)
        };
    }

    private static IReadOnlyList<double> SumMonthly(IEnumerable<IReadOnlyList<double>> profiles)
    {
        var totals = new double[SystemEnergyProfileHelper.MonthsPerYear];
        foreach (var profile in profiles)
        {
            for (var month = 0; month < SystemEnergyProfileHelper.MonthsPerYear; month++)
            {
                if (month < profile.Count && double.IsFinite(profile[month]))
                    totals[month] += profile[month];
            }
        }

        return totals;
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

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Error,
            code,
            message,
            Source);
}
