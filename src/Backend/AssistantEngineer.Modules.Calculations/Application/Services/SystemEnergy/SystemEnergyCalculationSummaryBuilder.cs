using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class SystemEnergyCalculationSummaryBuilder : ISystemEnergyCalculationSummaryBuilder
{
    private static readonly IReadOnlyList<string> RequiredForbiddenClaims =
    [
        "Full ISO compliance",
        "Full EN compliance",
        "StandardReference equivalence",
        "EnergyPlus comparison workflow",
        "ASHRAE 140 / BESTEST-style validation anchor"
    ];

    private const string Source = "SystemEnergyCalculationSummaryBuilder";

    public SystemEnergyCalculationSummary Build(SystemEnergyPrimaryEnergyResult primaryEnergyResult)
    {
        ArgumentNullException.ThrowIfNull(primaryEnergyResult);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(primaryEnergyResult.Diagnostics);

        var annualEmissionsByCarrier = primaryEnergyResult.Emissions
            .GroupBy(result => result.Carrier)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(result => result.AnnualEmissionsKg),
                EqualityComparer<SystemEnergyCarrier>.Default);

        var carrierSummaries = primaryEnergyResult.Carriers
            .Select(carrier =>
            {
                var carrierDiagnostics = new List<StandardCalculationDiagnostic>();
                carrierDiagnostics.AddRange(carrier.Diagnostics);
                carrierDiagnostics.Add(CreateInfo(
                    "AE-SYS-SUMMARY-CARRIER-BUILT",
                    $"Carrier summary built for '{carrier.Carrier}'."));

                return new SystemEnergyCarrierSummary(
                    Carrier: carrier.Carrier,
                    AnnualFinalEnergyKWh: carrier.AnnualFinalEnergyKWh,
                    AnnualRenewablePrimaryEnergyKWh: carrier.AnnualRenewablePrimaryEnergyKWh,
                    AnnualNonRenewablePrimaryEnergyKWh: carrier.AnnualNonRenewablePrimaryEnergyKWh,
                    AnnualTotalPrimaryEnergyKWh: carrier.AnnualTotalPrimaryEnergyKWh,
                    AnnualEmissionsKg: annualEmissionsByCarrier.TryGetValue(carrier.Carrier, out var emissions)
                        ? emissions
                        : null,
                    MonthlyFinalEnergyKWh: carrier.MonthlyFinalEnergyKWh,
                    MonthlyTotalPrimaryEnergyKWh: carrier.MonthlyTotalPrimaryEnergyKWh,
                    Diagnostics: carrierDiagnostics);
            })
            .ToArray();

        var defaultEmissionByCarrier = primaryEnergyResult.FactorSet.EmissionFactors
            .GroupBy(factor => factor.Carrier)
            .ToDictionary(
                group => group.Key,
                group => group.First().KgPerKWh,
                EqualityComparer<SystemEnergyCarrier>.Default);

        var endUseSummaries = primaryEnergyResult.EndUses
            .Select(endUse =>
            {
                var endUseDiagnostics = new List<StandardCalculationDiagnostic>();
                endUseDiagnostics.AddRange(endUse.Diagnostics);
                endUseDiagnostics.Add(CreateInfo(
                    "AE-SYS-SUMMARY-ENDUSE-BUILT",
                    $"End-use summary built for '{endUse.EndUse}'."));

                double? annualEmissions = null;
                if (endUse.AnnualFinalEnergyByCarrierKWh.Count > 0)
                {
                    var emissionsTotal = 0.0;
                    var emissionsCount = 0;
                    foreach (var carrierEntry in endUse.AnnualFinalEnergyByCarrierKWh)
                    {
                        if (!defaultEmissionByCarrier.TryGetValue(carrierEntry.Key, out var kgPerKWh))
                            continue;

                        emissionsTotal += carrierEntry.Value * kgPerKWh;
                        emissionsCount++;
                    }

                    if (emissionsCount > 0)
                        annualEmissions = emissionsTotal;
                }

                return new SystemEnergyEndUseSummary(
                    EndUse: endUse.EndUse,
                    AnnualFinalEnergyKWh: endUse.AnnualFinalEnergyKWh,
                    AnnualRenewablePrimaryEnergyKWh: endUse.AnnualRenewablePrimaryEnergyKWh,
                    AnnualNonRenewablePrimaryEnergyKWh: endUse.AnnualNonRenewablePrimaryEnergyKWh,
                    AnnualTotalPrimaryEnergyKWh: endUse.AnnualTotalPrimaryEnergyKWh,
                    AnnualEmissionsKg: annualEmissions,
                    AnnualFinalEnergyByCarrierKWh: endUse.AnnualFinalEnergyByCarrierKWh,
                    Diagnostics: endUseDiagnostics);
            })
            .ToArray();

        var disclosureSummary = BuildDisclosureSummary(primaryEnergyResult.Disclosure);

        diagnostics.AddRange(carrierSummaries.SelectMany(summary => summary.Diagnostics));
        diagnostics.AddRange(endUseSummaries.SelectMany(summary => summary.Diagnostics));
        diagnostics.AddRange(disclosureSummary.Diagnostics);

        return new SystemEnergyCalculationSummary(
            CalculationId: primaryEnergyResult.CalculationId,
            AnnualTotalFinalEnergyKWh: primaryEnergyResult.AnnualTotalFinalEnergyKWh,
            AnnualTotalRenewablePrimaryEnergyKWh: primaryEnergyResult.AnnualTotalRenewablePrimaryEnergyKWh,
            AnnualTotalNonRenewablePrimaryEnergyKWh: primaryEnergyResult.AnnualTotalNonRenewablePrimaryEnergyKWh,
            AnnualTotalPrimaryEnergyKWh: primaryEnergyResult.AnnualTotalPrimaryEnergyKWh,
            AnnualTotalEmissionsKg: primaryEnergyResult.AnnualTotalEmissionsKg,
            Carriers: carrierSummaries,
            EndUses: endUseSummaries,
            DisclosureSummary: disclosureSummary,
            Diagnostics: diagnostics);
    }

    private static SystemEnergyDisclosureSummary BuildDisclosureSummary(
        StandardCalculationDisclosure disclosure)
    {
        var boundary = disclosure.ClaimBoundary;

        var forbidden = boundary.ForbiddenClaims
            .Where(claim => !string.IsNullOrWhiteSpace(claim))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        foreach (var required in RequiredForbiddenClaims)
        {
            if (!forbidden.Contains(required, StringComparer.Ordinal))
                forbidden.Add(required);
        }

        var allowed = boundary.AllowedClaims
            .Where(claim => !string.IsNullOrWhiteSpace(claim))
            .Where(claim => forbidden.All(required => !claim.Contains(required, StringComparison.Ordinal)))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var mappedStatus = SystemEnergyDisclosureStatusMapper.FromStandardMode(disclosure.Mode);
        var status = mappedStatus == SystemEnergyDisclosureStatus.StandardInspired
            ? SystemEnergyDisclosureStatus.StandardInspired
            : SystemEnergyDisclosureStatus.NotForCompliance;

        var diagnostics = new List<StandardCalculationDiagnostic>
        {
            CreateInfo(
                "AE-SYS-SUMMARY-DISCLOSURE-BUILT",
                "Disclosure summary was built for reporting output."),
            CreateWarning(
                "AE-SYS-SUMMARY-NOT-FOR-COMPLIANCE",
                "System-energy summary is not for compliance claims without external validation.")
        };
        diagnostics.AddRange(disclosure.Diagnostics);

        return new SystemEnergyDisclosureSummary(
            Status: status,
            AllowedClaims: allowed,
            ForbiddenClaims: forbidden,
            Assumptions: boundary.Assumptions,
            Limitations: boundary.Limitations,
            Diagnostics: diagnostics);
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
