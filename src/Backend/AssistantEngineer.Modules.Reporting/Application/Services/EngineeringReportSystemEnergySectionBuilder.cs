using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal sealed class EngineeringReportSystemEnergySectionBuilder
{
    private readonly IEngineeringReportDiagnosticAggregator _diagnosticAggregator;
    private readonly EngineeringReportFormattingService _formatting;

    public EngineeringReportSystemEnergySectionBuilder(
        IEngineeringReportDiagnosticAggregator diagnosticAggregator,
        EngineeringReportFormattingService formatting)
    {
        _diagnosticAggregator = diagnosticAggregator;
        _formatting = formatting;
    }

    public void BuildSystemEnergySections(
        EngineeringReportGenerationRequest request,
        ICollection<EngineeringReportSection> sections,
        ICollection<string> assumptions,
        ICollection<EngineeringReportDiagnostic> diagnostics,
        ICollection<EngineeringReportValue> summaries,
        ref int order)
    {
        if (!EngineeringReportSectionSelectionPolicy.ShouldIncludeSection(
                request.ReportKind,
                request.SystemEnergySummary is not null,
                diagnostics,
                "system-energy",
                CalculationTraceModuleKind.SystemEnergy,
                request.DetailLevel))
            return;

        var summary = request.SystemEnergySummary!;
        assumptions.AddRange(summary.DisclosureSummary.Assumptions);

        summaries.Add(new EngineeringReportValue("final_energy_total_kwh", "Total final energy", summary.AnnualTotalFinalEnergyKWh, new EngineeringReportUnit("kWh")));
        summaries.Add(new EngineeringReportValue("primary_energy_total_kwh", "Total primary energy", summary.AnnualTotalPrimaryEnergyKWh, new EngineeringReportUnit("kWh")));

        sections.Add(new EngineeringReportSection(
            SectionId: "system-energy",
            SectionKind: EngineeringReportSectionKind.SystemEnergy,
            Title: "System Energy",
            Order: ++order,
            SummaryText: "System energy chain summary including final, primary and carbon indicators.",
            KeyValues:
            [
                new EngineeringReportValue("annual_total_final_kwh", "Annual total final energy", summary.AnnualTotalFinalEnergyKWh, new EngineeringReportUnit("kWh")),
                new EngineeringReportValue("annual_total_primary_kwh", "Annual total primary energy", summary.AnnualTotalPrimaryEnergyKWh, new EngineeringReportUnit("kWh")),
                new EngineeringReportValue("annual_total_emissions_kg", "Annual CO2", summary.AnnualTotalEmissionsKg, new EngineeringReportUnit("kg")),
                new EngineeringReportValue("carrier_count", "Carrier count", summary.Carriers.Count),
                new EngineeringReportValue("end_use_count", "End use count", summary.EndUses.Count)
            ],
            Tables:
            [
                BuildSystemEnergyCarrierTable(summary.Carriers),
                BuildSystemEnergyEndUseTable(summary.EndUses)
            ],
            ChartPlaceholders: [],
            Diagnostics: [],
            Assumptions: [],
            ChildSections: []));

        sections.Add(new EngineeringReportSection(
            SectionId: "final-energy",
            SectionKind: EngineeringReportSectionKind.FinalEnergy,
            Title: "Final Energy",
            Order: ++order,
            SummaryText: "Final energy split by carrier.",
            KeyValues: [],
            Tables: [BuildSystemEnergyCarrierTable(summary.Carriers)],
            ChartPlaceholders: [],
            Diagnostics: [],
            Assumptions: [],
            ChildSections: []));

        sections.Add(new EngineeringReportSection(
            SectionId: "primary-energy-carbon",
            SectionKind: EngineeringReportSectionKind.PrimaryEnergyAndCarbon,
            Title: "Primary Energy and Carbon",
            Order: ++order,
            SummaryText: "Primary energy and carbon indicators by carrier and end use.",
            KeyValues:
            [
                new EngineeringReportValue("primary_total_kwh", "Total primary energy", summary.AnnualTotalPrimaryEnergyKWh, new EngineeringReportUnit("kWh")),
                new EngineeringReportValue("renewable_primary_total_kwh", "Renewable primary energy", summary.AnnualTotalRenewablePrimaryEnergyKWh, new EngineeringReportUnit("kWh")),
                new EngineeringReportValue("non_renewable_primary_total_kwh", "Non-renewable primary energy", summary.AnnualTotalNonRenewablePrimaryEnergyKWh, new EngineeringReportUnit("kWh")),
                new EngineeringReportValue("co2_total_kg", "Total CO2", summary.AnnualTotalEmissionsKg, new EngineeringReportUnit("kg"))
            ],
            Tables: [BuildSystemEnergyEndUseTable(summary.EndUses)],
            ChartPlaceholders: [],
            Diagnostics: [],
            Assumptions: [],
            ChildSections: []));

        foreach (var diagnostic in summary.Diagnostics)
            diagnostics.Add(_diagnosticAggregator.FromStandardDiagnostic(diagnostic, CalculationTraceModuleKind.SystemEnergy, "SystemEnergySummary"));
    }

    private EngineeringReportTable BuildSystemEnergyCarrierTable(
        IReadOnlyList<SystemEnergyCarrierSummary> carriers) =>
        new(
            TableId: "system-energy-by-carrier",
            Title: "Final/primary/CO2 by carrier",
            Columns: ["Carrier", "Final (kWh)", "Primary total (kWh)", "CO2 (kg)"],
            Rows: carriers
                .OrderBy(item => item.Carrier)
                .Select(item => (IReadOnlyList<string>)
                [
                    item.Carrier.ToString(),
                    _formatting.FormatFixed2(item.AnnualFinalEnergyKWh),
                    _formatting.FormatFixed2(item.AnnualTotalPrimaryEnergyKWh),
                    _formatting.FormatNullableFixed2(item.AnnualEmissionsKg)
                ])
                .ToArray(),
            Units: new Dictionary<string, string>
            {
                ["Final (kWh)"] = "kWh",
                ["Primary total (kWh)"] = "kWh",
                ["CO2 (kg)"] = "kg"
            },
            Notes: []);

    private EngineeringReportTable BuildSystemEnergyEndUseTable(
        IReadOnlyList<SystemEnergyEndUseSummary> endUses) =>
        new(
            TableId: "system-energy-by-end-use",
            Title: "Final/primary/CO2 by end use",
            Columns: ["End use", "Final (kWh)", "Primary total (kWh)", "CO2 (kg)"],
            Rows: endUses
                .OrderBy(item => item.EndUse)
                .Select(item => (IReadOnlyList<string>)
                [
                    item.EndUse.ToString(),
                    _formatting.FormatFixed2(item.AnnualFinalEnergyKWh),
                    _formatting.FormatFixed2(item.AnnualTotalPrimaryEnergyKWh),
                    _formatting.FormatNullableFixed2(item.AnnualEmissionsKg)
                ])
                .ToArray(),
            Units: new Dictionary<string, string>
            {
                ["Final (kWh)"] = "kWh",
                ["Primary total (kWh)"] = "kWh",
                ["CO2 (kg)"] = "kg"
            },
            Notes: []);
}
