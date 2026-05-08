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
        "pyBuildingEnergy parity",
        "EnergyPlus parity",
        "ASHRAE 140 validation"
    ];

    private static readonly int[] DaysPerMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    private readonly IDomesticHotWaterSystemLossInputValidator _validator;
    private readonly IDomesticHotWaterStorageLossCalculator _storageLossCalculator;
    private readonly IDomesticHotWaterDistributionLossCalculator _distributionLossCalculator;
    private readonly IDomesticHotWaterCirculationLossCalculator _circulationLossCalculator;
    private readonly IDomesticHotWaterEn15316HandoffBuilder _handoffBuilder;
    private readonly IStandardCalculationDisclosureFactory _disclosureFactory;

    public DomesticHotWaterSystemLoadCalculator(
        IDomesticHotWaterSystemLossInputValidator validator,
        IDomesticHotWaterStorageLossCalculator storageLossCalculator,
        IDomesticHotWaterDistributionLossCalculator distributionLossCalculator,
        IDomesticHotWaterCirculationLossCalculator circulationLossCalculator,
        IDomesticHotWaterEn15316HandoffBuilder handoffBuilder,
        IStandardCalculationDisclosureFactory disclosureFactory)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _storageLossCalculator = storageLossCalculator ?? throw new ArgumentNullException(nameof(storageLossCalculator));
        _distributionLossCalculator = distributionLossCalculator ?? throw new ArgumentNullException(nameof(distributionLossCalculator));
        _circulationLossCalculator = circulationLossCalculator ?? throw new ArgumentNullException(nameof(circulationLossCalculator));
        _handoffBuilder = handoffBuilder ?? throw new ArgumentNullException(nameof(handoffBuilder));
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

        var hourlySystemHeatRequirement = input.UsefulDemand.HourlyUsefulEnergyKWh8760
            .Select((useful, index) =>
                useful +
                storage.HourlyLossKWh8760[index] +
                distribution.HourlyLossKWh8760[index] +
                circulationThermal.HourlyLossKWh8760[index])
            .ToArray();

        var hourlyRecoverableLoss = storage.HourlyRecoverableLossKWh8760
            .Select((value, index) =>
                value +
                distribution.HourlyRecoverableLossKWh8760[index] +
                circulationThermal.HourlyRecoverableLossKWh8760[index])
            .ToArray();
        var hourlyNonRecoverableLoss = storage.HourlyNonRecoverableLossKWh8760
            .Select((value, index) =>
                value +
                distribution.HourlyNonRecoverableLossKWh8760[index] +
                circulationThermal.HourlyNonRecoverableLossKWh8760[index])
            .ToArray();

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
                Diagnostics: []),
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
}
