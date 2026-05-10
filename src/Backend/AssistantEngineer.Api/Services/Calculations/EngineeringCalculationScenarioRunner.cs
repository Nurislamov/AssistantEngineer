using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Topology;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Facades;

namespace AssistantEngineer.Api.Services.Calculations;

public sealed class EngineeringCalculationScenarioRunner : IEngineeringCalculationScenarioRunner
{
    private readonly ILoadCalculationsFacade _loadCalculations;
    private readonly IThermalTopologyBuilder _thermalTopologyBuilder;
    private readonly IThermalTopologyValidator _thermalTopologyValidator;
    private readonly IEngineeringCalculationScenarioModuleExecutor _moduleExecutor;
    private readonly IEngineeringCalculationWeatherSolarScenarioStep _weatherSolarScenarioStep;
    private readonly IEngineeringCalculationVentilationScenarioStep _ventilationScenarioStep;
    private readonly IEngineeringCalculationGroundScenarioStep _groundScenarioStep;
    private readonly IEngineeringCalculationDomesticHotWaterScenarioStep _domesticHotWaterScenarioStep;
    private readonly IEngineeringCalculationSystemEnergyScenarioStep _systemEnergyScenarioStep;
    private readonly IEngineeringCalculationScenarioResultBuilder _resultBuilder;
    private readonly IEngineeringCalculationScenarioRequestValidator _requestValidator;

