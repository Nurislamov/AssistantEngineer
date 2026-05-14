using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal sealed class EngineeringReportLoadResultsSectionBuilder
{
    private readonly EngineeringReportFormattingService _formatting;

    public EngineeringReportLoadResultsSectionBuilder(
        EngineeringReportFormattingService formatting)
    {
        _formatting = formatting;
    }

    public void BuildThermalZonesSection(
        EngineeringReportGenerationRequest request,
        ICollection<EngineeringReportSection> sections,
        ICollection<EngineeringReportDiagnostic> diagnostics,
        ref int order)
    {
        var hasData = request.MultiZoneSummary is not null ||
                      (request.CalculationTrace is not null && EngineeringReportSectionSelectionPolicy.HasTraceModule(request.CalculationTrace, CalculationTraceModuleKind.ThermalTopology));

        if (!EngineeringReportSectionSelectionPolicy.ShouldIncludeSection(
                request.ReportKind,
                hasData,
                diagnostics,
                "thermal-zones",
                CalculationTraceModuleKind.ThermalTopology,
                request.DetailLevel))
            return;

        sections.Add(new EngineeringReportSection(
            SectionId: "thermal-zones",
            SectionKind: EngineeringReportSectionKind.ThermalZones,
            Title: "Thermal Zones",
            Order: ++order,
            SummaryText: "Thermal topology and zone-level summary.",
            KeyValues:
            [
                new EngineeringReportValue("zone_count", "Zone count", request.MultiZoneSummary?.AnnualHeatingEnergyByZoneKWh.Count ?? 0),
                new EngineeringReportValue("trace_topology_steps", "Topology trace steps", EngineeringReportSectionSelectionPolicy.CountTraceSteps(request.CalculationTrace, CalculationTraceModuleKind.ThermalTopology))
            ],
            Tables: BuildZoneSummaryTables(request.MultiZoneSummary),
            ChartPlaceholders: [],
            Diagnostics: [],
            Assumptions: [],
            ChildSections: []));
    }

    public void BuildHeatingCoolingSection(
        EngineeringReportGenerationRequest request,
        ICollection<EngineeringReportSection> sections,
        ICollection<string> assumptions,
        ICollection<EngineeringReportDiagnostic> diagnostics,
        ICollection<EngineeringReportValue> summaries,
        ref int order)
    {
        if (!EngineeringReportSectionSelectionPolicy.ShouldIncludeSection(
                request.ReportKind,
                request.HeatingCoolingSummary is not null,
                diagnostics,
                "heating-cooling",
                CalculationTraceModuleKind.Iso52016,
                request.DetailLevel))
            return;

        var summary = request.HeatingCoolingSummary!;
        assumptions.AddRange(summary.Assumptions);

        summaries.Add(new EngineeringReportValue("annual_heating_kwh", "Annual heating need", summary.AnnualHeatingDemandKWh, new EngineeringReportUnit("kWh")));
        summaries.Add(new EngineeringReportValue("annual_cooling_kwh", "Annual cooling need", summary.AnnualCoolingDemandKWh, new EngineeringReportUnit("kWh")));

        sections.Add(new EngineeringReportSection(
            SectionId: "heating-cooling-loads",
            SectionKind: EngineeringReportSectionKind.HeatingCoolingLoads,
            Title: "Heating and Cooling Loads",
            Order: ++order,
            SummaryText: "Building annual heating/cooling demand and peak load summary.",
            KeyValues:
            [
                new EngineeringReportValue("annual_heating_kwh", "Annual heating demand", summary.AnnualHeatingDemandKWh, new EngineeringReportUnit("kWh")),
                new EngineeringReportValue("annual_cooling_kwh", "Annual cooling demand", summary.AnnualCoolingDemandKWh, new EngineeringReportUnit("kWh")),
                new EngineeringReportValue("annual_total_kwh", "Annual total demand", summary.AnnualTotalDemandKWh, new EngineeringReportUnit("kWh")),
                new EngineeringReportValue("peak_heating_w", "Peak heating", summary.PeakHeatingW, new EngineeringReportUnit("W")),
                new EngineeringReportValue("peak_cooling_w", "Peak cooling", summary.PeakCoolingW, new EngineeringReportUnit("W")),
                new EngineeringReportValue("energy_data_source", "Energy data source", summary.EnergyDataSource),
                new EngineeringReportValue("hourly_record_count", "Hourly record count", summary.HourlyRecordCount)
            ],
            Tables: BuildHeatingCoolingTables(summary),
            ChartPlaceholders: [],
            Diagnostics: [],
            Assumptions: [],
            ChildSections: []));
    }

    public void BuildNaturalVentilationSection(
        EngineeringReportGenerationRequest request,
        ICollection<EngineeringReportSection> sections,
        ICollection<EngineeringReportDiagnostic> diagnostics,
        ICollection<EngineeringReportValue> summaries,
        ref int order)
    {
        if (!EngineeringReportSectionSelectionPolicy.ShouldIncludeSection(
                request.ReportKind,
                request.NaturalVentilationSummary is not null,
                diagnostics,
                "natural-ventilation",
                CalculationTraceModuleKind.Ventilation,
                request.DetailLevel))
            return;

        var summary = request.NaturalVentilationSummary!;
        summaries.Add(new EngineeringReportValue("natural_ventilation_airflow_m3h", "Natural ventilation airflow", summary.TotalAirflowCubicMetersPerHour, new EngineeringReportUnit("m3/h")));

        sections.Add(new EngineeringReportSection(
            SectionId: "natural-ventilation",
            SectionKind: EngineeringReportSectionKind.NaturalVentilation,
            Title: "Natural Ventilation",
            Order: ++order,
            SummaryText: "Opening-level airflow and control mode summary.",
            KeyValues:
            [
                new EngineeringReportValue("opening_count", "Opening count", summary.Openings.Count),
                new EngineeringReportValue("flow_configuration", "Flow configuration", summary.FlowConfiguration.ToString()),
                new EngineeringReportValue("total_airflow_m3h", "Total airflow", summary.TotalAirflowCubicMetersPerHour, new EngineeringReportUnit("m3/h")),
                new EngineeringReportValue("total_airflow_kgs", "Total airflow", summary.TotalAirflowKilogramsPerSecond, new EngineeringReportUnit("kg/s"))
            ],
            Tables: [],
            ChartPlaceholders: [],
            Diagnostics: [],
            Assumptions: [],
            ChildSections: []));
    }

    public void BuildGroundSection(
        EngineeringReportGenerationRequest request,
        ICollection<EngineeringReportSection> sections,
        ICollection<EngineeringReportDiagnostic> diagnostics,
        ICollection<EngineeringReportValue> summaries,
        ref int order)
    {
        if (!EngineeringReportSectionSelectionPolicy.ShouldIncludeSection(
                request.ReportKind,
                request.GroundSummary is not null,
                diagnostics,
                "ground-boundaries",
                CalculationTraceModuleKind.Ground,
                request.DetailLevel))
            return;

        var summary = request.GroundSummary!;
        summaries.Add(new EngineeringReportValue("ground_h_total_w_per_k", "Total H_ground", summary.TotalGroundHeatTransferCoefficientWPerKelvin, new EngineeringReportUnit("W/K")));

        sections.Add(new EngineeringReportSection(
            SectionId: "ground-boundaries",
            SectionKind: EngineeringReportSectionKind.GroundBoundaries,
            Title: "Ground Boundaries",
            Order: ++order,
            SummaryText: "Ground boundary coefficients and temperature profile availability.",
            KeyValues:
            [
                new EngineeringReportValue("ground_surface_count", "Ground surface count", summary.GroundSurfaces.Count),
                new EngineeringReportValue("h_ground_total", "Total H_ground", summary.TotalGroundHeatTransferCoefficientWPerKelvin, new EngineeringReportUnit("W/K")),
                new EngineeringReportValue("hourly_profile_count", "Hourly profile count", summary.SurfaceHourlyGroundTemperaturesCelsius.Count),
                new EngineeringReportValue("monthly_profile_count", "Monthly profile count", summary.SurfaceMonthlyGroundTemperaturesCelsius.Count)
            ],
            Tables: [],
            ChartPlaceholders: [],
            Diagnostics: [],
            Assumptions: [],
            ChildSections: []));
    }

    public void BuildDomesticHotWaterSection(
        EngineeringReportGenerationRequest request,
        ICollection<EngineeringReportSection> sections,
        ICollection<EngineeringReportDiagnostic> diagnostics,
        ICollection<EngineeringReportValue> summaries,
        ref int order)
    {
        if (!EngineeringReportSectionSelectionPolicy.ShouldIncludeSection(
                request.ReportKind,
                request.DomesticHotWaterSummary is not null,
                diagnostics,
                "domestic-hot-water",
                CalculationTraceModuleKind.DomesticHotWater,
                request.DetailLevel))
            return;

        var summary = request.DomesticHotWaterSummary!;
        var annual = new DomesticHotWaterSystemLoadAnnualSummary(
            summary.AnnualUsefulEnergyKWh,
            summary.AnnualStorageLossKWh,
            summary.AnnualDistributionLossKWh,
            summary.AnnualCirculationLossKWh,
            summary.AnnualRecoverableLossKWh,
            summary.AnnualAuxiliaryElectricityKWh,
            summary.AnnualSystemHeatRequirementKWh);

        summaries.Add(new EngineeringReportValue("dhw_useful_kwh", "DHW useful demand", annual.UsefulEnergyKWh, new EngineeringReportUnit("kWh")));
        summaries.Add(new EngineeringReportValue("dhw_system_load_kwh", "DHW system load", annual.SystemLoadKWh, new EngineeringReportUnit("kWh")));

        sections.Add(new EngineeringReportSection(
            SectionId: "domestic-hot-water",
            SectionKind: EngineeringReportSectionKind.DomesticHotWater,
            Title: "Domestic Hot Water",
            Order: ++order,
            SummaryText: "Useful demand, losses, recovered losses and auxiliary demand.",
            KeyValues:
            [
                new EngineeringReportValue("annual_useful_kwh", "Annual useful demand", annual.UsefulEnergyKWh, new EngineeringReportUnit("kWh")),
                new EngineeringReportValue("annual_storage_losses_kwh", "Storage losses", annual.StorageLossesKWh, new EngineeringReportUnit("kWh")),
                new EngineeringReportValue("annual_distribution_losses_kwh", "Distribution losses", annual.DistributionLossesKWh, new EngineeringReportUnit("kWh")),
                new EngineeringReportValue("annual_circulation_losses_kwh", "Circulation losses", annual.CirculationLossesKWh, new EngineeringReportUnit("kWh")),
                new EngineeringReportValue("annual_recovered_losses_kwh", "Recovered losses", annual.RecoveredLossesKWh, new EngineeringReportUnit("kWh")),
                new EngineeringReportValue("annual_auxiliary_kwh", "Auxiliary energy", annual.AuxiliaryEnergyKWh, new EngineeringReportUnit("kWh")),
                new EngineeringReportValue("annual_system_load_kwh", "System load", annual.SystemLoadKWh, new EngineeringReportUnit("kWh"))
            ],
            Tables:
            [
                new EngineeringReportTable(
                    TableId: "dhw-annual-summary",
                    Title: "DHW annual summary",
                    Columns: ["Metric", "Value"],
                    Rows:
                    [
                        ["Useful demand", _formatting.FormatFixed2(annual.UsefulEnergyKWh)],
                        ["System load", _formatting.FormatFixed2(annual.SystemLoadKWh)],
                        ["Auxiliary", _formatting.FormatFixed2(annual.AuxiliaryEnergyKWh)]
                    ],
                    Units: new Dictionary<string, string> { ["Value"] = "kWh" },
                    Notes: [])
            ],
            ChartPlaceholders: [],
            Diagnostics: [],
            Assumptions: [],
            ChildSections: []));
    }

    private IReadOnlyList<EngineeringReportTable> BuildZoneSummaryTables(
        MultiZoneAnnualSummary? summary)
    {
        if (summary is null)
            return [];

        var zoneRows = summary.AnnualHeatingEnergyByZoneKWh.Keys
            .Union(summary.AnnualCoolingEnergyByZoneKWh.Keys, StringComparer.Ordinal)
            .OrderBy(item => item, StringComparer.Ordinal)
            .Select(zoneId => (IReadOnlyList<string>)
            [
                zoneId,
                summary.AnnualHeatingEnergyByZoneKWh.TryGetValue(zoneId, out var heating) ? _formatting.FormatFixed2(heating) : "0.00",
                summary.AnnualCoolingEnergyByZoneKWh.TryGetValue(zoneId, out var cooling) ? _formatting.FormatFixed2(cooling) : "0.00"
            ])
            .ToArray();

        return
        [
            new EngineeringReportTable(
                TableId: "zone-annual-loads",
                Title: "Zone annual loads",
                Columns: ["Zone", "Heating (kWh)", "Cooling (kWh)"],
                Rows: zoneRows,
                Units: new Dictionary<string, string>
                {
                    ["Heating (kWh)"] = "kWh",
                    ["Cooling (kWh)"] = "kWh"
                },
                Notes: [])
        ];
    }

    private IReadOnlyList<EngineeringReportTable> BuildHeatingCoolingTables(
        BuildingEnergyBalanceResult summary)
    {
        var monthlyRows = summary.MonthlyBalances
            .OrderBy(item => item.Month)
            .Select(item => (IReadOnlyList<string>)
            [
                item.Month.ToString(),
                _formatting.FormatFixed2(item.HeatingDemandKWh),
                _formatting.FormatFixed2(item.CoolingDemandKWh),
                _formatting.FormatFixed2(item.HeatingDemandKWh + item.CoolingDemandKWh)
            ])
            .ToArray();

        var tables = new List<EngineeringReportTable>
        {
            new(
                TableId: "monthly-heating-cooling",
                Title: "Monthly heating/cooling demand",
                Columns: ["Month", "Heating (kWh)", "Cooling (kWh)", "Total (kWh)"],
                Rows: monthlyRows,
                Units: new Dictionary<string, string>
                {
                    ["Heating (kWh)"] = "kWh",
                    ["Cooling (kWh)"] = "kWh",
                    ["Total (kWh)"] = "kWh"
                },
                Notes: [])
        };

        if (summary.ComponentBreakdown is { } breakdown)
        {
            tables.Add(new EngineeringReportTable(
                TableId: "annual-component-breakdown",
                Title: "Annual component breakdown",
                Columns: ["Component", "Energy (kWh)"],
                Rows:
                [
                    ["Transmission", _formatting.FormatFixed2(breakdown.TransmissionKWh)],
                    ["Ventilation", _formatting.FormatFixed2(breakdown.VentilationKWh)],
                    ["Infiltration", _formatting.FormatFixed2(breakdown.InfiltrationKWh)],
                    ["Solar gains", _formatting.FormatFixed2(breakdown.SolarGainsKWh)],
                    ["Internal gains", _formatting.FormatFixed2(breakdown.InternalGainsKWh)],
                    ["Ground", _formatting.FormatFixed2(breakdown.GroundKWh)]
                ],
                Units: new Dictionary<string, string> { ["Energy (kWh)"] = "kWh" },
                Notes: []));
        }

        return tables;
    }
}
