using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal sealed class EngineeringReportSectionBuilder
{
    private readonly IEngineeringReportDiagnosticAggregator _diagnosticAggregator;
    private readonly EngineeringReportFormattingService _formatting;

    public EngineeringReportSectionBuilder(
        IEngineeringReportDiagnosticAggregator diagnosticAggregator,
        EngineeringReportFormattingService formatting)
    {
        _diagnosticAggregator = diagnosticAggregator;
        _formatting = formatting;
    }

    public EngineeringReportSection BuildExecutiveSummarySection(
        EngineeringReportGenerationRequest request,
        int order)
    {
        var availableModules = GetAvailableModules(request);
        return new EngineeringReportSection(
            SectionId: "executive-summary",
            SectionKind: EngineeringReportSectionKind.ExecutiveSummary,
            Title: "Executive Summary",
            Order: order,
            SummaryText: "Internal engineering implementation summary for deterministic report generation.",
            KeyValues:
            [
                new EngineeringReportValue("report_kind", "Report kind", request.ReportKind.ToString()),
                new EngineeringReportValue("requested_format", "Requested format", request.RequestedFormat.ToString()),
                new EngineeringReportValue("detail_level", "Detail level", request.DetailLevel.ToString()),
                new EngineeringReportValue("available_modules", "Available modules", string.Join(", ", availableModules)),
                new EngineeringReportValue("module_count", "Available module count", availableModules.Count)
            ],
            Tables: [],
            ChartPlaceholders: [],
            Diagnostics: [],
            Assumptions: [],
            ChildSections: []);
    }

    public EngineeringReportSection BuildInputSummarySection(
        EngineeringReportGenerationRequest request,
        int order) =>
        new(
            SectionId: "input-summary",
            SectionKind: EngineeringReportSectionKind.InputSummary,
            Title: "Input Summary",
            Order: order,
            SummaryText: "Provided calculation summaries and identifiers.",
            KeyValues:
            [
                new EngineeringReportValue("project_id", "Project id", request.ProjectId ?? "n/a"),
                new EngineeringReportValue("building_id", "Building id", request.BuildingId ?? "n/a"),
                new EngineeringReportValue("source_calculation_count", "Source calculation count", request.SourceCalculationIds?.Count ?? 0),
                new EngineeringReportValue("trace_provided", "Calculation trace provided", request.CalculationTrace is not null)
            ],
            Tables: [],
            ChartPlaceholders: [],
            Diagnostics: [],
            Assumptions: [],
            ChildSections: []);

    public void BuildWeatherAndSolarSection(
        EngineeringReportGenerationRequest request,
        ICollection<EngineeringReportSection> sections,
        ICollection<EngineeringReportDiagnostic> diagnostics,
        ref int order)
    {
        if (!EngineeringReportSectionSelectionPolicy.ShouldIncludeSection(
                request.ReportKind,
                request.CalculationTrace is not null && EngineeringReportSectionSelectionPolicy.HasTraceModule(request.CalculationTrace, CalculationTraceModuleKind.Weather, CalculationTraceModuleKind.Solar),
                diagnostics,
                "weather-solar",
                CalculationTraceModuleKind.Weather,
                request.DetailLevel))
            return;

        sections.Add(new EngineeringReportSection(
            SectionId: "weather-solar",
            SectionKind: EngineeringReportSectionKind.WeatherAndSolar,
            Title: "Weather and Solar",
            Order: ++order,
            SummaryText: "Weather source and solar context from provided trace.",
            KeyValues:
            [
                new EngineeringReportValue("trace_weather_steps", "Weather steps", EngineeringReportSectionSelectionPolicy.CountTraceSteps(request.CalculationTrace, CalculationTraceModuleKind.Weather)),
                new EngineeringReportValue("trace_solar_steps", "Solar steps", EngineeringReportSectionSelectionPolicy.CountTraceSteps(request.CalculationTrace, CalculationTraceModuleKind.Solar))
            ],
            Tables: [],
            ChartPlaceholders: [],
            Diagnostics: [],
            Assumptions: [],
            ChildSections: []));
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

    public EngineeringReportSection BuildAssumptionsSection(
        IEnumerable<string> assumptions,
        int order) =>
        new(
            SectionId: "assumptions",
            SectionKind: EngineeringReportSectionKind.Assumptions,
            Title: "Assumptions",
            Order: order,
            SummaryText: "Aggregated assumptions used across provided calculation summaries.",
            KeyValues: assumptions
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(item => item, StringComparer.Ordinal)
                .Select((item, index) => new EngineeringReportValue($"assumption_{index + 1:000}", $"Assumption {index + 1}", item))
                .ToArray(),
            Tables: [],
            ChartPlaceholders: [],
            Diagnostics: [],
            Assumptions: [],
            ChildSections: []);

    public EngineeringReportSection BuildWarningsSection(
        IEnumerable<string> warnings,
        int order) =>
        new(
            SectionId: "warnings",
            SectionKind: EngineeringReportSectionKind.Warnings,
            Title: "Warnings",
            Order: order,
            SummaryText: "Aggregated warnings from calculations and trace.",
            KeyValues: warnings
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(item => item, StringComparer.Ordinal)
                .Select((item, index) => new EngineeringReportValue($"warning_{index + 1:000}", $"Warning {index + 1}", item))
                .ToArray(),
            Tables: [],
            ChartPlaceholders: [],
            Diagnostics: [],
            Assumptions: [],
            ChildSections: []);

    public EngineeringReportSection BuildLimitationsSection(
        int order) =>
        new(
            SectionId: "limitations",
            SectionKind: EngineeringReportSectionKind.Limitations,
            Title: "Known Limitations",
            Order: order,
            SummaryText: "Current report generation boundaries for internal engineering usage.",
            KeyValues:
            [
                new EngineeringReportValue("limitation_001", "Limitation", "Reports summarize current internal engineering calculations only."),
                new EngineeringReportValue("limitation_002", "Limitation", "Report is not a legal compliance certificate."),
                new EngineeringReportValue("limitation_003", "Limitation", "Report is not external validation evidence."),
                new EngineeringReportValue("limitation_004", "Limitation", "Report does not prove full standard compliance."),
                new EngineeringReportValue("limitation_005", "Limitation", "PDF/HTML production rendering is not the focus of this stage."),
                new EngineeringReportValue("limitation_006", "Limitation", "Charts are placeholders unless explicitly implemented.")
            ],
            Tables: [],
            ChartPlaceholders: [],
            Diagnostics: [],
            Assumptions: [],
            ChildSections: []);

    public EngineeringReportSection BuildMetadataSection(
        EngineeringReportGenerationRequest request,
        DateTimeOffset generatedAt,
        string schemaVersion,
        int order)
    {
        var metadataRows = (request.Metadata ?? new Dictionary<string, string>())
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .Select(item => new EngineeringReportValue(
                Key: item.Key,
                Label: item.Key,
                Value: item.Value))
            .ToList();

        metadataRows.Add(new EngineeringReportValue("generated_utc", "Generated UTC", _formatting.FormatIsoUtc(generatedAt)));
        metadataRows.Add(new EngineeringReportValue("schema_version", "Schema version", schemaVersion));

        return new EngineeringReportSection(
            SectionId: "metadata",
            SectionKind: EngineeringReportSectionKind.Metadata,
            Title: "Metadata",
            Order: order,
            SummaryText: "Deterministic metadata included in report payload.",
            KeyValues: metadataRows,
            Tables: [],
            ChartPlaceholders: [],
            Diagnostics: [],
            Assumptions: [],
            ChildSections: []);
    }

    public void AddAssumptionsAndWarnings(
        EngineeringReportGenerationRequest request,
        ICollection<string> assumptions,
        ICollection<string> warnings)
    {
        assumptions.AddRange(request.Assumptions ?? []);
        warnings.AddRange(request.Warnings ?? []);

        if (request.HeatingCoolingSummary is not null)
            assumptions.AddRange(request.HeatingCoolingSummary.Assumptions);

        if (request.CalculationTrace is not null)
        {
            assumptions.AddRange(request.CalculationTrace.Assumptions);
            warnings.AddRange(request.CalculationTrace.Warnings);
        }
    }

    public void AddSourceSpecificAssumptions(
        EngineeringReportGenerationRequest request,
        ICollection<string> assumptions)
    {
        if (request.SystemEnergySummary is not null)
            assumptions.AddRange(request.SystemEnergySummary.DisclosureSummary.Assumptions);
    }

    private IReadOnlyList<string> GetAvailableModules(
        EngineeringReportGenerationRequest request)
    {
        var modules = new SortedSet<string>(StringComparer.Ordinal)
        {
            "Reporting"
        };

        if (request.HeatingCoolingSummary is not null)
            modules.Add("Iso52016");

        if (request.MultiZoneSummary is not null)
            modules.Add("MultiZone");

        if (request.NaturalVentilationSummary is not null)
            modules.Add("Ventilation");

        if (request.GroundSummary is not null)
            modules.Add("Ground");

        if (request.DomesticHotWaterSummary is not null)
            modules.Add("DomesticHotWater");

        if (request.SystemEnergySummary is not null)
            modules.Add("SystemEnergy");

        if (request.CalculationTrace is not null)
        {
            foreach (var module in request.CalculationTrace.Summary.Modules.Select(item => item.ToString()))
                modules.Add(module);
        }

        return modules.ToArray();
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
