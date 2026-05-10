using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Topology;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Trace;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;
using System.Diagnostics;

namespace AssistantEngineer.Api.Services.Calculations;

public sealed class EngineeringCalculationScenarioRunner : IEngineeringCalculationScenarioRunner
{
    private readonly ILoadCalculationsFacade _loadCalculations;
    private readonly IThermalTopologyBuilder _thermalTopologyBuilder;
    private readonly IThermalTopologyValidator _thermalTopologyValidator;
    private readonly IDomesticHotWaterSystemLoadCalculator _domesticHotWaterSystemLoadCalculator;
    private readonly ISystemEnergyFoundationCalculator _systemEnergyFoundationCalculator;
    private readonly ICalculationTraceBuilder _traceBuilder;
    private readonly ICalculationTraceSanitizer _traceSanitizer;
    private readonly IEngineeringReportBuilder _reportBuilder;
    private readonly IEngineeringReportJsonExporter _reportJsonExporter;
    private readonly IEngineeringReportMarkdownExporter _reportMarkdownExporter;

    public EngineeringCalculationScenarioRunner(
        ILoadCalculationsFacade loadCalculations,
        IThermalTopologyBuilder thermalTopologyBuilder,
        IThermalTopologyValidator thermalTopologyValidator,
        IDomesticHotWaterSystemLoadCalculator domesticHotWaterSystemLoadCalculator,
        ISystemEnergyFoundationCalculator systemEnergyFoundationCalculator,
        ICalculationTraceBuilder traceBuilder,
        ICalculationTraceSanitizer traceSanitizer,
        IEngineeringReportBuilder reportBuilder,
        IEngineeringReportJsonExporter reportJsonExporter,
        IEngineeringReportMarkdownExporter reportMarkdownExporter)
    {
        _loadCalculations = loadCalculations;
        _thermalTopologyBuilder = thermalTopologyBuilder;
        _thermalTopologyValidator = thermalTopologyValidator;
        _domesticHotWaterSystemLoadCalculator = domesticHotWaterSystemLoadCalculator;
        _systemEnergyFoundationCalculator = systemEnergyFoundationCalculator;
        _traceBuilder = traceBuilder;
        _traceSanitizer = traceSanitizer;
        _reportBuilder = reportBuilder;
        _reportJsonExporter = reportJsonExporter;
        _reportMarkdownExporter = reportMarkdownExporter;
    }

    public async Task<EngineeringCalculationScenarioResultDto> RunAsync(
        EngineeringCalculationScenarioRequestDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.State);

        var diagnostics = new List<EngineeringWorkflowDiagnosticDto>();
        diagnostics.AddRange(request.State.Diagnostics);

        var assumptions = new List<string>(request.State.Assumptions);
        var warnings = new List<string>(request.State.Diagnostics
            .Where(item => item.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
            .Select(item => item.Message));

        var moduleResults = new List<EngineeringCalculationModuleExecutionResultDto>();
        var timings = new List<EngineeringCalculationModuleTimingDto>();
        var executedModules = new List<string>();
        var skippedModules = new List<string>();
        var unavailableModules = new List<string>();

        BuildingThermalTopology? topology = null;
        BuildingEnergyBalanceResult? heatingCoolingSummary = null;
        DomesticHotWaterSystemLoadFoundationResult? dhwFoundationSummary = null;
        SystemEnergyCalculationResult? systemEnergySummary = null;

        diagnostics.AddRange(ValidateScenarioRequest(request));
        diagnostics = SortAndDistinctDiagnostics(diagnostics).ToList();

        if (request.ExecutionMode == EngineeringCalculationExecutionMode.ValidateOnly)
        {
            assumptions.Add("Execution mode ValidateOnly returns deterministic diagnostics without module execution.");
            return BuildScenarioResult(
                request,
                moduleResults,
                timings,
                executedModules,
                skippedModules,
                unavailableModules,
                diagnostics,
                assumptions,
                warnings,
                topologySummary: null,
                ventilationSummary: "Not executed in ValidateOnly mode.",
                groundSummary: "Not executed in ValidateOnly mode.",
                heatingCoolingSummaryText: "Not executed in ValidateOnly mode.",
                dhwSummary: "Not executed in ValidateOnly mode.",
                systemEnergySummaryText: "Not executed in ValidateOnly mode.",
                heatingCoolingResult: null,
                calculationTrace: request.IncludeTrace ? BuildTrace(request, moduleResults, diagnostics, assumptions, warnings) : null,
                includeReport: request.IncludeReport,
                reportFormats: request.ReportFormats);
        }

        if (request.ExecutionMode == EngineeringCalculationExecutionMode.PrepareOnly)
        {
            assumptions.Add("Execution mode PrepareOnly does not execute calculation modules.");
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "info",
                Code: "SCENARIO_PREPARE_ONLY",
                Message: "Scenario request is prepared and validated without module execution.",
                SourceStep: "Review"));

