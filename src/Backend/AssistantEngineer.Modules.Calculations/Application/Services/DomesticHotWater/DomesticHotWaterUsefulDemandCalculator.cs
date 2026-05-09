using AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

public sealed class DomesticHotWaterUsefulDemandCalculator : IDomesticHotWaterUsefulDemandCalculator
{
    private const double DefaultWaterDensityKgPerLiter = 0.997;
    private const double DefaultWaterSpecificHeatJPerKgKelvin = 4186.0;

    private static readonly IReadOnlyList<string> RequiredForbiddenClaims =
    [
        "Full ISO compliance",
        "Full EN compliance",
        "StandardReference equivalence",
        "EnergyPlus comparison workflow",
        "ASHRAE 140 / BESTEST-style validation anchor"
    ];

    private static readonly int[] DaysPerMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    private readonly IDomesticHotWaterDemandInputValidator _validator;
    private readonly IDomesticHotWaterDemandBasisCalculator _basisCalculator;
    private readonly IDomesticHotWaterDrawProfileBuilder _drawProfileBuilder;
    private readonly IStandardCalculationDisclosureFactory _disclosureFactory;

    public DomesticHotWaterUsefulDemandCalculator(
        IDomesticHotWaterDemandInputValidator validator,
        IDomesticHotWaterDemandBasisCalculator basisCalculator,
        IDomesticHotWaterDrawProfileBuilder drawProfileBuilder,
        IStandardCalculationDisclosureFactory disclosureFactory)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _basisCalculator = basisCalculator ?? throw new ArgumentNullException(nameof(basisCalculator));
        _drawProfileBuilder = drawProfileBuilder ?? throw new ArgumentNullException(nameof(drawProfileBuilder));
        _disclosureFactory = disclosureFactory ?? throw new ArgumentNullException(nameof(disclosureFactory));
    }

    public DomesticHotWaterUsefulDemandResult Calculate(DomesticHotWaterUsefulDemandInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var diagnostics = new List<StandardCalculationDiagnostic>();

        var validation = _validator.Validate(input);
        diagnostics.AddRange(validation.Diagnostics);

        var basis = _basisCalculator.CalculateDailyVolume(input.Demand);
        diagnostics.AddRange(basis.Diagnostics);

        var drawProfileInput = MergeDrawProfileInput(input);
        var drawProfile = _drawProfileBuilder.Build(drawProfileInput);
        diagnostics.AddRange(drawProfile.Diagnostics);

        var density = ResolveDensity(input, diagnostics);
        var cp = ResolveSpecificHeat(input, diagnostics);

        var deltaT = input.TemperatureModel.HotWaterSetpointTemperatureCelsius -
                     input.TemperatureModel.ColdWaterTemperatureCelsius;
        diagnostics.Add(CreateInfo(
            "AE-DHW-TEMPERATURE-RISE-CALCULATED",
            $"Temperature rise calculated as {deltaT:F3} K."));

        IReadOnlyList<double> hourlyVolume;
        IReadOnlyList<double> hourlyEnergy;
        double annualVolume;
        double dailyVolume;
        IReadOnlyList<double> monthlyVolume;
        IReadOnlyList<double> monthlyEnergy;
        double annualEnergy;
        double dailyEnergy;

        if (input.Demand.DemandBasis == DomesticHotWaterDemandBasis.ScheduledEnergy &&
            basis.UsesScheduledUsefulEnergy &&
            basis.ScheduledUsefulEnergyKWh is { Count: 8760 })
        {
            hourlyEnergy = basis.ScheduledUsefulEnergyKWh.ToArray();
            annualEnergy = hourlyEnergy.Sum();
            dailyEnergy = annualEnergy / 365.0;

            if (deltaT > 0.0)
            {
                hourlyVolume = hourlyEnergy
                    .Select(energy => energy * 3_600_000.0 / (density * cp * deltaT))
                    .ToArray();
            }
            else
            {
                hourlyVolume = new double[8760];
            }

            annualVolume = hourlyVolume.Sum();
            dailyVolume = annualVolume / 365.0;
            monthlyVolume = BuildMonthlySums(hourlyVolume);
            monthlyEnergy = BuildMonthlySums(hourlyEnergy);
            diagnostics.Add(CreateInfo(
                "AE-DHW-SCHEDULED-ENERGY-USED",
                "Scheduled useful-energy profile was used directly for useful demand."));
        }
        else if (input.Demand.DemandBasis is DomesticHotWaterDemandBasis.CustomHourlyVolume or DomesticHotWaterDemandBasis.ScheduledVolume &&
            basis.UsesCustomHourlyVolume &&
            basis.CustomHourlyVolumeLiters8760.Count == 8760)
        {
            hourlyVolume = basis.CustomHourlyVolumeLiters8760.ToArray();
            annualVolume = hourlyVolume.Sum();
            dailyVolume = annualVolume / 365.0;
            monthlyVolume = BuildMonthlySums(hourlyVolume);
            hourlyEnergy = hourlyVolume
                .Select(volume => CalculateEnergyKWh(volume, density, cp, Math.Max(deltaT, 0.0)))
                .ToArray();
            monthlyEnergy = BuildMonthlySums(hourlyEnergy);
            annualEnergy = hourlyEnergy.Sum();
            dailyEnergy = annualEnergy / 365.0;
            diagnostics.Add(CreateInfo(
                "AE-DHW-CUSTOM-HOURLY-VOLUME-USED",
                "Custom 8760 hourly volume was used directly."));
        }
        else
        {
            dailyVolume = basis.DailyVolumeLiters;
            annualVolume = dailyVolume * 365.0;
            hourlyVolume = drawProfile.AnnualHourlyFractions8760
                .Select(fraction => annualVolume * fraction)
                .ToArray();
            monthlyVolume = BuildMonthlySums(hourlyVolume);
            hourlyEnergy = hourlyVolume
                .Select(volume => CalculateEnergyKWh(volume, density, cp, Math.Max(deltaT, 0.0)))
                .ToArray();
            monthlyEnergy = BuildMonthlySums(hourlyEnergy);
            annualEnergy = hourlyEnergy.Sum();
            dailyEnergy = annualEnergy / 365.0;
            diagnostics.Add(CreateInfo(
                "AE-DHW-HOURLY-VOLUME-PROFILE-CALCULATED",
                "8760 hourly DHW volume profile was calculated from normalized annual fractions."));
        }
        diagnostics.Add(CreateInfo(
            "AE-DHW-MONTHLY-VOLUME-CALCULATED",
            "Monthly DHW volume sums were calculated using a non-leap calendar."));

        diagnostics.Add(CreateInfo(
            "AE-DHW-USEFUL-ENERGY-CALCULATED",
            "Useful DHW energy was calculated for hourly, monthly, daily, and annual outputs."));

        var disclosure = MergeDisclosure(
            _disclosureFactory.CreateDomesticHotWaterIso12831Disclosure(),
            input.DisclosureOverride,
            diagnostics);

        return new DomesticHotWaterUsefulDemandResult(
            CalculationId: input.CalculationId,
            BuildingId: input.BuildingId,
            ZoneId: input.ZoneId,
            RoomId: input.RoomId,
            DemandBasis: input.Demand.DemandBasis,
            UseCategory: input.Demand.UseCategory,
            DailyVolumeLiters: dailyVolume,
            AnnualVolumeLiters: annualVolume,
            MonthlyVolumeLiters: monthlyVolume,
            HourlyVolumeLiters8760: hourlyVolume,
            TemperatureRiseKelvin: deltaT,
            DailyUsefulEnergyKWh: dailyEnergy,
            AnnualUsefulEnergyKWh: annualEnergy,
            MonthlyUsefulEnergyKWh: monthlyEnergy,
            HourlyUsefulEnergyKWh8760: hourlyEnergy,
            Disclosure: disclosure,
            Diagnostics: diagnostics);
    }

    public DomesticHotWaterDrawOffProfileResult Calculate(DomesticHotWaterDemandDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var resolution = definition.ScheduledUsefulEnergyProfileKWh is { Count: 12 } ||
                         definition.MonthlySchedule is { Count: 12 }
            ? DomesticHotWaterDrawOffProfileResolution.Monthly
            : DomesticHotWaterDrawOffProfileResolution.Hourly;
        var numberOfSteps = resolution == DomesticHotWaterDrawOffProfileResolution.Monthly ? 12 : 8760;

        var drawOffBuilder = new DomesticHotWaterDrawOffProfileBuilder();
        var request = new DomesticHotWaterDrawOffProfileRequest(
            DemandDefinition: definition,
            Resolution: resolution,
            NumberOfSteps: numberOfSteps,
            Schedule: resolution == DomesticHotWaterDrawOffProfileResolution.Monthly
                ? definition.MonthlySchedule
                : definition.HourlySchedule,
            NormalizationMode: DomesticHotWaterScheduleNormalizationMode.NormalizeToUnity,
            FallbackProfileMode: DomesticHotWaterFallbackProfileMode.DeterministicByUseKind,
            DiagnosticsMode: DomesticHotWaterDiagnosticsMode.Verbose);

        return drawOffBuilder.Build(request);
    }

    private static DomesticHotWaterDrawProfileInput MergeDrawProfileInput(DomesticHotWaterUsefulDemandInput input)
    {
        if (input.DrawProfile.HourlyFractions24 is not null || input.Demand.CustomDailyProfileFractions is null)
            return input.DrawProfile;

        return input.DrawProfile with
        {
            HourlyFractions24 = input.Demand.CustomDailyProfileFractions
        };
    }

    private static double ResolveDensity(
        DomesticHotWaterUsefulDemandInput input,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (input.WaterDensityKgPerLiter is > 0.0)
            return input.WaterDensityKgPerLiter.Value;

        diagnostics.Add(CreateInfo(
            "AE-DHW-WATER-DENSITY-DEFAULTED",
            $"Water density defaulted to {DefaultWaterDensityKgPerLiter:F3} kg/l."));
        return DefaultWaterDensityKgPerLiter;
    }

    private static double ResolveSpecificHeat(
        DomesticHotWaterUsefulDemandInput input,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (input.WaterSpecificHeatJPerKgKelvin is > 0.0)
            return input.WaterSpecificHeatJPerKgKelvin.Value;

        diagnostics.Add(CreateInfo(
            "AE-DHW-WATER-CP-DEFAULTED",
            $"Water specific heat defaulted to {DefaultWaterSpecificHeatJPerKgKelvin:F1} J/(kg.K)."));
        return DefaultWaterSpecificHeatJPerKgKelvin;
    }

    private static double CalculateEnergyKWh(
        double liters,
        double densityKgPerLiter,
        double cpJPerKgKelvin,
        double deltaTKelvin) =>
        liters * densityKgPerLiter * cpJPerKgKelvin * deltaTKelvin / 3_600_000.0;

    private static IReadOnlyList<double> BuildMonthlySums(IReadOnlyList<double> hourlyValues)
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
            "DomesticHotWaterUsefulDemandCalculator");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        DomesticHotWaterDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.DomesticHotWater,
            "DomesticHotWaterUsefulDemandCalculator");
}
