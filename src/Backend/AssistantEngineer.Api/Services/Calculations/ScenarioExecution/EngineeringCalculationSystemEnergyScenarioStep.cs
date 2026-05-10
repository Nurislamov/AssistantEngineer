using System.Globalization;
using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Api.Services.Calculations;

public sealed class EngineeringCalculationSystemEnergyScenarioStep : IEngineeringCalculationSystemEnergyScenarioStep
{
    private readonly ISystemEnergyFoundationCalculator _systemEnergyFoundationCalculator;

    public EngineeringCalculationSystemEnergyScenarioStep(
        ISystemEnergyFoundationCalculator systemEnergyFoundationCalculator)
    {
        _systemEnergyFoundationCalculator = systemEnergyFoundationCalculator;
    }

    public EngineeringCalculationSystemEnergyScenarioStepResult Execute(
        EngineeringCalculationScenarioRequestDto request,
        DomesticHotWaterSystemLoadFoundationResult? domesticHotWaterSummary)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.State);

        var loads = BuildSystemEnergyLoads(request.State, domesticHotWaterSummary);
        if (loads.Count == 0)
        {
            return new EngineeringCalculationSystemEnergyScenarioStepResult(
                ScenarioModuleExecution.Skip(
                    "System-energy useful loads are unavailable (metadata keys or DHW handoff missing).",
                    "Provide structured load metadata (`system_energy.*_annual_kwh`) or execute DHW module first."),
                Summary: null,
                Assumptions: [],
                Warnings: [],
                Diagnostics: []);
        }

        var stageDefinitions = BuildDefaultSystemEnergyStages(loads);
        var generatorDefinitions = BuildDefaultSystemEnergyGenerators(loads);

        var summary = _systemEnergyFoundationCalculator.Calculate(
            new SystemEnergyCalculationRequest(
                CalculationId: request.ScenarioId,
                LoadInputs: loads,
                StageDefinitions: stageDefinitions,
                GeneratorDefinitions: generatorDefinitions,
                FactorCatalog: BuildDefaultFactorCatalog(),
                TimeStepHours: 1.0,
                OutputResolution: SystemEnergyProfileShape.Hourly8760,
                OwnershipPolicy: SystemEnergyLossOwnershipPolicy.NoDoubleCounting,
                StrictFactorMode: false));

        return new EngineeringCalculationSystemEnergyScenarioStepResult(
            ScenarioModuleExecution.Execute(
            [
                new EngineeringCalculationModuleValueDto("system_final_kwh", "Annual final energy", summary.AnnualSummary.FinalEnergyKWh, "kWh"),
                new EngineeringCalculationModuleValueDto("system_primary_kwh", "Annual primary energy", summary.AnnualSummary.PrimaryEnergyKWh, "kWh"),
                new EngineeringCalculationModuleValueDto("system_co2_kg", "Annual CO2 emissions", summary.AnnualSummary.Co2Kg, "kg")
            ], "ISystemEnergyFoundationCalculator"),
            Summary: summary,
            Assumptions: summary.Assumptions,
            Warnings: summary.Warnings,
            Diagnostics: summary.Diagnostics.Select(item =>
                FromStandardDiagnostic(item, "SystemEnergy", "SystemEnergyFoundation")).ToArray());
    }

    private static IReadOnlyList<SystemEnergyUsefulLoadInput> BuildSystemEnergyLoads(
        EngineeringWorkflowStateDto state,
        DomesticHotWaterSystemLoadFoundationResult? domesticHotWaterSummary)
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
        else if (domesticHotWaterSummary is not null)
        {
            loads.Add(new SystemEnergyUsefulLoadInput(
                LoadId: "load-dhw-foundation",
                BuildingId: state.BuildingId?.ToString(),
                ZoneId: null,
                RoomId: null,
                EndUse: SystemEnergyEndUse.DomesticHotWater,
                HourlyUsefulEnergyKWh8760: domesticHotWaterSummary.UsefulEnergyProfileKWh,
                MonthlyUsefulEnergyKWh: null,
                AnnualUsefulEnergyKWh: domesticHotWaterSummary.AnnualSummary.UsefulEnergyKWh,
                Source: "EngineeringCalculationSystemEnergyScenarioStep",
                Diagnostics: [],
                HourlySystemLoadKWh8760: domesticHotWaterSummary.SystemLoadProfileKWh,
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
            Source: "EngineeringCalculationSystemEnergyScenarioStep",
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
            Source: "EngineeringCalculationSystemEnergyScenarioStep");
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
                Source: "EngineeringCalculationSystemEnergyScenarioStep"))
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
            Source: "EngineeringCalculationSystemEnergyScenarioStep");
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

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var invariantParsed) &&
            double.IsFinite(invariantParsed) &&
            invariantParsed > 0.0)
        {
            return invariantParsed;
        }

        if (double.TryParse(raw, out var parsed) && double.IsFinite(parsed) && parsed > 0.0)
            return parsed;

        return null;
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
}