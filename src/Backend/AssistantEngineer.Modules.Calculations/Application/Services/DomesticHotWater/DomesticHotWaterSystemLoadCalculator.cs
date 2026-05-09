using AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

public sealed class DomesticHotWaterSystemLoadCalculator : IDomesticHotWaterSystemLoadCalculator
{
    private static readonly IReadOnlyList<string> RequiredForbiddenClaims =
    [
        "Full ISO compliance",
        "Full EN compliance",
        "StandardReference equivalence",
        "EnergyPlus comparison workflow",
        "ASHRAE 140 / BESTEST-style validation anchor"
    ];

    private static readonly int[] DaysPerMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    private readonly IDomesticHotWaterSystemLossInputValidator _validator;
    private readonly IDomesticHotWaterStorageLossCalculator _storageLossCalculator;
    private readonly IDomesticHotWaterDistributionLossCalculator _distributionLossCalculator;
    private readonly IDomesticHotWaterCirculationLossCalculator _circulationLossCalculator;
    private readonly IDomesticHotWaterEn15316HandoffBuilder _handoffBuilder;
    private readonly IDomesticHotWaterLossCalculator _lossCalculator;
    private readonly IStandardCalculationDisclosureFactory _disclosureFactory;

    public DomesticHotWaterSystemLoadCalculator(
        IDomesticHotWaterSystemLossInputValidator validator,
        IDomesticHotWaterStorageLossCalculator storageLossCalculator,
        IDomesticHotWaterDistributionLossCalculator distributionLossCalculator,
        IDomesticHotWaterCirculationLossCalculator circulationLossCalculator,
        IDomesticHotWaterEn15316HandoffBuilder handoffBuilder,
        IDomesticHotWaterLossCalculator lossCalculator,
        IStandardCalculationDisclosureFactory disclosureFactory)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _storageLossCalculator = storageLossCalculator ?? throw new ArgumentNullException(nameof(storageLossCalculator));
        _distributionLossCalculator = distributionLossCalculator ?? throw new ArgumentNullException(nameof(distributionLossCalculator));
        _circulationLossCalculator = circulationLossCalculator ?? throw new ArgumentNullException(nameof(circulationLossCalculator));
        _handoffBuilder = handoffBuilder ?? throw new ArgumentNullException(nameof(handoffBuilder));
        _lossCalculator = lossCalculator ?? throw new ArgumentNullException(nameof(lossCalculator));
        _disclosureFactory = disclosureFactory ?? throw new ArgumentNullException(nameof(disclosureFactory));
    }

    public DomesticHotWaterSystemLoadResult Calculate(DomesticHotWaterSystemLossInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        var validation = _validator.Validate(input);
        diagnostics.AddRange(validation.Diagnostics);

        var defaultAmbient = input.DefaultAmbientTemperatureCelsius is { } ambient && double.IsFinite(ambient)
            ? ambient
            : 20.0;
        var defaultRecoverableFraction = input.DefaultRecoverableFraction is { } recoverable &&
                                         double.IsFinite(recoverable)
            ? Math.Clamp(recoverable, 0.0, 1.0)
            : 0.0;

        var storage = _storageLossCalculator.Calculate(
            input.UsefulDemand,
            input.Storage,
            defaultAmbient,
            defaultRecoverableFraction);
        var distribution = _distributionLossCalculator.Calculate(
            input.UsefulDemand,
            input.Distribution,
            defaultAmbient,
            defaultRecoverableFraction);
        var circulationComponents = _circulationLossCalculator.Calculate(
            input.UsefulDemand,
            input.Circulation,
            defaultAmbient,
            defaultRecoverableFraction);

        var components = new List<DomesticHotWaterLossComponentResult>
        {
            storage,
            distribution
        };
        components.AddRange(circulationComponents);

        diagnostics.AddRange(storage.Diagnostics);
        diagnostics.AddRange(distribution.Diagnostics);
        diagnostics.AddRange(circulationComponents.SelectMany(component => component.Diagnostics));

        var circulationThermal = components.FirstOrDefault(component =>
            component.ComponentKind == DomesticHotWaterLossComponentKind.Circulation)
            ?? DomesticHotWaterLossProfileHelper.CreateZeroComponent(
                DomesticHotWaterLossComponentKind.Circulation,
                DomesticHotWaterLossRecoveryMode.NonRecoverable,
                "AE-DHW-CIRCULATION-LOSS-NOT-CALCULABLE",
                "Circulation thermal loss component was not available and defaulted to zero.",
                "DomesticHotWaterSystemLoadCalculator");
        var auxiliary = components.FirstOrDefault(component =>
            component.ComponentKind == DomesticHotWaterLossComponentKind.AuxiliaryElectricity)
            ?? DomesticHotWaterLossProfileHelper.CreateZeroComponent(
                DomesticHotWaterLossComponentKind.AuxiliaryElectricity,
                DomesticHotWaterLossRecoveryMode.NonRecoverable,
                "AE-DHW-CIRCULATION-PUMP-AUXILIARY-CALCULATED",
                "Auxiliary electricity component was not available and defaulted to zero.",
                "DomesticHotWaterSystemLoadCalculator");

        double[] hourlySystemHeatRequirement;
        double[] hourlyRecoverableLoss;
        double[] hourlyNonRecoverableLoss;
        if (input.LossOwnershipPolicy == DomesticHotWaterLossOwnershipPolicy.SystemEnergyOwnLosses)
        {
            hourlySystemHeatRequirement = input.UsefulDemand.HourlyUsefulEnergyKWh8760.ToArray();
            hourlyRecoverableLoss = new double[8760];
            hourlyNonRecoverableLoss = new double[8760];
            diagnostics.Add(CreateInfo(
                "AE-DHW-LOSS-OWNERSHIP-SYSTEM-ENERGY",
                "DHW system heat lane uses useful demand only because technical losses are owned by system-energy chain."));
        }
        else
        {
            hourlySystemHeatRequirement = input.UsefulDemand.HourlyUsefulEnergyKWh8760
                .Select((useful, index) =>
                    useful +
                    storage.HourlyLossKWh8760[index] +
                    distribution.HourlyLossKWh8760[index] +
                    circulationThermal.HourlyLossKWh8760[index])
                .ToArray();

            hourlyRecoverableLoss = storage.HourlyRecoverableLossKWh8760
                .Select((value, index) =>
                    value +
                    distribution.HourlyRecoverableLossKWh8760[index] +
                    circulationThermal.HourlyRecoverableLossKWh8760[index])
                .ToArray();
            hourlyNonRecoverableLoss = storage.HourlyNonRecoverableLossKWh8760
                .Select((value, index) =>
                    value +
                    distribution.HourlyNonRecoverableLossKWh8760[index] +
                    circulationThermal.HourlyNonRecoverableLossKWh8760[index])
                .ToArray();

            if (input.LossOwnershipPolicy == DomesticHotWaterLossOwnershipPolicy.NoDoubleCounting)
            {
                diagnostics.Add(CreateInfo(
                    "AE-DHW-LOSS-OWNERSHIP-NO-DOUBLE-COUNTING",
                    "DHW technical losses are included in DHW system-load lane under no-double-counting ownership policy."));
            }
        }

        var monthlySystemHeat = BuildMonthlyFromHourly(hourlySystemHeatRequirement);

        diagnostics.Add(CreateInfo(
            "AE-DHW-SYSTEM-HOURLY-PROFILE-CALCULATED",
            "Hourly DHW system heat-requirement profile was calculated."));
        diagnostics.Add(CreateInfo(
            "AE-DHW-SYSTEM-MONTHLY-AGGREGATED",
            "Monthly DHW system heat-requirement values were aggregated."));
        diagnostics.Add(CreateInfo(
            "AE-DHW-SYSTEM-LOAD-CALCULATED",
            "DHW system load was calculated from useful demand and component losses."));

        var disclosure = MergeDisclosure(
            _disclosureFactory.CreateDomesticHotWaterIso12831Disclosure(),
            input.DisclosureOverride,
            diagnostics);

        var interimResult = new DomesticHotWaterSystemLoadResult(
            CalculationId: input.CalculationId,
            BuildingId: input.UsefulDemand.BuildingId,
            ZoneId: input.UsefulDemand.ZoneId,
            RoomId: input.UsefulDemand.RoomId,
            UsefulDemand: input.UsefulDemand,
            LossComponents: components,
            AnnualUsefulEnergyKWh: input.UsefulDemand.AnnualUsefulEnergyKWh,
            AnnualStorageLossKWh: storage.AnnualLossKWh,
            AnnualDistributionLossKWh: distribution.AnnualLossKWh,
            AnnualCirculationLossKWh: circulationThermal.AnnualLossKWh,
            AnnualAuxiliaryElectricityKWh: auxiliary.AnnualLossKWh,
            AnnualRecoverableLossKWh: hourlyRecoverableLoss.Sum(),
            AnnualNonRecoverableLossKWh: hourlyNonRecoverableLoss.Sum(),
            AnnualSystemHeatRequirementKWh: hourlySystemHeatRequirement.Sum(),
            MonthlySystemHeatRequirementKWh: monthlySystemHeat,
            HourlySystemHeatRequirementKWh8760: hourlySystemHeatRequirement,
            HourlyRecoverableLossKWh8760: hourlyRecoverableLoss,
            HourlyNonRecoverableLossKWh8760: hourlyNonRecoverableLoss,
            HourlyAuxiliaryElectricityKWh8760: auxiliary.HourlyLossKWh8760,
            En15316Handoff: new DomesticHotWaterEn15316Handoff(
                CalculationId: input.CalculationId,
                EndUse: "DomesticHotWater",
                UsefulEnergySource: "Interim",
                AnnualUsefulDhwEnergyKWh: input.UsefulDemand.AnnualUsefulEnergyKWh,
                AnnualDhwSystemHeatRequirementKWh: hourlySystemHeatRequirement.Sum(),
                AnnualDhwAuxiliaryElectricityKWh: auxiliary.AnnualLossKWh,
                HourlyUsefulDhwEnergyKWh8760: input.UsefulDemand.HourlyUsefulEnergyKWh8760,
                HourlyDhwSystemHeatRequirementKWh8760: hourlySystemHeatRequirement,
                HourlyDhwAuxiliaryElectricityKWh8760: auxiliary.HourlyLossKWh8760,
                HourlyRecoverableLossKWh8760: hourlyRecoverableLoss,
                HourlyNonRecoverableLossKWh8760: hourlyNonRecoverableLoss,
                Diagnostics: [],
                LossOwnershipPolicy: input.LossOwnershipPolicy),
            Disclosure: disclosure,
            Diagnostics: diagnostics);

        var handoff = _handoffBuilder.Build(interimResult);
        diagnostics.AddRange(handoff.Diagnostics);
        diagnostics.Add(CreateInfo(
            "AE-DHW-SYSTEM-EN15316-HANDOFF-BUILT",
            "EN15316 handoff contract was built from DHW system-load result."));

        return interimResult with
        {
            En15316Handoff = handoff,
            Diagnostics = diagnostics
        };
    }

    public DomesticHotWaterSystemLoadFoundationResult Calculate(DomesticHotWaterSystemLoadRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.UsefulDemandProfileKWh);
        ArgumentNullException.ThrowIfNull(request.LossDefinition);

        var assumptions = new List<string>();
        var warnings = new List<string>();
        var diagnostics = new List<StandardCalculationDiagnostic>();

        if (request.UsefulDemandProfileKWh.Any(value => !double.IsFinite(value) || value < 0.0))
        {
            diagnostics.Add(CreateWarning(
                "AE-DHW-SYSTEM-USEFUL-PROFILE-INVALID",
                "Useful demand profile contains invalid values; invalid values were clamped to zero."));
        }

        var useful = request.UsefulDemandProfileKWh
            .Select(value => double.IsFinite(value) && value > 0.0 ? value : 0.0)
            .ToArray();
        var setpointProfile = request.HotWaterSetpointProfileCelsius;

        var losses = _lossCalculator.Calculate(useful, request.LossDefinition, setpointProfile);
        diagnostics.AddRange(losses.Diagnostics);
        assumptions.AddRange(losses.Assumptions);
        warnings.AddRange(losses.Warnings);

        var system = new double[useful.Length];
        for (var index = 0; index < useful.Length; index++)
        {
            var raw = useful[index] +
                      losses.StorageLossesProfileKWh[index] +
                      losses.DistributionLossesProfileKWh[index] +
                      losses.CirculationLossesProfileKWh[index] -
                      losses.RecoveredLossesProfileKWh[index];
            system[index] = Math.Max(0.0, raw);
        }

        assumptions.Add("System load convention: useful + storage + distribution + circulation - recovered.");
        assumptions.Add("Auxiliary energy profile is tracked separately and not added to thermal system load.");

        var monthly = BuildMonthlyFromProfile(system);
        var summary = new DomesticHotWaterSystemLoadAnnualSummary(
            UsefulEnergyKWh: useful.Sum(),
            StorageLossesKWh: losses.StorageLossesProfileKWh.Sum(),
            DistributionLossesKWh: losses.DistributionLossesProfileKWh.Sum(),
            CirculationLossesKWh: losses.CirculationLossesProfileKWh.Sum(),
            RecoveredLossesKWh: losses.RecoveredLossesProfileKWh.Sum(),
            AuxiliaryEnergyKWh: losses.AuxiliaryEnergyProfileKWh.Sum(),
            SystemLoadKWh: system.Sum());

        return new DomesticHotWaterSystemLoadFoundationResult(
            UsefulEnergyProfileKWh: useful,
            StorageLossesProfileKWh: losses.StorageLossesProfileKWh,
            DistributionLossesProfileKWh: losses.DistributionLossesProfileKWh,
            CirculationLossesProfileKWh: losses.CirculationLossesProfileKWh,
            RecoveredLossesProfileKWh: losses.RecoveredLossesProfileKWh,
            AuxiliaryEnergyProfileKWh: losses.AuxiliaryEnergyProfileKWh,
            SystemLoadProfileKWh: system,
            MonthlySystemLoadKWh: monthly,
            AnnualSummary: summary,
            Assumptions: assumptions.ToArray(),
            Warnings: warnings.ToArray(),
            Diagnostics: SortDiagnostics(diagnostics));
    }

    private static IReadOnlyList<double> BuildMonthlyFromHourly(IReadOnlyList<double> hourlyValues)
    {
        var monthly = new double[12];
        var offset = 0;
        for (var month = 0; month < 12; month++)
        {
            var hours = DaysPerMonth[month] * 24;
            monthly[month] = hourlyValues.Skip(offset).Take(hours).Sum();
            offset += hours;
        }

        return monthly;
    }

    private static IReadOnlyList<double> BuildMonthlyFromProfile(IReadOnlyList<double> values)
    {
        if (values.Count == 12)
            return values.ToArray();

        if (values.Count != 8760)
            return Enumerable.Repeat(0.0, 12).ToArray();

        return BuildMonthlyFromHourly(values);
    }

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
                "AE-DHW-DISCLOSURE-OVERRIDE-SANITIZED",
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
        DomesticHotWaterDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            StandardCalculationStage.DomesticHotWater,
            "DomesticHotWaterSystemLoadCalculator");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        DomesticHotWaterDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.DomesticHotWater,
            "DomesticHotWaterSystemLoadCalculator");

    private static IReadOnlyList<StandardCalculationDiagnostic> SortDiagnostics(
        IEnumerable<StandardCalculationDiagnostic> diagnostics) =>
        diagnostics
            .OrderByDescending(item => item.Severity)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.Message, StringComparer.Ordinal)
            .ToArray();
}