            return BuildScenarioResult(
                request,
                moduleResults,
                timings,
                executedModules,
                skippedModules,
                unavailableModules,
                SortAndDistinctDiagnostics(diagnostics),
                assumptions,
                warnings,
                topologySummary: "Prepared only.",
                ventilationSummary: "Prepared only.",
                groundSummary: "Prepared only.",
                heatingCoolingSummaryText: "Prepared only.",
                dhwSummary: "Prepared only.",
                systemEnergySummaryText: "Prepared only.",
                heatingCoolingResult: null,
                calculationTrace: request.IncludeTrace ? BuildTrace(request, moduleResults, diagnostics, assumptions, warnings) : null,
                includeReport: request.IncludeReport,
                reportFormats: request.ReportFormats);
        }

        if (request.ExecutionMode == EngineeringCalculationExecutionMode.DryRun)
        {
            assumptions.Add("Execution mode DryRun returns deterministic execution plan without invoking calculators.");
            return BuildScenarioResult(
                request,
                moduleResults,
                timings,
                executedModules,
                skippedModules,
                unavailableModules,
                SortAndDistinctDiagnostics(diagnostics),
                assumptions,
                warnings,
                topologySummary: "DryRun plan only.",
                ventilationSummary: "DryRun plan only.",
                groundSummary: "DryRun plan only.",
                heatingCoolingSummaryText: "DryRun plan only.",
                dhwSummary: "DryRun plan only.",
                systemEnergySummaryText: "DryRun plan only.",
                heatingCoolingResult: null,
                calculationTrace: request.IncludeTrace ? BuildTrace(request, moduleResults, diagnostics, assumptions, warnings) : null,
                includeReport: request.IncludeReport,
                reportFormats: request.ReportFormats);
        }

        if (request.ExecutionMode == EngineeringCalculationExecutionMode.ExecuteFullRequired &&
            diagnostics.Any(item => IsError(item.Severity)))
        {
            assumptions.Add("Execution mode ExecuteFullRequired stops when critical validation diagnostics are present.");

            return BuildScenarioResult(
                request,
                moduleResults,
                timings,
                executedModules,
                skippedModules,
                unavailableModules,
                SortAndDistinctDiagnostics(diagnostics),
                assumptions,
                warnings,
                topologySummary: null,
                ventilationSummary: null,
                groundSummary: null,
                heatingCoolingSummaryText: null,
                dhwSummary: null,
                systemEnergySummaryText: null,
                heatingCoolingResult: null,
                calculationTrace: request.IncludeTrace ? BuildTrace(request, moduleResults, diagnostics, assumptions, warnings) : null,
                includeReport: request.IncludeReport,
                reportFormats: request.ReportFormats);
        }

        var modeAllowsPartial = request.ExecutionMode == EngineeringCalculationExecutionMode.ExecuteAvailableModules;

        var topologyRun = ExecuteModule(
            "ThermalTopology",
            "Thermal topology normalization",
            () =>
            {
                if (request.State.Zones.Count == 0 || request.State.Boundaries.Count == 0)
                {
                    return ModuleExecution.Skip(
                        "Thermal topology requires zones and boundaries.",
                        "Configure at least one zone and one envelope boundary.");
                }

                var result = BuildThermalTopology(request.State);
                topology = result.Topology;
                assumptions.AddRange(result.Assumptions);
                warnings.AddRange(result.Warnings);
                diagnostics.AddRange(result.Diagnostics);
                return ModuleExecution.Execute(result.Values, "IThermalTopologyBuilder");
            });
        AddModule(topologyRun, moduleResults, timings, executedModules, skippedModules, unavailableModules);

        var weatherRun = ExecuteModule(
            "WeatherSolar",
            "Weather and solar readiness",
            () =>
            {
                var weatherStatus = request.State.WeatherSolarSettings.WeatherSourceStatus ?? "Unavailable";
                if (weatherStatus.Equals("Unavailable", StringComparison.OrdinalIgnoreCase) ||
                    weatherStatus.Equals("n/a", StringComparison.OrdinalIgnoreCase))
                {
                    return ModuleExecution.Skip(
                        "Weather and solar readiness data is unavailable.",
                        "Provide weather/solar readiness input to execute dependent modules.");
                }

                return ModuleExecution.Execute(
                [
                    new EngineeringCalculationModuleValueDto("weather_status", "Weather source status", weatherStatus),
                    new EngineeringCalculationModuleValueDto("timezone_summary", "Location/timezone summary", request.State.WeatherSolarSettings.LocationTimezoneSummary),
                    new EngineeringCalculationModuleValueDto("solar_readiness", "Solar chain readiness", request.State.WeatherSolarSettings.SolarChainReadinessSummary)
                ], "WorkflowState.WeatherSolarSettings");
            });
        AddModule(weatherRun, moduleResults, timings, executedModules, skippedModules, unavailableModules);

        var ventilationRun = ExecuteModule(
            "Ventilation",
            "Natural ventilation execution",
            () =>
            {
                if (request.State.VentilationSettings.OpeningCount <= 0)
                {
                    return ModuleExecution.Skip(
                        "No natural ventilation openings are configured.",
                        "Configure natural ventilation openings to execute this module.");
                }

                return ModuleExecution.Skip(
                    "Structured natural ventilation hourly input is not available in workflow state foundation payload.",
                    "Provide detailed ventilation opening geometry, control rules and hourly environments.");
            });
        AddModule(ventilationRun, moduleResults, timings, executedModules, skippedModules, unavailableModules);

        var groundRun = ExecuteModule(
            "Ground",
            "Ground boundary execution",
            () =>
            {
                if (request.State.GroundSettings.GroundBoundaryCount <= 0)
                {
                    return ModuleExecution.Skip(
                        "No ground boundaries are configured.",
                        "Configure ground boundaries to execute this module.");
                }

                return ModuleExecution.Skip(
                    "Structured ground boundary geometry and climate inputs are not available in workflow state foundation payload.",
                    "Provide detailed ground inputs to execute this module.");
            });
        AddModule(groundRun, moduleResults, timings, executedModules, skippedModules, unavailableModules);

        var heatingRun = await ExecuteModuleAsync(
            "HeatingCooling",
            "ISO52016/MultiZone heating-cooling load",
            async () =>
            {
                if (!request.State.BuildingId.HasValue || request.State.BuildingId <= 0)
                {
                    return ModuleExecution.Skip(
                        "Building id is not available for heating/cooling calculation.",
                        "Select a valid building id.");
                }

                var result = await _loadCalculations.CalculateBuildingEnergyBalanceAsync(
                    request.State.BuildingId.Value,
                    CoolingLoadCalculationMethodDto.Iso52016,
                    HeatingLoadCalculationMethodDto.En12831,
                    cancellationToken);

                if (result.IsFailure)
                {
                    return modeAllowsPartial
                        ? ModuleExecution.Skip(
                            $"Heating/cooling module was not executed: {result.Error}",
                            "Ensure building model and climate data are available for load calculation.")
                        : ModuleExecution.Fail(
                            $"Heating/cooling module failed: {result.Error}",
                            "ExecuteFullRequired mode requires successful heating/cooling execution.");
                }

                heatingCoolingSummary = result.Value;
                assumptions.AddRange(heatingCoolingSummary.Assumptions);
                diagnostics.AddRange(heatingCoolingSummary.Diagnostics.Select(item =>
                    FromCalculationDiagnostic(item, "Validation", "Iso52016")));

                return ModuleExecution.Execute(
                [
                    new EngineeringCalculationModuleValueDto("annual_heating_kwh", "Annual heating demand", heatingCoolingSummary.AnnualHeatingDemandKWh, "kWh"),
                    new EngineeringCalculationModuleValueDto("annual_cooling_kwh", "Annual cooling demand", heatingCoolingSummary.AnnualCoolingDemandKWh, "kWh"),
                    new EngineeringCalculationModuleValueDto("annual_total_kwh", "Annual total demand", heatingCoolingSummary.AnnualTotalDemandKWh, "kWh")
                ], "ILoadCalculationsFacade");
            });
        AddModule(heatingRun, moduleResults, timings, executedModules, skippedModules, unavailableModules);

        var dhwRun = ExecuteModule(
            "DomesticHotWater",
            "Domestic hot water execution",
            () =>
            {
                var annualUseful = ResolveAnnualValue(request.State.Metadata, "dhw.useful_annual_kwh");
                if (!(annualUseful > 0.0))
                {
                    return ModuleExecution.Skip(
                        "DHW annual useful demand is not provided in workflow metadata (`dhw.useful_annual_kwh`).",
                        "Provide structured DHW useful demand metadata to execute DHW module.");
                }

                var usefulProfile = BuildFlatProfile8760(annualUseful.Value);
                var lossDefinition = new DomesticHotWaterLossDefinition(
                    SystemKind: DomesticHotWaterSystemKind.CentralStorage,
                    StorageVolumeLiters: 300,
                    StorageLossCoefficientWPerKelvin: 2.0,
                    StorageAmbientTemperatureCelsius: 20.0,
                    DistributionPipeLengthMeters: 40.0,
                    DistributionLossCoefficientWPerMeterKelvin: 0.12,
                    CirculationOperationSchedule: null,
                    CirculationOperationFraction: 0.6,
                    CirculationLoopLengthMeters: 15.0,
                    CirculationLossCoefficientWPerMeterKelvin: 0.15,
                    RecoveredLossFraction: 0.2,
                    AuxiliaryEnergyProfileKWh: null,
                    AuxiliaryEnergyPerStepKWh: 0.01,
                    LossOwnershipPolicy: DomesticHotWaterLossOwnershipPolicy.DhwOwnLosses,
                    TimeStepHours: 1.0,
                    Source: "EngineeringCalculationScenarioRunner",
                    Diagnostics: []);

                dhwFoundationSummary = _domesticHotWaterSystemLoadCalculator.Calculate(
                    new DomesticHotWaterSystemLoadRequest(
                        UsefulDemandProfileKWh: usefulProfile,
                        LossDefinition: lossDefinition,
                        ColdWaterTemperatureProfileCelsius: null,
                        HotWaterSetpointProfileCelsius: null,
                        TimeStepHours: 1.0));

                assumptions.AddRange(dhwFoundationSummary.Assumptions);
                warnings.AddRange(dhwFoundationSummary.Warnings);
                diagnostics.AddRange(dhwFoundationSummary.Diagnostics.Select(item =>
                    FromStandardDiagnostic(item, "DomesticHotWater", "DhwFoundation")));

                return ModuleExecution.Execute(
                [
                    new EngineeringCalculationModuleValueDto("dhw_annual_useful_kwh", "Annual useful DHW demand", dhwFoundationSummary.AnnualSummary.UsefulEnergyKWh, "kWh"),
                    new EngineeringCalculationModuleValueDto("dhw_annual_system_kwh", "Annual DHW system heat", dhwFoundationSummary.AnnualSummary.SystemLoadKWh, "kWh"),
                    new EngineeringCalculationModuleValueDto("dhw_annual_losses_kwh", "Annual DHW losses", dhwFoundationSummary.AnnualSummary.StorageLossesKWh + dhwFoundationSummary.AnnualSummary.DistributionLossesKWh + dhwFoundationSummary.AnnualSummary.CirculationLossesKWh, "kWh")
                ], "IDomesticHotWaterSystemLoadCalculator");
            });
        AddModule(dhwRun, moduleResults, timings, executedModules, skippedModules, unavailableModules);

        var systemEnergyRun = ExecuteModule(
            "SystemEnergy",
            "System energy execution",
            () =>
            {
                var loads = BuildSystemEnergyLoads(request.State, dhwFoundationSummary);
                if (loads.Count == 0)
                {
                    return ModuleExecution.Skip(
                        "System-energy useful loads are unavailable (metadata keys or DHW handoff missing).",
                        "Provide structured load metadata (`system_energy.*_annual_kwh`) or execute DHW module first.");
                }

                var stageDefinitions = BuildDefaultSystemEnergyStages(loads);
                var generatorDefinitions = BuildDefaultSystemEnergyGenerators(loads);
                var factors = BuildDefaultFactorCatalog();

                systemEnergySummary = _systemEnergyFoundationCalculator.Calculate(
                    new SystemEnergyCalculationRequest(
                        CalculationId: request.ScenarioId,
                        LoadInputs: loads,
                        StageDefinitions: stageDefinitions,
                        GeneratorDefinitions: generatorDefinitions,
                        FactorCatalog: factors,
                        TimeStepHours: 1.0,
                        OutputResolution: SystemEnergyProfileShape.Hourly8760,
                        OwnershipPolicy: SystemEnergyLossOwnershipPolicy.NoDoubleCounting,
                        StrictFactorMode: false));

                assumptions.AddRange(systemEnergySummary.Assumptions);
                warnings.AddRange(systemEnergySummary.Warnings);
                diagnostics.AddRange(systemEnergySummary.Diagnostics.Select(item =>
                    FromStandardDiagnostic(item, "SystemEnergy", "SystemEnergyFoundation")));

                return ModuleExecution.Execute(
                [
                    new EngineeringCalculationModuleValueDto("system_final_kwh", "Annual final energy", systemEnergySummary.AnnualSummary.FinalEnergyKWh, "kWh"),
                    new EngineeringCalculationModuleValueDto("system_primary_kwh", "Annual primary energy", systemEnergySummary.AnnualSummary.PrimaryEnergyKWh, "kWh"),
                    new EngineeringCalculationModuleValueDto("system_co2_kg", "Annual CO2 emissions", systemEnergySummary.AnnualSummary.Co2Kg, "kg")
                ], "ISystemEnergyFoundationCalculator");
            });
        AddModule(systemEnergyRun, moduleResults, timings, executedModules, skippedModules, unavailableModules);

        diagnostics = SortAndDistinctDiagnostics(diagnostics).ToList();
        warnings = warnings
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
        assumptions = assumptions
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        var trace = request.IncludeTrace
            ? BuildTrace(request, moduleResults, diagnostics, assumptions, warnings)
            : null;

        return BuildScenarioResult(
            request,
            moduleResults,
            timings,
            executedModules,
            skippedModules,
            unavailableModules,
            diagnostics,
            assumptions,
            warnings,
            topologySummary: topology is null ? "Skipped." : $"Zones: {topology.Zones.Count}, surfaces: {topology.Surfaces.Count}.",
            ventilationSummary: FindSummary(moduleResults, "Ventilation"),
            groundSummary: FindSummary(moduleResults, "Ground"),
            heatingCoolingSummaryText: heatingCoolingSummary is null
                ? "Skipped."
                : $"Heating {Round(heatingCoolingSummary.AnnualHeatingDemandKWh)} kWh, cooling {Round(heatingCoolingSummary.AnnualCoolingDemandKWh)} kWh.",
            dhwSummary: dhwFoundationSummary is null
                ? "Skipped."
                : $"Useful {Round(dhwFoundationSummary.AnnualSummary.UsefulEnergyKWh)} kWh, system {Round(dhwFoundationSummary.AnnualSummary.SystemLoadKWh)} kWh.",
            systemEnergySummaryText: systemEnergySummary is null
                ? "Skipped."
                : $"Final {Round(systemEnergySummary.AnnualSummary.FinalEnergyKWh)} kWh, primary {Round(systemEnergySummary.AnnualSummary.PrimaryEnergyKWh)} kWh, CO2 {Round(systemEnergySummary.AnnualSummary.Co2Kg)} kg.",
            heatingCoolingResult: heatingCoolingSummary,
            calculationTrace: trace,
            includeReport: request.IncludeReport,
            reportFormats: request.ReportFormats);
    }

    private static IReadOnlyList<EngineeringWorkflowDiagnosticDto> ValidateScenarioRequest(
        EngineeringCalculationScenarioRequestDto request)
    {
        var diagnostics = new List<EngineeringWorkflowDiagnosticDto>();

        if (string.IsNullOrWhiteSpace(request.ScenarioId))
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "error",
                Code: "SCENARIO_ID_MISSING",
                Message: "Scenario id is required.",
                SourceStep: "Review",
                TargetField: "scenarioId"));
        }

        if (request.State.ProjectId <= 0)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "error",
                Code: "SCENARIO_PROJECT_ID_INVALID",
                Message: "Project id must be greater than zero for scenario execution.",
                SourceStep: "Project",
                TargetField: "projectId"));
        }

        if (!request.State.BuildingId.HasValue || request.State.BuildingId <= 0)
        {
            diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                Severity: "warning",
                Code: "SCENARIO_BUILDING_ID_MISSING",
                Message: "Building id is missing; only modules independent from building persistence can run.",
                SourceStep: "Building",
                TargetField: "buildingId"));
        }

        if (request.ExecutionMode == EngineeringCalculationExecutionMode.ExecuteFullRequired)
        {
            if (request.State.Zones.Count == 0)
            {
                diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                    Severity: "error",
                    Code: "SCENARIO_ZONES_REQUIRED",
                    Message: "ExecuteFullRequired mode requires at least one zone.",
                    SourceStep: "Zones"));
            }

            if (request.State.Boundaries.Count == 0)
            {
                diagnostics.Add(new EngineeringWorkflowDiagnosticDto(
                    Severity: "error",
                    Code: "SCENARIO_BOUNDARIES_REQUIRED",
                    Message: "ExecuteFullRequired mode requires at least one boundary.",
                    SourceStep: "Envelope"));
            }
        }

        return SortAndDistinctDiagnostics(diagnostics);
    }

    private static ModuleRunOutcome ExecuteModule(
        string moduleKind,
        string stepName,
        Func<ModuleExecution> execute)
    {
        var stopwatch = Stopwatch.StartNew();
        var outcome = execute();
        stopwatch.Stop();

        return new ModuleRunOutcome(moduleKind, stepName, outcome, stopwatch.Elapsed.TotalMilliseconds);
    }

    private static async Task<ModuleRunOutcome> ExecuteModuleAsync(
        string moduleKind,
        string stepName,
        Func<Task<ModuleExecution>> execute)
    {
        var stopwatch = Stopwatch.StartNew();
        var outcome = await execute();
        stopwatch.Stop();

        return new ModuleRunOutcome(moduleKind, stepName, outcome, stopwatch.Elapsed.TotalMilliseconds);
    }

    private static void AddModule(
        ModuleRunOutcome outcome,
        ICollection<EngineeringCalculationModuleExecutionResultDto> moduleResults,
        ICollection<EngineeringCalculationModuleTimingDto> timings,
        ICollection<string> executedModules,
        ICollection<string> skippedModules,
        ICollection<string> unavailableModules)
    {
        var status = outcome.Execution.Status;
        if (status == EngineeringCalculationModuleExecutionStatus.Executed)
        {
            executedModules.Add(outcome.ModuleKind);
        }
        else if (status == EngineeringCalculationModuleExecutionStatus.Skipped)
        {
            skippedModules.Add(outcome.ModuleKind);
        }
        else
        {
            unavailableModules.Add(outcome.ModuleKind);
        }

        timings.Add(new EngineeringCalculationModuleTimingDto(outcome.ModuleKind, Round(outcome.DurationMilliseconds)));
        moduleResults.Add(new EngineeringCalculationModuleExecutionResultDto(
            ModuleKind: outcome.ModuleKind,
            Status: status,
            SummaryValues: outcome.Execution.Values,
            Diagnostics: outcome.Execution.Diagnostics,
            Assumptions: outcome.Execution.Assumptions,
            Warnings: outcome.Execution.Warnings,
            DurationMilliseconds: Round(outcome.DurationMilliseconds),
            SourceServiceName: outcome.Execution.SourceServiceName));
    }

    private (BuildingThermalTopology Topology, IReadOnlyList<EngineeringCalculationModuleValueDto> Values, IReadOnlyList<string> Assumptions, IReadOnlyList<string> Warnings, IReadOnlyList<EngineeringWorkflowDiagnosticDto> Diagnostics)
        BuildThermalTopology(
            EngineeringWorkflowStateDto state)
    {
        var zones = state.Zones
            .OrderBy(item => item.ZoneId, StringComparer.Ordinal)
            .Select(item => new ThermalTopologyZoneInput(
                ZoneId: item.ZoneId,
                Name: item.Name,
                RoomIds: [item.ZoneId]))
            .ToArray();

        var rooms = state.Zones
            .OrderBy(item => item.ZoneId, StringComparer.Ordinal)
            .Select(item => new ThermalTopologyRoomInput(
                RoomId: item.ZoneId,
                ZoneId: item.ZoneId,
                VolumeCubicMeters: item.AirVolumeM3,
                FloorAreaSquareMeters: item.FloorAreaM2))
            .ToArray();

        var surfaces = state.Boundaries
            .OrderBy(item => item.BoundaryId, StringComparer.Ordinal)
            .Select(item => new ThermalTopologySurfaceInput(
                SurfaceId: item.BoundaryId,
                RoomId: item.ZoneOrRoomName,
                ZoneId: item.ZoneOrRoomName,
                BoundaryKind: MapBoundaryKind(item.Indicator),
                AreaSquareMeters: item.AreaM2 ?? 0.0,
                UValueWPerSquareMeterKelvin: item.UValue,
                AdjacentZoneId: item.AdjacentZoneReference,
                AdjacentRoomId: null,
                BoundarySource: "EngineeringWorkflowState"))
            .ToArray();

        var topology = _thermalTopologyBuilder.Build(new ThermalTopologyBuildInput(
            BuildingId: state.BuildingId?.ToString() ?? "n/a",
            Zones: zones,
            Rooms: rooms,
            Surfaces: surfaces,
            DisclosureOverride: null));

        var validation = _thermalTopologyValidator.Validate(topology);
        var diagnostics = topology.Diagnostics
            .Concat(validation.Diagnostics)
            .Select(item => FromStandardDiagnostic(item, "Envelope", "ThermalTopology"))
            .ToArray();

        var assumptions = new List<string>(topology.Disclosure.ClaimBoundary.Assumptions)
        {
            "Thermal topology uses workflow state zones/boundaries as deterministic normalized input."
        };

        var warnings = validation.IsValid
            ? Array.Empty<string>()
            : new[] { "Thermal topology validation contains diagnostics." };

        return (
            topology,
            new[]
            {
                new EngineeringCalculationModuleValueDto("zones_count", "Zones count", topology.Zones.Count),
                new EngineeringCalculationModuleValueDto("rooms_count", "Rooms count", topology.Rooms.Count),
                new EngineeringCalculationModuleValueDto("surfaces_count", "Surfaces count", topology.Surfaces.Count),
                new EngineeringCalculationModuleValueDto("is_valid", "Topology valid", validation.IsValid)
            },
            assumptions,
            warnings,
            diagnostics);
    }

    private CalculationTraceDocument BuildTrace(
        EngineeringCalculationScenarioRequestDto request,
        IReadOnlyList<EngineeringCalculationModuleExecutionResultDto> moduleResults,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics,
        IReadOnlyList<string> assumptions,
        IReadOnlyList<string> warnings)
    {
        var detailLevel = ParseTraceDetailLevel(request.DetailLevel);

        _traceBuilder.SetDetailLevel(detailLevel);
        _traceBuilder.Initialize(
            traceId: $"scenario-trace-{request.ScenarioId}",
            calculationType: "EngineeringCalculationScenario",
            rootModule: CalculationTraceModuleKind.Generic,
            calculationId: request.State.BuildingId?.ToString(),
            createdTimestampUtc: request.DeterministicTimestampUtc,
            metadata: new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["execution.mode"] = request.ExecutionMode.ToString(),
                ["scenario.kind"] = request.ScenarioKind.ToString()
            });

        foreach (var module in moduleResults.OrderBy(item => item.ModuleKind, StringComparer.Ordinal))
        {
            var stepId = _traceBuilder.AddStep(
                moduleKind: ParseTraceModuleKind(module.ModuleKind),
                stepName: module.ModuleKind,
                formulaOrConventionLabel: "Engineering scenario runner orchestration");

            _traceBuilder.AddOutputValue(stepId, new CalculationTraceValue(
                Key: "module_status",
                Label: "Module execution status",
                Value: module.Status.ToString(),
                Unit: null,
                ValueKind: CalculationTraceValueKind.Output));

            foreach (var value in module.SummaryValues)
            {
                _traceBuilder.AddOutputValue(stepId, new CalculationTraceValue(
                    Key: value.Key,
                    Label: value.Label,
                    Value: value.Value,
                    Unit: value.Unit is null ? null : new CalculationTraceUnit(value.Unit),
                    ValueKind: CalculationTraceValueKind.Output));
            }

            foreach (var warning in module.Warnings)
            {
                _traceBuilder.AddWarning(stepId, warning);
            }

            foreach (var assumption in module.Assumptions)
            {
                _traceBuilder.AddAssumption(stepId, assumption);
            }

            foreach (var diagnostic in module.Diagnostics)
            {
                _traceBuilder.AddDiagnostic(stepId, new CalculationTraceDiagnostic(
                    Severity: ParseTraceSeverity(diagnostic.Severity),
                    Code: diagnostic.Code,
                    Message: diagnostic.Message,
                    ModuleKind: ParseTraceModuleKind(module.ModuleKind),
                    Context: diagnostic.TargetField,
                    Source: diagnostic.SourceModule));
            }
        }

        foreach (var assumption in assumptions)
        {
            _traceBuilder.AddDocumentAssumption(assumption);
        }

        foreach (var warning in warnings)
        {
            _traceBuilder.AddDocumentWarning(warning);
        }

        foreach (var diagnostic in diagnostics)
        {
            _traceBuilder.AddDocumentDiagnostic(new CalculationTraceDiagnostic(
                Severity: ParseTraceSeverity(diagnostic.Severity),
                Code: diagnostic.Code,
                Message: diagnostic.Message,
                ModuleKind: ParseTraceModuleKindFromStep(diagnostic.SourceStep),
                Context: diagnostic.TargetField,
                Source: diagnostic.SourceModule));
        }

        var trace = _traceBuilder.Build();
        return _traceSanitizer.Sanitize(trace, detailLevel);
    }

    private EngineeringCalculationScenarioResultDto BuildScenarioResult(
        EngineeringCalculationScenarioRequestDto request,
        IReadOnlyList<EngineeringCalculationModuleExecutionResultDto> moduleResults,
        IReadOnlyList<EngineeringCalculationModuleTimingDto> timings,
        IReadOnlyList<string> executedModules,
        IReadOnlyList<string> skippedModules,
        IReadOnlyList<string> unavailableModules,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics,
        IReadOnlyList<string> assumptions,
        IReadOnlyList<string> warnings,
        string? topologySummary,
        string? ventilationSummary,
        string? groundSummary,
        string? heatingCoolingSummaryText,
        string? dhwSummary,
        string? systemEnergySummaryText,
        BuildingEnergyBalanceResult? heatingCoolingResult,
        CalculationTraceDocument? calculationTrace,
        bool includeReport,
        IReadOnlyList<string>? reportFormats)
    {
        var status = DetermineStatus(request.ExecutionMode, moduleResults, diagnostics, warnings);

        EngineeringReportDocument? reportDocument = null;
        EngineeringWorkflowReportPreviewDto? reportPreview = null;
        string? reportJson = null;
        string? reportMarkdown = null;

        if (includeReport)
        {
            var report = _reportBuilder.Build(new EngineeringReportGenerationRequest(
                ReportKind: EngineeringReportKind.FullEngineeringCore,
                RequestedFormat: EngineeringReportFormat.Json,
                ReportTitle: $"Scenario {request.ScenarioId}",
                ProjectId: request.State.ProjectId.ToString(),
                BuildingId: request.State.BuildingId?.ToString(),
                HeatingCoolingSummary: heatingCoolingResult,
                ValidationDiagnostics: diagnostics.Select(item =>
                    new CalculationDiagnostic(
                        Severity: ParseCalculationSeverity(item.Severity),
                        Code: item.Code,
                        Message: item.Message,
                        Context: item.SourceStep)).ToArray(),
                CalculationTrace: calculationTrace,
                DetailLevel: ParseReportDetailLevel(request.DetailLevel),
                IncludeTraceAppendix: request.IncludeTrace,
                IncludeLimitations: true,
                DeterministicTimestampUtc: request.DeterministicTimestampUtc,
                Assumptions: assumptions,
                Warnings: warnings,
                SourceCalculationIds: string.IsNullOrWhiteSpace(request.ScenarioId) ? [] : [request.ScenarioId],
                Metadata: new SortedDictionary<string, string>(StringComparer.Ordinal)
                {
                    ["scenario.kind"] = request.ScenarioKind.ToString(),
                    ["execution.mode"] = request.ExecutionMode.ToString()
                }));

            reportDocument = report;
            reportPreview = new EngineeringWorkflowReportPreviewDto(
                ReportKind: report.ReportKind.ToString(),
                Title: report.Title,
                Sections: report.Sections.OrderBy(item => item.Order).Select(item => item.Title).ToArray(),
                WarningsCount: report.Warnings.Count,
                DiagnosticsCount: report.Diagnostics.Count,
                ExportFormatsAvailable: ["Json", "Markdown"],
                GeneratedTimestampUtc: report.GeneratedTimestampUtc,
                Limitations: report.Sections
                    .Where(item => item.SectionKind == EngineeringReportSectionKind.Limitations)
                    .SelectMany(item => item.KeyValues.Select(value => value.Value?.ToString() ?? string.Empty))
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray());

            var formats = (reportFormats ?? ["Json"])
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (formats.Contains("Json", StringComparer.OrdinalIgnoreCase))
            {
                reportJson = _reportJsonExporter.Export(report, indented: true);
            }

            if (formats.Contains("Markdown", StringComparer.OrdinalIgnoreCase))
            {
                reportMarkdown = _reportMarkdownExporter.Export(report);
            }
        }

        var traceSummary = calculationTrace is null
            ? null
            : new EngineeringWorkflowTraceSummaryDto(
                TraceId: calculationTrace.TraceId,
                CalculationId: calculationTrace.CalculationId,
                DetailLevel: request.DetailLevel ?? "Standard",
                Modules: calculationTrace.Steps.Select(item => item.ModuleKind.ToString()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
                Assumptions: calculationTrace.Assumptions,
                Warnings: calculationTrace.Warnings,
                Steps: calculationTrace.Steps
                    .OrderBy(item => item.Sequence)
                    .Select(item => new EngineeringWorkflowTraceStepSummaryDto(
                        StepId: item.StepId,
                        ModuleKind: item.ModuleKind.ToString(),
                        StepName: item.StepName,
                        Sequence: item.Sequence,
                        Assumptions: item.Assumptions,
                        Warnings: item.Warnings,
                        DiagnosticsCount: item.Diagnostics.Count))
                    .ToArray());

        return new EngineeringCalculationScenarioResultDto(
            ScenarioId: request.ScenarioId,
            Status: status,
            Executed: moduleResults.Any(item => item.Status == EngineeringCalculationModuleExecutionStatus.Executed),
            ExecutedModules: executedModules.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            SkippedModules: skippedModules.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            UnavailableModules: unavailableModules.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            ValidationDiagnostics: SortAndDistinctDiagnostics(diagnostics),
            Assumptions: assumptions.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            Warnings: warnings.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            ModuleSummaries: new EngineeringCalculationModuleSummariesDto(
                TopologySummary: topologySummary,
                VentilationSummary: ventilationSummary,
                GroundSummary: groundSummary,
                HeatingCoolingSummary: heatingCoolingSummaryText,
                DomesticHotWaterSummary: dhwSummary,
                SystemEnergySummary: systemEnergySummaryText),
            ModuleResults: moduleResults
                .OrderBy(item => item.ModuleKind, StringComparer.Ordinal)
                .ToArray(),
            Timings: timings
                .OrderByDescending(item => item.DurationMilliseconds)
                .ThenBy(item => item.ModuleKind, StringComparer.Ordinal)
                .ToArray(),
            CalculationTrace: calculationTrace,
            CalculationTraceSummary: traceSummary,
            EngineeringReport: reportDocument,
            ReportPreview: reportPreview,
            ReportJson: reportJson,
            ReportMarkdown: reportMarkdown,
            Metadata: new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["execution.mode"] = request.ExecutionMode.ToString(),
                ["scenario.kind"] = request.ScenarioKind.ToString(),
                ["module.executed.count"] = executedModules.Count.ToString(),
                ["module.skipped.count"] = skippedModules.Count.ToString(),
                ["module.unavailable.count"] = unavailableModules.Count.ToString()
            });
    }

    private static IReadOnlyList<SystemEnergyUsefulLoadInput> BuildSystemEnergyLoads(
        EngineeringWorkflowStateDto state,
        DomesticHotWaterSystemLoadFoundationResult? dhwSummary)
    {
        var loads = new List<SystemEnergyUsefulLoadInput>();

        var heating = ResolveAnnualValue(state.Metadata, "system_energy.heating_annual_kwh");
        if (heating is > 0.0)
        {
            loads.Add(CreateSystemEnergyLoad("load-heating", state, SystemEnergyEndUse.SpaceHeating, heating.Value));
        }

        var cooling = ResolveAnnualValue(state.Metadata, "system_energy.cooling_annual_kwh");
        if (cooling is > 0.0)
        {
            loads.Add(CreateSystemEnergyLoad("load-cooling", state, SystemEnergyEndUse.SpaceCooling, cooling.Value));
        }

        var dhw = ResolveAnnualValue(state.Metadata, "system_energy.dhw_annual_kwh");
        if (dhw is > 0.0)
        {
            loads.Add(CreateSystemEnergyLoad("load-dhw", state, SystemEnergyEndUse.DomesticHotWater, dhw.Value));
        }
        else if (dhwSummary is not null)
        {
            loads.Add(new SystemEnergyUsefulLoadInput(
                LoadId: "load-dhw-foundation",
                BuildingId: state.BuildingId?.ToString(),
                ZoneId: null,
                RoomId: null,
                EndUse: SystemEnergyEndUse.DomesticHotWater,
                HourlyUsefulEnergyKWh8760: dhwSummary.UsefulEnergyProfileKWh,
                MonthlyUsefulEnergyKWh: null,
                AnnualUsefulEnergyKWh: dhwSummary.AnnualSummary.UsefulEnergyKWh,
                Source: "EngineeringCalculationScenarioRunner",
                Diagnostics: [],
                HourlySystemLoadKWh8760: dhwSummary.SystemLoadProfileKWh,
                TimeStepHours: 1.0,
                LossOwnershipPolicy: SystemEnergyLossOwnershipPolicy.NoDoubleCounting,
                Assumptions:
                [
                    "DHW load was adapted from DHW foundation output profile."
                ]));
        }

        return loads;
    }

    private static SystemEnergyUsefulLoadInput CreateSystemEnergyLoad(
        string loadId,
        EngineeringWorkflowStateDto state,
        SystemEnergyEndUse endUse,
        double annualKwh)
    {
        var profile = BuildFlatProfile8760(annualKwh);
        return new SystemEnergyUsefulLoadInput(
            LoadId: loadId,
            BuildingId: state.BuildingId?.ToString(),
            ZoneId: null,
            RoomId: null,
            EndUse: endUse,
            HourlyUsefulEnergyKWh8760: profile,
            MonthlyUsefulEnergyKWh: null,
            AnnualUsefulEnergyKWh: annualKwh,
            Source: "EngineeringCalculationScenarioRunner",
            Diagnostics: [],
            HourlySystemLoadKWh8760: null,
            TimeStepHours: 1.0,
            LossOwnershipPolicy: SystemEnergyLossOwnershipPolicy.NoDoubleCounting,
            Assumptions:
            [
                "Useful load profile was expanded as deterministic flat 8760 profile from annual metadata."
            ]);
    }

    private static IReadOnlyList<SystemEnergyStageDefinition> BuildDefaultSystemEnergyStages(
        IReadOnlyList<SystemEnergyUsefulLoadInput> loads)
    {
        var definitions = new List<SystemEnergyStageDefinition>();

        foreach (var load in loads)
        {
            var useKind = MapUseKind(load.EndUse);
            definitions.Add(CreateStage(SystemEnergySubsystemKind.Emission, useKind, 10));
            definitions.Add(CreateStage(SystemEnergySubsystemKind.Distribution, useKind, 20));
            definitions.Add(CreateStage(SystemEnergySubsystemKind.Storage, useKind, 30));
        }

        return definitions;
    }

    private static SystemEnergyStageDefinition CreateStage(
        SystemEnergySubsystemKind subsystemKind,
        SystemEnergyUseKind useKind,
        int priority)
    {
        return new SystemEnergyStageDefinition(
            StageId: $"{subsystemKind}-{useKind}-{priority}",
            SubsystemKind: subsystemKind,
            AppliesToUse: useKind,
            Efficiency: 1.0,
            LossFraction: 0.0,
            FixedLossProfile: null,
            AuxiliaryEnergyProfile: null,
            RecoveredLossFraction: 0.0,
            TargetCarrier: SystemEnergyCarrierKind.Electricity,
            CalculationMode: SystemEnergyModuleCalculationMode.FixedEfficiency,
            VerboseDiagnostics: false,
            Priority: priority,
            Source: "EngineeringCalculationScenarioRunner");
    }

    private static IReadOnlyList<SystemEnergyGeneratorDefinition> BuildDefaultSystemEnergyGenerators(
        IReadOnlyList<SystemEnergyUsefulLoadInput> loads)
    {
        return loads
            .Select(load => new SystemEnergyGeneratorDefinition(
                GeneratorId: $"generator-{load.EndUse}-{load.LoadId}",
                UseKind: MapUseKind(load.EndUse),
                GeneratorKind: SystemEnergyGeneratorKind.GenericEfficiencyGenerator,
                CarrierKind: SystemEnergyCarrierKind.Electricity,
                Efficiency: 1.0,
                Cop: null,
                SeasonalPerformanceFactor: null,
                RenewableContributionFraction: 0.0,
                AuxiliaryEnergyProfile: null,
                Priority: 0,
                Source: "EngineeringCalculationScenarioRunner"))
            .ToArray();
    }

    private static EnergyFactorCatalog BuildDefaultFactorCatalog()
    {
        return new EnergyFactorCatalog(
            CatalogId: "scenario-default-factors",
            Version: "v1",
            Entries:
            [
                new EnergyFactorCatalogEntry(
                    CarrierKind: SystemEnergyCarrierKind.Electricity,
                    PrimaryEnergyFactorNonRenewable: 1.8,
                    PrimaryEnergyFactorRenewable: 0.2,
                    TotalPrimaryEnergyFactor: 2.0,
                    Co2FactorKgPerKWh: 0.4,
                    SourceLabel: "ScenarioDefault")
            ],
            Source: "EngineeringCalculationScenarioRunner");
    }

    private static ThermalBoundaryKind MapBoundaryKind(string indicator)
    {
        if (indicator.Equals("ground", StringComparison.OrdinalIgnoreCase))
            return ThermalBoundaryKind.Ground;

        if (indicator.Equals("adiabatic", StringComparison.OrdinalIgnoreCase))
            return ThermalBoundaryKind.Adiabatic;

        if (indicator.Equals("adjacent", StringComparison.OrdinalIgnoreCase))
            return ThermalBoundaryKind.AdjacentUnconditionedZone;

        return ThermalBoundaryKind.Outdoor;
    }

    private static SystemEnergyUseKind MapUseKind(SystemEnergyEndUse endUse)
    {
        return endUse switch
        {
            SystemEnergyEndUse.SpaceHeating => SystemEnergyUseKind.SpaceHeating,
            SystemEnergyEndUse.SpaceCooling => SystemEnergyUseKind.SpaceCooling,
            SystemEnergyEndUse.DomesticHotWater => SystemEnergyUseKind.DomesticHotWater,
            SystemEnergyEndUse.Ventilation => SystemEnergyUseKind.Ventilation,
            SystemEnergyEndUse.Auxiliary => SystemEnergyUseKind.Auxiliary,
            _ => SystemEnergyUseKind.Generic
        };
    }

    private static EngineeringCalculationExecutionStatus DetermineStatus(
        EngineeringCalculationExecutionMode mode,
        IReadOnlyCollection<EngineeringCalculationModuleExecutionResultDto> moduleResults,
        IReadOnlyCollection<EngineeringWorkflowDiagnosticDto> diagnostics,
        IReadOnlyCollection<string> warnings)
    {
        var hasErrors = diagnostics.Any(item => IsError(item.Severity));

        if (mode == EngineeringCalculationExecutionMode.PrepareOnly || mode == EngineeringCalculationExecutionMode.ValidateOnly || mode == EngineeringCalculationExecutionMode.DryRun)
        {
            return hasErrors
                ? EngineeringCalculationExecutionStatus.FailedValidation
                : EngineeringCalculationExecutionStatus.Prepared;
        }

        if (hasErrors && moduleResults.All(item => item.Status != EngineeringCalculationModuleExecutionStatus.Executed))
        {
            return EngineeringCalculationExecutionStatus.FailedValidation;
        }

        if (moduleResults.Any(item => item.Status == EngineeringCalculationModuleExecutionStatus.Failed))
        {
            return EngineeringCalculationExecutionStatus.FailedExecution;
        }

        var executedCount = moduleResults.Count(item => item.Status == EngineeringCalculationModuleExecutionStatus.Executed);
        var skippedCount = moduleResults.Count(item => item.Status == EngineeringCalculationModuleExecutionStatus.Skipped);

        if (executedCount == 0 && skippedCount > 0)
        {
            return EngineeringCalculationExecutionStatus.PartiallyExecuted;
        }

        if (skippedCount > 0)
        {
            return EngineeringCalculationExecutionStatus.PartiallyExecuted;
        }

        if (warnings.Count > 0 || diagnostics.Any(item => item.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase)))
        {
            return EngineeringCalculationExecutionStatus.CompletedWithWarnings;
        }

        return EngineeringCalculationExecutionStatus.Completed;
    }

    private static CalculationTraceModuleKind ParseTraceModuleKind(string moduleKind)
    {
        return moduleKind switch
        {
            "ThermalTopology" => CalculationTraceModuleKind.ThermalTopology,
            "WeatherSolar" => CalculationTraceModuleKind.Weather,
            "Ventilation" => CalculationTraceModuleKind.Ventilation,
            "Ground" => CalculationTraceModuleKind.Ground,
            "HeatingCooling" => CalculationTraceModuleKind.Iso52016,
            "DomesticHotWater" => CalculationTraceModuleKind.DomesticHotWater,
            "SystemEnergy" => CalculationTraceModuleKind.SystemEnergy,
            _ => CalculationTraceModuleKind.Generic
        };
    }

    private static CalculationTraceModuleKind ParseTraceModuleKindFromStep(string sourceStep)
    {
        return sourceStep switch
        {
            "Zones" or "Envelope" => CalculationTraceModuleKind.ThermalTopology,
            "WeatherSolar" => CalculationTraceModuleKind.Weather,
            "Ventilation" => CalculationTraceModuleKind.Ventilation,
            "Ground" => CalculationTraceModuleKind.Ground,
            "DomesticHotWater" => CalculationTraceModuleKind.DomesticHotWater,
            "SystemEnergy" => CalculationTraceModuleKind.SystemEnergy,
            "Reports" => CalculationTraceModuleKind.Reporting,
            "Validation" => CalculationTraceModuleKind.Validation,
            _ => CalculationTraceModuleKind.Generic
        };
    }

    private static CalculationTraceSeverity ParseTraceSeverity(string severity)
    {
        if (severity.Equals("error", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceSeverity.Error;
        if (severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceSeverity.Warning;
        if (severity.Equals("assumption", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceSeverity.Assumption;
        if (severity.Equals("debug", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceSeverity.Debug;

        return CalculationTraceSeverity.Info;
    }

    private static CalculationDiagnosticSeverity ParseCalculationSeverity(string severity)
    {
        if (severity.Equals("error", StringComparison.OrdinalIgnoreCase))
            return CalculationDiagnosticSeverity.Error;
        if (severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
            return CalculationDiagnosticSeverity.Warning;

        return CalculationDiagnosticSeverity.Info;
    }

    private static CalculationTraceDetailLevel ParseTraceDetailLevel(string? detailLevel)
    {
        if (!string.IsNullOrWhiteSpace(detailLevel) &&
            Enum.TryParse<CalculationTraceDetailLevel>(detailLevel, true, out var parsed))
        {
            return parsed;
        }

        return CalculationTraceDetailLevel.Standard;
    }

    private static EngineeringReportDetailLevel ParseReportDetailLevel(string? detailLevel)
    {
        if (!string.IsNullOrWhiteSpace(detailLevel) &&
            Enum.TryParse<EngineeringReportDetailLevel>(detailLevel, true, out var parsed))
        {
            return parsed;
        }

        return EngineeringReportDetailLevel.Standard;
    }

    private static EngineeringWorkflowDiagnosticDto FromCalculationDiagnostic(
        CalculationDiagnostic diagnostic,
        string step,
        string? sourceModule = null)
    {
        return new EngineeringWorkflowDiagnosticDto(
            Severity: diagnostic.Severity switch
            {
                CalculationDiagnosticSeverity.Error => "error",
                CalculationDiagnosticSeverity.Warning => "warning",
                _ => "info"
            },
            Code: diagnostic.Code,
            Message: diagnostic.Message,
            SourceStep: step,
            SourceModule: sourceModule,
            TargetField: diagnostic.Context);
    }

    private static EngineeringWorkflowDiagnosticDto FromStandardDiagnostic(
        StandardCalculationDiagnostic diagnostic,
        string step,
        string? sourceModule = null)
    {
        return new EngineeringWorkflowDiagnosticDto(
            Severity: diagnostic.Severity switch
            {
                CalculationDiagnosticSeverity.Error => "error",
                CalculationDiagnosticSeverity.Warning => "warning",
                _ => "info"
            },
            Code: diagnostic.Code,
            Message: diagnostic.Message,
            SourceStep: step,
            SourceModule: sourceModule,
            TargetField: diagnostic.Context);
    }

    private static IReadOnlyList<EngineeringWorkflowDiagnosticDto> SortAndDistinctDiagnostics(
        IEnumerable<EngineeringWorkflowDiagnosticDto> diagnostics)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        return diagnostics
            .Where(item => !string.IsNullOrWhiteSpace(item.Message))
            .OrderByDescending(item => SeverityRank(item.Severity))
            .ThenBy(item => item.SourceStep.ToString(), StringComparer.Ordinal)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.Message, StringComparer.Ordinal)
            .Where(item => seen.Add($"{item.SourceStep}|{item.Code}|{item.Message}|{item.TargetField}"))
            .ToArray();
    }

    private static int SeverityRank(string severity)
    {
        if (severity.Equals("error", StringComparison.OrdinalIgnoreCase))
            return 4;
        if (severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
            return 3;
        if (severity.Equals("assumption", StringComparison.OrdinalIgnoreCase))
            return 2;

        return 1;
    }

    private static bool IsError(string severity) =>
        severity.Equals("error", StringComparison.OrdinalIgnoreCase);

    private static string FindSummary(
        IEnumerable<EngineeringCalculationModuleExecutionResultDto> moduleResults,
        string moduleKind)
    {
        var result = moduleResults.FirstOrDefault(item => item.ModuleKind.Equals(moduleKind, StringComparison.Ordinal));
        if (result is null)
            return "Not started.";

        return result.Status switch
        {
            EngineeringCalculationModuleExecutionStatus.Executed => "Executed.",
            EngineeringCalculationModuleExecutionStatus.Skipped => "Skipped.",
            EngineeringCalculationModuleExecutionStatus.NotSupported => "Not supported.",
            EngineeringCalculationModuleExecutionStatus.Failed => "Failed.",
            _ => "Not started."
        };
    }

    private static double[] BuildFlatProfile8760(double annualValueKwh)
    {
        var safeAnnual = double.IsFinite(annualValueKwh) && annualValueKwh > 0.0
            ? annualValueKwh
            : 0.0;
        var hourly = safeAnnual / 8760.0;
        return Enumerable.Repeat(hourly, 8760).ToArray();
    }

    private static double? ResolveAnnualValue(
        IReadOnlyDictionary<string, string> metadata,
        string key)
    {
        if (!metadata.TryGetValue(key, out var raw))
            return null;

        if (double.TryParse(raw, out var parsed) && double.IsFinite(parsed) && parsed > 0.0)
            return parsed;

        return null;
    }

    private static double Round(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);

    private sealed record ModuleRunOutcome(
        string ModuleKind,
        string StepName,
        ModuleExecution Execution,
        double DurationMilliseconds);

    private sealed class ModuleExecution
    {
        public EngineeringCalculationModuleExecutionStatus Status { get; }
        public IReadOnlyList<EngineeringCalculationModuleValueDto> Values { get; }
        public IReadOnlyList<EngineeringWorkflowDiagnosticDto> Diagnostics { get; }
        public IReadOnlyList<string> Assumptions { get; }
        public IReadOnlyList<string> Warnings { get; }
        public string SourceServiceName { get; }

        private ModuleExecution(
            EngineeringCalculationModuleExecutionStatus status,
            IReadOnlyList<EngineeringCalculationModuleValueDto> values,
            IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics,
            IReadOnlyList<string> assumptions,
            IReadOnlyList<string> warnings,
            string sourceServiceName)
        {
            Status = status;
            Values = values;
            Diagnostics = diagnostics;
            Assumptions = assumptions;
            Warnings = warnings;
            SourceServiceName = sourceServiceName;
        }

        public static ModuleExecution Execute(
            IReadOnlyList<EngineeringCalculationModuleValueDto> values,
            string sourceServiceName) =>
            new(
                EngineeringCalculationModuleExecutionStatus.Executed,
                values,
                [],
                [],
                [],
                sourceServiceName);

        public static ModuleExecution Skip(
            string message,
            string suggestedCorrection) =>
            new(
                EngineeringCalculationModuleExecutionStatus.Skipped,
                [],
                [
                    new EngineeringWorkflowDiagnosticDto(
                        Severity: "warning",
                        Code: "SCENARIO_MODULE_SKIPPED",
                        Message: message,
                        SourceStep: "Review",
                        SuggestedCorrection: suggestedCorrection)
                ],
                [],
                [message],
                "EngineeringCalculationScenarioRunner");

        public static ModuleExecution Fail(
            string message,
            string suggestedCorrection) =>
            new(
                EngineeringCalculationModuleExecutionStatus.Failed,
                [],
                [
                    new EngineeringWorkflowDiagnosticDto(
                        Severity: "error",
                        Code: "SCENARIO_MODULE_FAILED",
                        Message: message,
                        SourceStep: "Review",
                        SuggestedCorrection: suggestedCorrection)
                ],
                [],
                [],
                "EngineeringCalculationScenarioRunner");
    }
}