    public EngineeringCalculationScenarioRunner(
        ILoadCalculationsFacade loadCalculations,
        IThermalTopologyBuilder thermalTopologyBuilder,
        IThermalTopologyValidator thermalTopologyValidator,
        IEngineeringCalculationScenarioModuleExecutor moduleExecutor,
        IEngineeringCalculationWeatherSolarScenarioStep weatherSolarScenarioStep,
        IEngineeringCalculationVentilationScenarioStep ventilationScenarioStep,
        IEngineeringCalculationGroundScenarioStep groundScenarioStep,
        IEngineeringCalculationDomesticHotWaterScenarioStep domesticHotWaterScenarioStep,
        IEngineeringCalculationSystemEnergyScenarioStep systemEnergyScenarioStep,
        IEngineeringCalculationScenarioResultBuilder resultBuilder,
        IEngineeringCalculationScenarioRequestValidator requestValidator)
    {
        _loadCalculations = loadCalculations;
        _thermalTopologyBuilder = thermalTopologyBuilder;
        _thermalTopologyValidator = thermalTopologyValidator;
        _moduleExecutor = moduleExecutor;
        _weatherSolarScenarioStep = weatherSolarScenarioStep;
        _ventilationScenarioStep = ventilationScenarioStep;
        _groundScenarioStep = groundScenarioStep;
        _domesticHotWaterScenarioStep = domesticHotWaterScenarioStep;
        _systemEnergyScenarioStep = systemEnergyScenarioStep;
        _resultBuilder = resultBuilder;
        _requestValidator = requestValidator;
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

        diagnostics.AddRange(_requestValidator.Validate(request));
        diagnostics = _requestValidator.SortAndDistinct(diagnostics).ToList();

        if (request.ExecutionMode == EngineeringCalculationExecutionMode.ValidateOnly)
        {
            assumptions.Add("Execution mode ValidateOnly returns deterministic diagnostics without module execution.");
            return _resultBuilder.BuildScenarioResult(
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
                calculationTrace: request.IncludeTrace ? _resultBuilder.BuildTrace(request, moduleResults, diagnostics, assumptions, warnings) : null,
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

            return _resultBuilder.BuildScenarioResult(
                request,
                moduleResults,
                timings,
                executedModules,
                skippedModules,
                unavailableModules,
                _requestValidator.SortAndDistinct(diagnostics),
                assumptions,
                warnings,
                topologySummary: "Prepared only.",
                ventilationSummary: "Prepared only.",
                groundSummary: "Prepared only.",
                heatingCoolingSummaryText: "Prepared only.",
                dhwSummary: "Prepared only.",
                systemEnergySummaryText: "Prepared only.",
                heatingCoolingResult: null,
                calculationTrace: request.IncludeTrace ? _resultBuilder.BuildTrace(request, moduleResults, diagnostics, assumptions, warnings) : null,
                includeReport: request.IncludeReport,
                reportFormats: request.ReportFormats);
        }

        if (request.ExecutionMode == EngineeringCalculationExecutionMode.DryRun)
        {
            assumptions.Add("Execution mode DryRun returns deterministic execution plan without invoking calculators.");
            return _resultBuilder.BuildScenarioResult(
                request,
                moduleResults,
                timings,
                executedModules,
                skippedModules,
                unavailableModules,
                _requestValidator.SortAndDistinct(diagnostics),
                assumptions,
                warnings,
                topologySummary: "DryRun plan only.",
                ventilationSummary: "DryRun plan only.",
                groundSummary: "DryRun plan only.",
                heatingCoolingSummaryText: "DryRun plan only.",
                dhwSummary: "DryRun plan only.",
                systemEnergySummaryText: "DryRun plan only.",
                heatingCoolingResult: null,
                calculationTrace: request.IncludeTrace ? _resultBuilder.BuildTrace(request, moduleResults, diagnostics, assumptions, warnings) : null,
                includeReport: request.IncludeReport,
                reportFormats: request.ReportFormats);
        }

        if (request.ExecutionMode == EngineeringCalculationExecutionMode.ExecuteFullRequired &&
            _requestValidator.HasErrors(diagnostics))
        {
            assumptions.Add("Execution mode ExecuteFullRequired stops when critical validation diagnostics are present.");

            return _resultBuilder.BuildScenarioResult(
                request,
                moduleResults,
                timings,
                executedModules,
                skippedModules,
                unavailableModules,
                _requestValidator.SortAndDistinct(diagnostics),
                assumptions,
                warnings,
                topologySummary: null,
                ventilationSummary: null,
                groundSummary: null,
                heatingCoolingSummaryText: null,
                dhwSummary: null,
                systemEnergySummaryText: null,
                heatingCoolingResult: null,
                calculationTrace: request.IncludeTrace ? _resultBuilder.BuildTrace(request, moduleResults, diagnostics, assumptions, warnings) : null,
                includeReport: request.IncludeReport,
                reportFormats: request.ReportFormats);
        }

        var modeAllowsPartial = request.ExecutionMode == EngineeringCalculationExecutionMode.ExecuteAvailableModules;

        var topologyRun = _moduleExecutor.Execute(
            "ThermalTopology",
            "Thermal topology normalization",
            () =>
            {
                if (request.State.Zones.Count == 0 || request.State.Boundaries.Count == 0)
                {
                    return ScenarioModuleExecution.Skip(
                        "Thermal topology requires zones and boundaries.",
                        "Configure at least one zone and one envelope boundary.");
                }

                var result = BuildThermalTopology(request.State);
                topology = result.Topology;
                assumptions.AddRange(result.Assumptions);
                warnings.AddRange(result.Warnings);
                diagnostics.AddRange(result.Diagnostics);
                return ScenarioModuleExecution.Execute(result.Values, "IThermalTopologyBuilder");
            });
        _moduleExecutor.AddModuleOutcome(topologyRun, moduleResults, timings, executedModules, skippedModules, unavailableModules);

        var weatherRun = _moduleExecutor.Execute(
            "WeatherSolar",
            "Weather and solar readiness",
            () => _weatherSolarScenarioStep.Execute(request));
        _moduleExecutor.AddModuleOutcome(weatherRun, moduleResults, timings, executedModules, skippedModules, unavailableModules);
        var ventilationRun = _moduleExecutor.Execute(
            "Ventilation",
            "Natural ventilation execution",
            () => _ventilationScenarioStep.Execute(request));
        _moduleExecutor.AddModuleOutcome(ventilationRun, moduleResults, timings, executedModules, skippedModules, unavailableModules);
        var groundRun = _moduleExecutor.Execute(
            "Ground",
            "Ground boundary execution",
            () => _groundScenarioStep.Execute(request));
        _moduleExecutor.AddModuleOutcome(groundRun, moduleResults, timings, executedModules, skippedModules, unavailableModules);
        var heatingRun = await _moduleExecutor.ExecuteAsync(
            "HeatingCooling",
            "ISO52016/MultiZone heating-cooling load",
            async () =>
            {
                if (!request.State.BuildingId.HasValue || request.State.BuildingId <= 0)
                {
                    return ScenarioModuleExecution.Skip(
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
                        ? ScenarioModuleExecution.Skip(
                            $"Heating/cooling module was not executed: {result.Error}",
                            "Ensure building model and climate data are available for load calculation.")
                        : ScenarioModuleExecution.Fail(
                            $"Heating/cooling module failed: {result.Error}",
                            "ExecuteFullRequired mode requires successful heating/cooling execution.");
                }

                heatingCoolingSummary = result.Value;
                assumptions.AddRange(heatingCoolingSummary.Assumptions);
                diagnostics.AddRange(heatingCoolingSummary.Diagnostics.Select(item =>
                    FromCalculationDiagnostic(item, "Validation", "Iso52016")));

                return ScenarioModuleExecution.Execute(
                [
                    new EngineeringCalculationModuleValueDto("annual_heating_kwh", "Annual heating demand", heatingCoolingSummary.AnnualHeatingDemandKWh, "kWh"),
                    new EngineeringCalculationModuleValueDto("annual_cooling_kwh", "Annual cooling demand", heatingCoolingSummary.AnnualCoolingDemandKWh, "kWh"),
                    new EngineeringCalculationModuleValueDto("annual_total_kwh", "Annual total demand", heatingCoolingSummary.AnnualTotalDemandKWh, "kWh")
                ], "ILoadCalculationsFacade");
            });
        _moduleExecutor.AddModuleOutcome(heatingRun, moduleResults, timings, executedModules, skippedModules, unavailableModules);

        var dhwRun = _moduleExecutor.Execute(
            "DomesticHotWater",
            "Domestic hot water execution",
            () =>
            {
                var stepResult = _domesticHotWaterScenarioStep.Execute(request);
                if (stepResult.Summary is not null)
                {
                    dhwFoundationSummary = stepResult.Summary;
                }

                assumptions.AddRange(stepResult.Assumptions);
                warnings.AddRange(stepResult.Warnings);
                diagnostics.AddRange(stepResult.Diagnostics);
                return stepResult.Execution;
            });
        _moduleExecutor.AddModuleOutcome(dhwRun, moduleResults, timings, executedModules, skippedModules, unavailableModules);
        var systemEnergyRun = _moduleExecutor.Execute(
            "SystemEnergy",
            "System energy execution",
            () =>
            {
                var stepResult = _systemEnergyScenarioStep.Execute(request, dhwFoundationSummary);
                if (stepResult.Summary is not null)
                {
                    systemEnergySummary = stepResult.Summary;
                }

                assumptions.AddRange(stepResult.Assumptions);
                warnings.AddRange(stepResult.Warnings);
                diagnostics.AddRange(stepResult.Diagnostics);
                return stepResult.Execution;
            });
        _moduleExecutor.AddModuleOutcome(systemEnergyRun, moduleResults, timings, executedModules, skippedModules, unavailableModules);
        diagnostics = _requestValidator.SortAndDistinct(diagnostics).ToList();
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
            ? _resultBuilder.BuildTrace(request, moduleResults, diagnostics, assumptions, warnings)
            : null;

        return _resultBuilder.BuildScenarioResult(
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
            ventilationSummary: _resultBuilder.FindModuleSummary(moduleResults, "Ventilation"),
            groundSummary: _resultBuilder.FindModuleSummary(moduleResults, "Ground"),
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

    private static double Round(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);
}
