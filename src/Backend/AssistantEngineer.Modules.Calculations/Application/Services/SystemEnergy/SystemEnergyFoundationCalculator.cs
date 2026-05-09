using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class SystemEnergyFoundationCalculator : ISystemEnergyFoundationCalculator
{
    private const string Source = "SystemEnergyFoundationCalculator";
    private readonly ISystemEnergyEmissionCalculator _emissionCalculator;
    private readonly ISystemEnergyDistributionCalculator _distributionCalculator;
    private readonly ISystemEnergyStorageCalculator _storageCalculator;
    private readonly ISystemEnergyGenerationCalculator _generationCalculator;

    public SystemEnergyFoundationCalculator(
        ISystemEnergyEmissionCalculator emissionCalculator,
        ISystemEnergyDistributionCalculator distributionCalculator,
        ISystemEnergyStorageCalculator storageCalculator,
        ISystemEnergyGenerationCalculator generationCalculator)
    {
        _emissionCalculator = emissionCalculator ?? throw new ArgumentNullException(nameof(emissionCalculator));
        _distributionCalculator = distributionCalculator ?? throw new ArgumentNullException(nameof(distributionCalculator));
        _storageCalculator = storageCalculator ?? throw new ArgumentNullException(nameof(storageCalculator));
        _generationCalculator = generationCalculator ?? throw new ArgumentNullException(nameof(generationCalculator));
    }

    public SystemEnergyCalculationResult Calculate(SystemEnergyCalculationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var assumptions = new List<string>();
        var warnings = new List<string>();
        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(ValidateRequest(request));

        var usefulByUse = new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>();
        var systemLoadByUse = new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>();
        var emissionLossByUse = new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>();
        var distributionLossByUse = new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>();
        var storageLossByUse = new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>();
        var generationLossByUse = new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>();
        var recoveredByUse = new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>();
        var auxiliaryByUse = new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>();

        var finalByCarrier = new Dictionary<SystemEnergyCarrierKind, double[]>(EqualityComparer<SystemEnergyCarrierKind>.Default);

        foreach (var loadInput in request.LoadInputs)
        {
            var useKind = SystemEnergyFoundationMappingHelper.ToUseKind(loadInput.EndUse);
            var baseProfile = ResolveBaseLoadProfile(loadInput, request.OwnershipPolicy, diagnostics);
            usefulByUse[useKind] = loadInput.HourlyUsefulEnergyKWh8760;

            var emissionStage = ResolveStage(request.StageDefinitions, useKind, SystemEnergySubsystemKind.Emission);
            var distributionStage = ResolveStage(request.StageDefinitions, useKind, SystemEnergySubsystemKind.Distribution);
            var storageStage = ResolveStage(request.StageDefinitions, useKind, SystemEnergySubsystemKind.Storage);

            var emission = _emissionCalculator.Calculate(new SystemEnergyStageCalculationRequest(
                CalculationId: request.CalculationId,
                UseKind: useKind,
                InputProfileKWh: baseProfile,
                StageDefinition: emissionStage,
                TimeStepHours: request.TimeStepHours,
                OwnershipPolicy: request.OwnershipPolicy));
            var distribution = _distributionCalculator.Calculate(new SystemEnergyStageCalculationRequest(
                CalculationId: request.CalculationId,
                UseKind: useKind,
                InputProfileKWh: emission.OutputProfileKWh,
                StageDefinition: distributionStage,
                TimeStepHours: request.TimeStepHours,
                OwnershipPolicy: request.OwnershipPolicy));
            var storage = _storageCalculator.Calculate(new SystemEnergyStageCalculationRequest(
                CalculationId: request.CalculationId,
                UseKind: useKind,
                InputProfileKWh: distribution.OutputProfileKWh,
                StageDefinition: storageStage,
                TimeStepHours: request.TimeStepHours,
                OwnershipPolicy: request.OwnershipPolicy));

            var generation = _generationCalculator.Calculate(new SystemEnergyGenerationStageRequest(
                CalculationId: request.CalculationId,
                UseKind: useKind,
                LoadToGenerationProfileKWh: storage.OutputProfileKWh,
                Generators: request.GeneratorDefinitions,
                TimeStepHours: request.TimeStepHours,
                OwnershipPolicy: request.OwnershipPolicy));

            systemLoadByUse[useKind] = storage.OutputProfileKWh;
            emissionLossByUse[useKind] = emission.LossesProfileKWh;
            distributionLossByUse[useKind] = distribution.LossesProfileKWh;
            storageLossByUse[useKind] = storage.LossesProfileKWh;
            generationLossByUse[useKind] = generation.GenerationLossesProfileKWh;
            recoveredByUse[useKind] = SumProfiles(
                emission.RecoveredLossesProfileKWh,
                distribution.RecoveredLossesProfileKWh,
                storage.RecoveredLossesProfileKWh);
            auxiliaryByUse[useKind] = SumProfiles(
                emission.AuxiliaryEnergyProfileKWh,
                distribution.AuxiliaryEnergyProfileKWh,
                storage.AuxiliaryEnergyProfileKWh,
                generation.AuxiliaryEnergyProfileKWh);

            MergeCarrierProfiles(finalByCarrier, generation.FinalEnergyByCarrierKWh);

            diagnostics.AddRange(emission.Diagnostics);
            diagnostics.AddRange(distribution.Diagnostics);
            diagnostics.AddRange(storage.Diagnostics);
            diagnostics.AddRange(generation.Diagnostics);
            assumptions.AddRange(emission.Assumptions);
            assumptions.AddRange(distribution.Assumptions);
            assumptions.AddRange(storage.Assumptions);
            assumptions.AddRange(generation.Assumptions);
            warnings.AddRange(emission.Warnings);
            warnings.AddRange(distribution.Warnings);
            warnings.AddRange(storage.Warnings);
            warnings.AddRange(generation.Warnings);
        }

        var primaryByCarrier = new Dictionary<SystemEnergyCarrierKind, IReadOnlyList<double>>();
        var co2ByCarrier = new Dictionary<SystemEnergyCarrierKind, IReadOnlyList<double>>();
        foreach (var carrierEntry in finalByCarrier)
        {
            var factor = request.FactorCatalog.Entries.FirstOrDefault(entry => entry.CarrierKind == carrierEntry.Key);
            if (factor is null)
            {
                var message = $"No factor catalog entry found for carrier '{carrierEntry.Key}'.";
                if (request.StrictFactorMode)
                {
                    diagnostics.Add(CreateError("AE-SYS-FACTOR-MISSING", message));
                }
                else
                {
                    diagnostics.Add(CreateWarning("AE-SYS-FACTOR-MISSING", message + " Zero factors were applied."));
                }

                factor = new EnergyFactorCatalogEntry(
                    CarrierKind: carrierEntry.Key,
                    PrimaryEnergyFactorNonRenewable: 0.0,
                    PrimaryEnergyFactorRenewable: 0.0,
                    TotalPrimaryEnergyFactor: 0.0,
                    Co2FactorKgPerKWh: 0.0,
                    SourceLabel: "MissingFactorFallback");
            }

            var primary = carrierEntry.Value.Select(value => value * factor.TotalPrimaryEnergyFactor).ToArray();
            var co2 = carrierEntry.Value.Select(value => value * factor.Co2FactorKgPerKWh).ToArray();
            primaryByCarrier[carrierEntry.Key] = primary;
            co2ByCarrier[carrierEntry.Key] = co2;
        }

        var monthlyFinal = SystemEnergyProfileHelper.AggregateMonthly(SumProfiles(finalByCarrier.Values.ToArray()));
        var annualSummary = BuildAnnualSummary(
            usefulByUse,
            systemLoadByUse,
            emissionLossByUse,
            distributionLossByUse,
            storageLossByUse,
            generationLossByUse,
            recoveredByUse,
            auxiliaryByUse,
            finalByCarrier,
            primaryByCarrier,
            co2ByCarrier);

        assumptions.Add("Foundation chain order: intake -> emission -> distribution -> storage -> generation -> final/primary/CO2 aggregation.");
        assumptions.Add("NoDoubleCounting policy prevents duplicate upstream/system-stage loss addition.");

        return new SystemEnergyCalculationResult(
            UsefulEnergyByUseKWh: usefulByUse,
            SystemLoadByUseKWh: systemLoadByUse,
            EmissionLossesByUseKWh: emissionLossByUse,
            DistributionLossesByUseKWh: distributionLossByUse,
            StorageLossesByUseKWh: storageLossByUse,
            GenerationLossesByUseKWh: generationLossByUse,
            RecoveredLossesByUseKWh: recoveredByUse,
            AuxiliaryEnergyByUseKWh: auxiliaryByUse,
            FinalEnergyByCarrierKWh: finalByCarrier.ToDictionary(
                item => item.Key,
                item => (IReadOnlyList<double>)item.Value,
                EqualityComparer<SystemEnergyCarrierKind>.Default),
            PrimaryEnergyByCarrierKWh: primaryByCarrier,
            Co2ByCarrierKg: co2ByCarrier,
            MonthlyFinalEnergyKWh: monthlyFinal,
            AnnualSummary: annualSummary,
            Assumptions: assumptions.Distinct(StringComparer.Ordinal).ToArray(),
            Warnings: warnings.Distinct(StringComparer.Ordinal).ToArray(),
            Diagnostics: SortDiagnostics(diagnostics));
    }

    private static IReadOnlyList<StandardCalculationDiagnostic> ValidateRequest(SystemEnergyCalculationRequest request)
    {
        var diagnostics = new List<StandardCalculationDiagnostic>();

        if (request.TimeStepHours <= 0.0 || !double.IsFinite(request.TimeStepHours))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-REQUEST-TIMESTEP-INVALID",
                "System-energy calculation timestep must be positive and finite."));
        }

        if (request.LoadInputs.Count == 0)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-REQUEST-LOADS-MISSING",
                "System-energy calculation request must include at least one load input."));
        }

        foreach (var load in request.LoadInputs)
        {
            if (load.HourlyUsefulEnergyKWh8760 is null || load.HourlyUsefulEnergyKWh8760.Count == 0)
            {
                diagnostics.Add(CreateError(
                    "AE-SYS-REQUEST-LOAD-PROFILE-MISSING",
                    $"Load '{load.LoadId}' is missing useful profile."));
                continue;
            }

            if (load.HourlyUsefulEnergyKWh8760.Count != 8760 && load.HourlyUsefulEnergyKWh8760.Count != 12)
            {
                diagnostics.Add(CreateError(
                    "AE-SYS-REQUEST-LOAD-PROFILE-LENGTH-MISMATCH",
                    $"Load '{load.LoadId}' profile must contain 8760 hourly or 12 monthly values."));
            }

            if (load.HourlyUsefulEnergyKWh8760.Any(value => !double.IsFinite(value) || value < 0.0))
            {
                diagnostics.Add(CreateError(
                    "AE-SYS-REQUEST-LOAD-NEGATIVE",
                    $"Load '{load.LoadId}' useful profile must be finite and non-negative."));
            }
        }

        var duplicateStages = request.StageDefinitions
            .GroupBy(stage => new { stage.SubsystemKind, stage.AppliesToUse, stage.Priority })
            .Where(group => group.Count() > 1)
            .ToArray();
        foreach (var duplicate in duplicateStages)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-STAGE-DUPLICATE-CONFLICT",
                $"Duplicate stage conflict for subsystem '{duplicate.Key.SubsystemKind}' use '{duplicate.Key.AppliesToUse}' and priority '{duplicate.Key.Priority}'."));
        }

        foreach (var generator in request.GeneratorDefinitions)
        {
            if (generator.CarrierKind == SystemEnergyCarrierKind.Unknown)
            {
                diagnostics.Add(CreateError(
                    "AE-SYS-GEN-CARRIER-MISSING",
                    $"Generator '{generator.GeneratorId}' must define a known carrier."));
            }

            if (generator.Efficiency is <= 0.0)
            {
                diagnostics.Add(CreateError(
                    "AE-SYS-GEN-EFFICIENCY-INVALID",
                    $"Generator '{generator.GeneratorId}' efficiency must be greater than zero when provided."));
            }

            if (generator.Cop is <= 0.0)
            {
                diagnostics.Add(CreateError(
                    "AE-SYS-GEN-COP-INVALID",
                    $"Generator '{generator.GeneratorId}' COP must be greater than zero when provided."));
            }
        }

        if (request.OwnershipPolicy == SystemEnergyLossOwnershipPolicy.UpstreamOwnsLosses)
        {
            var hasDhwStorageStages = request.StageDefinitions.Any(stage =>
                stage.SubsystemKind is SystemEnergySubsystemKind.Storage or SystemEnergySubsystemKind.Distribution &&
                stage.AppliesToUse is SystemEnergyUseKind.DomesticHotWater or SystemEnergyUseKind.Generic);
            if (hasDhwStorageStages)
            {
                diagnostics.Add(CreateWarning(
                    "AE-SYS-OWNERSHIP-DOUBLE-COUNTING-RISK",
                    "UpstreamOwnsLosses policy with DHW storage/distribution stages may double count losses unless stages are skipped."));
            }
        }

        return diagnostics;
    }

    private static IReadOnlyList<double> ResolveBaseLoadProfile(
        SystemEnergyUsefulLoadInput loadInput,
        SystemEnergyLossOwnershipPolicy policy,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (policy is SystemEnergyLossOwnershipPolicy.UpstreamOwnsLosses or SystemEnergyLossOwnershipPolicy.NoDoubleCounting &&
            loadInput.EndUse == SystemEnergyEndUse.DomesticHotWater &&
            loadInput.HourlySystemLoadKWh8760 is { Count: > 0 } systemLoad)
        {
            diagnostics.Add(CreateInfo(
                "AE-SYS-OWNERSHIP-UPSTREAM-SYSTEM-LOAD-USED",
                $"Load '{loadInput.LoadId}' uses upstream DHW system-load lane to avoid loss double counting."));
            return systemLoad;
        }

        return loadInput.HourlyUsefulEnergyKWh8760;
    }

    private static SystemEnergyStageDefinition ResolveStage(
        IReadOnlyList<SystemEnergyStageDefinition> stages,
        SystemEnergyUseKind useKind,
        SystemEnergySubsystemKind subsystemKind)
    {
        var match = stages
            .Where(stage => stage.SubsystemKind == subsystemKind &&
                            (stage.AppliesToUse == useKind || stage.AppliesToUse == SystemEnergyUseKind.Generic))
            .OrderBy(stage => stage.Priority)
            .ThenBy(stage => stage.StageId, StringComparer.Ordinal)
            .FirstOrDefault();

        return match ?? new SystemEnergyStageDefinition(
            StageId: $"{subsystemKind}-{useKind}-Default",
            SubsystemKind: subsystemKind,
            AppliesToUse: useKind,
            Efficiency: 1.0,
            LossFraction: 0.0,
            FixedLossProfile: null,
            AuxiliaryEnergyProfile: null,
            RecoveredLossFraction: 0.0,
            TargetCarrier: SystemEnergyCarrierKind.Unknown,
            CalculationMode: SystemEnergyModuleCalculationMode.Disabled,
            VerboseDiagnostics: false);
    }

    private static void MergeCarrierProfiles(
        IDictionary<SystemEnergyCarrierKind, double[]> totals,
        IReadOnlyDictionary<SystemEnergyCarrierKind, IReadOnlyList<double>> values)
    {
        foreach (var entry in values)
        {
            if (!totals.TryGetValue(entry.Key, out var total))
            {
                total = new double[entry.Value.Count];
                totals[entry.Key] = total;
            }

            for (var index = 0; index < entry.Value.Count; index++)
                total[index] += entry.Value[index];
        }
    }

    private static SystemEnergyAnnualSummary BuildAnnualSummary(
        IReadOnlyDictionary<SystemEnergyUseKind, IReadOnlyList<double>> usefulByUse,
        IReadOnlyDictionary<SystemEnergyUseKind, IReadOnlyList<double>> systemByUse,
        IReadOnlyDictionary<SystemEnergyUseKind, IReadOnlyList<double>> emissionLossByUse,
        IReadOnlyDictionary<SystemEnergyUseKind, IReadOnlyList<double>> distributionLossByUse,
        IReadOnlyDictionary<SystemEnergyUseKind, IReadOnlyList<double>> storageLossByUse,
        IReadOnlyDictionary<SystemEnergyUseKind, IReadOnlyList<double>> generationLossByUse,
        IReadOnlyDictionary<SystemEnergyUseKind, IReadOnlyList<double>> recoveredByUse,
        IReadOnlyDictionary<SystemEnergyUseKind, IReadOnlyList<double>> auxiliaryByUse,
        IReadOnlyDictionary<SystemEnergyCarrierKind, double[]> finalByCarrier,
        IReadOnlyDictionary<SystemEnergyCarrierKind, IReadOnlyList<double>> primaryByCarrier,
        IReadOnlyDictionary<SystemEnergyCarrierKind, IReadOnlyList<double>> co2ByCarrier)
    {
        return new SystemEnergyAnnualSummary(
            UsefulEnergyKWh: usefulByUse.Values.Sum(profile => profile.Sum()),
            SystemLoadKWh: systemByUse.Values.Sum(profile => profile.Sum()),
            EmissionLossesKWh: emissionLossByUse.Values.Sum(profile => profile.Sum()),
            DistributionLossesKWh: distributionLossByUse.Values.Sum(profile => profile.Sum()),
            StorageLossesKWh: storageLossByUse.Values.Sum(profile => profile.Sum()),
            GenerationLossesKWh: generationLossByUse.Values.Sum(profile => profile.Sum()),
            RecoveredLossesKWh: recoveredByUse.Values.Sum(profile => profile.Sum()),
            AuxiliaryEnergyKWh: auxiliaryByUse.Values.Sum(profile => profile.Sum()),
            FinalEnergyKWh: finalByCarrier.Values.Sum(profile => profile.Sum()),
            PrimaryEnergyKWh: primaryByCarrier.Values.Sum(profile => profile.Sum()),
            Co2Kg: co2ByCarrier.Values.Sum(profile => profile.Sum()));
    }

    private static double[] SumProfiles(params IReadOnlyList<double>[] profiles)
    {
        if (profiles.Length == 0)
            return [];

        var size = profiles.Max(profile => profile.Count);
        var result = new double[size];
        foreach (var profile in profiles)
        {
            for (var index = 0; index < profile.Count; index++)
                result[index] += profile[index];
        }

        return result;
    }

    private static IReadOnlyList<StandardCalculationDiagnostic> SortDiagnostics(
        IEnumerable<StandardCalculationDiagnostic> diagnostics) =>
        diagnostics
            .OrderByDescending(item => item.Severity)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.Message, StringComparer.Ordinal)
            .ToArray();

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Error,
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

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            Source);
}
