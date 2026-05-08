using AssistantEngineer.Modules.Calculations.Application.Abstractions.Standards;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class SystemEnergyModuleChainCalculator : ISystemEnergyModuleChainCalculator
{
    private static readonly IReadOnlyList<string> RequiredForbiddenClaims =
    [
        "Full ISO compliance",
        "Full EN compliance",
        "StandardReference equivalence",
        "EnergyPlus comparison workflow",
        "ASHRAE 140 / BESTEST-style validation anchor"
    ];

    private const string Source = "SystemEnergyModuleChainCalculator";
    private readonly ISystemEnergyModuleChainInputValidator _inputValidator;
    private readonly ISystemEnergyModuleCalculator _moduleCalculator;
    private readonly ISystemEnergyGenerationHandoffBuilder _generationHandoffBuilder;
    private readonly IStandardCalculationDisclosureFactory _disclosureFactory;

    public SystemEnergyModuleChainCalculator(
        ISystemEnergyModuleChainInputValidator inputValidator,
        ISystemEnergyModuleCalculator moduleCalculator,
        ISystemEnergyGenerationHandoffBuilder generationHandoffBuilder,
        IStandardCalculationDisclosureFactory disclosureFactory)
    {
        _inputValidator = inputValidator ?? throw new ArgumentNullException(nameof(inputValidator));
        _moduleCalculator = moduleCalculator ?? throw new ArgumentNullException(nameof(moduleCalculator));
        _generationHandoffBuilder = generationHandoffBuilder ?? throw new ArgumentNullException(nameof(generationHandoffBuilder));
        _disclosureFactory = disclosureFactory ?? throw new ArgumentNullException(nameof(disclosureFactory));
    }

    public SystemEnergyModuleChainResult Calculate(SystemEnergyModuleChainInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        var validation = _inputValidator.Validate(input);
        diagnostics.AddRange(validation.Diagnostics);

        var usefulLoadsByEndUse = input.UsefulLoadSet.UsefulLoads
            .GroupBy(load => load.EndUse)
            .ToArray();

        var endUseResults = new List<SystemEnergyEndUseChainResult>(usefulLoadsByEndUse.Length);

        foreach (var endUseGroup in usefulLoadsByEndUse)
        {
            diagnostics.Add(CreateInfo(
                "AE-SYS-ENDUSE-LOADS-GROUPED",
                $"Grouped useful loads for end use '{endUseGroup.Key}'."));

            var usefulHourly = SystemEnergyProfileHelper.SumProfiles(endUseGroup.Select(load => load.HourlyUsefulEnergyKWh8760));
            var currentHourly = usefulHourly.ToArray();
            var moduleResults = new List<SystemEnergyModuleResult>();

            var modulesForEndUse = input.Modules
                .Where(module => module.EndUse == endUseGroup.Key)
                .OrderBy(module => GetModuleOrder(module.ModuleKind))
                .ThenBy(module => module.ModuleId, StringComparer.Ordinal)
                .ToArray();

            foreach (var module in modulesForEndUse)
            {
                var effectiveModule = module;
                if (module.ModuleKind == SystemEnergyModuleKind.Generation)
                {
                    effectiveModule = module with
                    {
                        CalculationMode = SystemEnergyModuleCalculationMode.HandoffOnly
                    };

                    diagnostics.Add(CreateInfo(
                        "AE-SYS-GENERATION-MODULE-DEFERRED",
                        $"Generation module '{module.ModuleId}' was deferred and treated as handoff-only in this stage."));
                }

                var moduleResult = _moduleCalculator.Calculate(effectiveModule, currentHourly);
                moduleResults.Add(moduleResult);
                diagnostics.AddRange(moduleResult.Diagnostics);
                currentHourly = moduleResult.HourlyOutputEnergyKWh8760.ToArray();
            }

            var hourlyRecoverableLoss = SystemEnergyProfileHelper.SumProfiles(moduleResults.Select(result => result.HourlyRecoverableLossKWh8760));
            var hourlyNonRecoverableLoss = SystemEnergyProfileHelper.SumProfiles(moduleResults.Select(result => result.HourlyNonRecoverableLossKWh8760));
            var monthlyUseful = SystemEnergyProfileHelper.AggregateMonthly(usefulHourly);
            var monthlySystemLoad = SystemEnergyProfileHelper.AggregateMonthly(currentHourly);

            endUseResults.Add(new SystemEnergyEndUseChainResult(
                EndUse: endUseGroup.Key,
                Modules: moduleResults,
                HourlyUsefulEnergyKWh8760: usefulHourly,
                HourlySystemLoadBeforeGenerationKWh8760: currentHourly,
                HourlyRecoverableLossKWh8760: hourlyRecoverableLoss,
                HourlyNonRecoverableLossKWh8760: hourlyNonRecoverableLoss,
                AnnualUsefulEnergyKWh: usefulHourly.Sum(),
                AnnualSystemLoadBeforeGenerationKWh: currentHourly.Sum(),
                AnnualRecoverableLossKWh: hourlyRecoverableLoss.Sum(),
                AnnualNonRecoverableLossKWh: hourlyNonRecoverableLoss.Sum(),
                MonthlyUsefulEnergyKWh: monthlyUseful,
                MonthlySystemLoadBeforeGenerationKWh: monthlySystemLoad,
                Diagnostics: moduleResults.SelectMany(result => result.Diagnostics).ToArray()));
        }

        var hourlyTotalUseful = SystemEnergyProfileHelper.SumProfiles(endUseResults.Select(result => result.HourlyUsefulEnergyKWh8760));
        var hourlyTotalSystemLoad = SystemEnergyProfileHelper.SumProfiles(endUseResults.Select(result => result.HourlySystemLoadBeforeGenerationKWh8760));
        var hourlyTotalRecoverable = SystemEnergyProfileHelper.SumProfiles(endUseResults.Select(result => result.HourlyRecoverableLossKWh8760));
        var hourlyTotalNonRecoverable = SystemEnergyProfileHelper.SumProfiles(endUseResults.Select(result => result.HourlyNonRecoverableLossKWh8760));
        var hourlyAuxiliary = SystemEnergyProfileHelper.SumProfiles(input.UsefulLoadSet.AuxiliaryLoads.Select(load => load.HourlyAuxiliaryEnergyKWh8760));

        var monthlyTotalUseful = SystemEnergyProfileHelper.AggregateMonthly(hourlyTotalUseful);
        var monthlyTotalSystemLoad = SystemEnergyProfileHelper.AggregateMonthly(hourlyTotalSystemLoad);

        diagnostics.Add(CreateInfo("AE-SYS-MODULE-CHAIN-CALCULATED", "System-energy module chain was calculated for all end uses."));
        diagnostics.Add(CreateInfo("AE-SYS-TOTALS-AGGREGATED", "System-energy hourly, monthly and annual totals were aggregated."));

        var disclosure = MergeDisclosure(
            _disclosureFactory.CreateSystemEnergyEn15316Disclosure(),
            input.DisclosureOverride,
            diagnostics);

        var interimResult = new SystemEnergyModuleChainResult(
            CalculationId: input.CalculationId,
            EndUses: endUseResults,
            AuxiliaryLoads: input.UsefulLoadSet.AuxiliaryLoads,
            HourlyTotalUsefulEnergyKWh8760: hourlyTotalUseful,
            HourlyTotalSystemLoadBeforeGenerationKWh8760: hourlyTotalSystemLoad,
            HourlyTotalRecoverableLossKWh8760: hourlyTotalRecoverable,
            HourlyTotalNonRecoverableLossKWh8760: hourlyTotalNonRecoverable,
            HourlyTotalAuxiliaryEnergyKWh8760: hourlyAuxiliary,
            AnnualTotalUsefulEnergyKWh: hourlyTotalUseful.Sum(),
            AnnualTotalSystemLoadBeforeGenerationKWh: hourlyTotalSystemLoad.Sum(),
            AnnualTotalRecoverableLossKWh: hourlyTotalRecoverable.Sum(),
            AnnualTotalNonRecoverableLossKWh: hourlyTotalNonRecoverable.Sum(),
            AnnualTotalAuxiliaryEnergyKWh: hourlyAuxiliary.Sum(),
            MonthlyTotalUsefulEnergyKWh: monthlyTotalUseful,
            MonthlyTotalSystemLoadBeforeGenerationKWh: monthlyTotalSystemLoad,
            GenerationHandoff: new SystemEnergyGenerationHandoff(
                CalculationId: input.CalculationId,
                HourlySystemLoadBeforeGenerationByEndUseKWh8760: new Dictionary<SystemEnergyEndUse, IReadOnlyList<double>>(),
                AnnualSystemLoadBeforeGenerationByEndUseKWh: new Dictionary<SystemEnergyEndUse, double>(),
                HourlyRecoverableLossByEndUseKWh8760: new Dictionary<SystemEnergyEndUse, IReadOnlyList<double>>(),
                HourlyNonRecoverableLossByEndUseKWh8760: new Dictionary<SystemEnergyEndUse, IReadOnlyList<double>>(),
                AuxiliaryLoads: input.UsefulLoadSet.AuxiliaryLoads,
                Diagnostics: []),
            Disclosure: disclosure,
            Diagnostics: diagnostics);

        var handoff = _generationHandoffBuilder.Build(interimResult);
        diagnostics.AddRange(handoff.Diagnostics);
        diagnostics.Add(CreateInfo("AE-SYS-GENERATION-HANDOFF-BUILT", "Generation handoff contract was built from module-chain result."));

        return interimResult with
        {
            GenerationHandoff = handoff,
            Diagnostics = diagnostics
        };
    }

    private static int GetModuleOrder(SystemEnergyModuleKind moduleKind) =>
        moduleKind switch
        {
            SystemEnergyModuleKind.UsefulDemand => 1,
            SystemEnergyModuleKind.Emission => 2,
            SystemEnergyModuleKind.Control => 3,
            SystemEnergyModuleKind.Distribution => 4,
            SystemEnergyModuleKind.Storage => 5,
            SystemEnergyModuleKind.Generation => 6,
            SystemEnergyModuleKind.Auxiliary => 7,
            SystemEnergyModuleKind.Recovery => 8,
            SystemEnergyModuleKind.Handoff => 9,
            _ => 10
        };

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
