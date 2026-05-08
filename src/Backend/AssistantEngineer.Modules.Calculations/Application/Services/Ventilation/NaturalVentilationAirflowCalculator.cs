using AssistantEngineer.Modules.Calculations.Application.Abstractions.Standards;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public sealed class NaturalVentilationAirflowCalculator : INaturalVentilationAirflowCalculator
{
    private static readonly IReadOnlyList<string> RequiredForbiddenClaims =
    [
        "Full ISO compliance",
        "Full EN compliance",
        "StandardReference equivalence",
        "EnergyPlus comparison workflow",
        "ASHRAE 140 / BESTEST-style validation anchor"
    ];

    private readonly INaturalVentilationOpeningGeometryNormalizer _geometryNormalizer;
    private readonly INaturalVentilationInputValidator _inputValidator;
    private readonly INaturalVentilationPressureCalculator _pressureCalculator;
    private readonly IStandardCalculationDisclosureFactory _disclosureFactory;

    public NaturalVentilationAirflowCalculator(
        INaturalVentilationOpeningGeometryNormalizer geometryNormalizer,
        INaturalVentilationInputValidator inputValidator,
        INaturalVentilationPressureCalculator pressureCalculator,
        IStandardCalculationDisclosureFactory disclosureFactory)
    {
        _geometryNormalizer = geometryNormalizer ?? throw new ArgumentNullException(nameof(geometryNormalizer));
        _inputValidator = inputValidator ?? throw new ArgumentNullException(nameof(inputValidator));
        _pressureCalculator = pressureCalculator ?? throw new ArgumentNullException(nameof(pressureCalculator));
        _disclosureFactory = disclosureFactory ?? throw new ArgumentNullException(nameof(disclosureFactory));
    }

    public NaturalVentilationCalculationResult Calculate(NaturalVentilationCalculationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Openings);
        ArgumentNullException.ThrowIfNull(input.Environment);

        var diagnostics = new List<StandardCalculationDiagnostic>();

        var normalizedOpenings = input.Openings
            .Select(_geometryNormalizer.Normalize)
            .ToArray();

        var normalizedInput = input with
        {
            Openings = normalizedOpenings
        };

        var validation = _inputValidator.Validate(normalizedInput);
        diagnostics.AddRange(validation.Diagnostics);

        var openingResults = new List<NaturalVentilationOpeningResult>();
        foreach (var opening in normalizedOpenings)
        {
            var openingDiagnostics = new List<StandardCalculationDiagnostic>();
            openingDiagnostics.AddRange(opening.Diagnostics);

            var windPressure = _pressureCalculator.CalculateWindPressure(opening, normalizedInput.Environment);
            var stackPressure = _pressureCalculator.CalculateStackPressure(opening, normalizedInput.Environment);
            var combinedPressure = _pressureCalculator.CalculateCombinedPressure(opening, normalizedInput.Environment);

            openingDiagnostics.AddRange(windPressure.Diagnostics);
            openingDiagnostics.AddRange(stackPressure.Diagnostics);
            openingDiagnostics.AddRange(combinedPressure.Diagnostics);

            var effectiveArea = Math.Max(0.0, opening.OpeningAreaSquareMeters * (opening.OpeningFraction ?? 0.0));
            var dischargeCoefficient = opening.DischargeCoefficient.GetValueOrDefault();
            var selectedPressure = SelectPressure(
                normalizedInput.FlowConfiguration,
                opening,
                normalizedInput.Environment,
                windPressure,
                stackPressure,
                combinedPressure,
                openingDiagnostics);

            double? airflowCubicMetersPerSecond = null;
            double? airflowCubicMetersPerHour = null;
            double? airflowKilogramsPerSecond = null;

            if (effectiveArea > 0.0 &&
                dischargeCoefficient > 0.0 &&
                selectedPressure.PressureDifferencePa.HasValue &&
                selectedPressure.AirDensityKgPerCubicMeter > 0.0)
            {
                var airflow =
                    dischargeCoefficient *
                    effectiveArea *
                    Math.Sqrt(
                        2.0 *
                        Math.Abs(selectedPressure.PressureDifferencePa.Value) /
                        selectedPressure.AirDensityKgPerCubicMeter);

                airflowCubicMetersPerSecond = airflow;
                airflowCubicMetersPerHour = airflow * 3600.0;
                airflowKilogramsPerSecond = airflow * selectedPressure.AirDensityKgPerCubicMeter;

                openingDiagnostics.Add(CreateInfo(
                    "AE-VENT-AIRFLOW-CALCULATED",
                    $"Opening '{opening.OpeningId}' airflow was calculated using deterministic orifice-flow assumptions."));
            }
            else
            {
                openingDiagnostics.Add(CreateWarning(
                    "AE-VENT-AIRFLOW-NOT-CALCULABLE",
                    $"Opening '{opening.OpeningId}' airflow is not calculable from current geometry, pressure, or coefficient metadata."));
            }

            openingResults.Add(new NaturalVentilationOpeningResult(
                OpeningId: opening.OpeningId,
                RoomId: opening.RoomId,
                ZoneId: opening.ZoneId,
                SurfaceId: opening.SurfaceId,
                EffectiveOpeningAreaSquareMeters: effectiveArea,
                DischargeCoefficient: dischargeCoefficient,
                WindPressureDifferencePa: windPressure.PressureDifferencePa,
                StackPressureDifferencePa: stackPressure.PressureDifferencePa,
                CombinedPressureDifferencePa: combinedPressure.PressureDifferencePa,
                AirflowCubicMetersPerSecond: airflowCubicMetersPerSecond,
                AirflowCubicMetersPerHour: airflowCubicMetersPerHour,
                AirflowKilogramsPerSecond: airflowKilogramsPerSecond,
                Diagnostics: openingDiagnostics));
        }

        diagnostics.AddRange(openingResults.SelectMany(result => result.Diagnostics));

        var totalAirflowM3PerS = 0.0;
        var totalAirflowM3PerH = 0.0;
        var totalAirflowKgPerS = 0.0;

        if (normalizedInput.FlowConfiguration == NaturalVentilationFlowConfiguration.Unknown ||
            normalizedInput.FlowConfiguration == NaturalVentilationFlowConfiguration.Other)
        {
            diagnostics.Add(CreateWarning(
                "AE-VENT-UNKNOWN-CONFIGURATION-NO-FALLBACK",
                $"Flow configuration '{normalizedInput.FlowConfiguration}' does not use a silent fallback path."));
        }
        else if (normalizedInput.FlowConfiguration == NaturalVentilationFlowConfiguration.CrossVentilation)
        {
            var positiveFlows = openingResults
                .Select(result => result.AirflowCubicMetersPerSecond.GetValueOrDefault())
                .Where(flow => flow > 0.0)
                .ToArray();

            if (positiveFlows.Length > 0)
            {
                totalAirflowM3PerS = positiveFlows.Min();
                totalAirflowM3PerH = totalAirflowM3PerS * 3600.0;

                var representativeDensity = ResolveRepresentativeDensity(openingResults, normalizedInput.Environment);
                totalAirflowKgPerS = totalAirflowM3PerS * representativeDensity;
            }

            diagnostics.Add(CreateInfo(
                "AE-VENT-CROSS-VENTILATION-MIN-FLOW-USED",
                "Cross-ventilation total airflow used conservative minimum opening flow to avoid double counting."));
        }
        else
        {
            totalAirflowM3PerS = openingResults.Sum(result => Math.Max(0.0, result.AirflowCubicMetersPerSecond.GetValueOrDefault()));
            totalAirflowM3PerH = totalAirflowM3PerS * 3600.0;
            var representativeDensity = ResolveRepresentativeDensity(openingResults, normalizedInput.Environment);
            totalAirflowKgPerS = totalAirflowM3PerS * representativeDensity;

            diagnostics.Add(CreateInfo(
                "AE-VENT-AIRFLOW-SUMMED-BY-OPENING",
                "Total airflow was aggregated by conservative sum across opening results."));
        }

        var disclosure = MergeDisclosure(
            _disclosureFactory.CreateNaturalVentilationEn16798Disclosure(),
            normalizedInput.DisclosureOverride,
            diagnostics);

        return new NaturalVentilationCalculationResult(
            CalculationId: normalizedInput.CalculationId,
            FlowConfiguration: normalizedInput.FlowConfiguration,
            TotalAirflowCubicMetersPerSecond: totalAirflowM3PerS,
            TotalAirflowCubicMetersPerHour: totalAirflowM3PerH,
            TotalAirflowKilogramsPerSecond: totalAirflowKgPerS,
            Openings: openingResults,
            Disclosure: disclosure,
            Diagnostics: diagnostics);
    }

    private static NaturalVentilationPressureResult SelectPressure(
        NaturalVentilationFlowConfiguration configuration,
        NaturalVentilationOpeningGeometry opening,
        NaturalVentilationEnvironment environment,
        NaturalVentilationPressureResult windPressure,
        NaturalVentilationPressureResult stackPressure,
        NaturalVentilationPressureResult combinedPressure,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        switch (configuration)
        {
            case NaturalVentilationFlowConfiguration.WindOnly:
                return windPressure;
            case NaturalVentilationFlowConfiguration.StackOnly:
                return stackPressure;
            case NaturalVentilationFlowConfiguration.CombinedWindAndStack:
                return combinedPressure;
            case NaturalVentilationFlowConfiguration.SingleSided:
                return combinedPressure;
            case NaturalVentilationFlowConfiguration.CrossVentilation:
                if (ShouldUseCombinedForCross(opening, environment, stackPressure))
                {
                    diagnostics.Add(CreateInfo(
                        "AE-VENT-CROSS-VENTILATION-COMBINED-PRESSURE-USED",
                        $"Opening '{opening.OpeningId}' cross-ventilation pressure used combined wind+stack pressure."));
                    return combinedPressure;
                }

                diagnostics.Add(CreateInfo(
                    "AE-VENT-CROSS-VENTILATION-WIND-PRESSURE-USED",
                    $"Opening '{opening.OpeningId}' cross-ventilation pressure used wind-only pressure."));
                return windPressure;
            default:
                diagnostics.Add(CreateWarning(
                    "AE-VENT-UNKNOWN-CONFIGURATION-NO-FALLBACK",
                    $"Flow configuration '{configuration}' is not supported for deterministic pressure selection."));
                return new NaturalVentilationPressureResult(
                    PressureDifferencePa: null,
                    AirDensityKgPerCubicMeter: ResolveDensityFromEnvironment(environment),
                    Diagnostics: []);
        }
    }

    private static bool ShouldUseCombinedForCross(
        NaturalVentilationOpeningGeometry opening,
        NaturalVentilationEnvironment environment,
        NaturalVentilationPressureResult stackPressure)
    {
        if (!stackPressure.PressureDifferencePa.HasValue || stackPressure.PressureDifferencePa.Value <= 0.0)
            return false;

        var hasHeightDifference =
            (opening.TopHeightMeters.HasValue && opening.BottomHeightMeters.HasValue) ||
            opening.OpeningHeightMeters is > 0.0 ||
            (opening.OpeningCenterHeightMeters.HasValue && environment.OpeningReferenceHeightMeters.HasValue);
        return hasHeightDifference;
    }

    private static double ResolveRepresentativeDensity(
        IReadOnlyList<NaturalVentilationOpeningResult> openingResults,
        NaturalVentilationEnvironment environment)
    {
        var densities = new List<double>();
        foreach (var opening in openingResults)
        {
            if (opening.AirflowCubicMetersPerSecond is > 0.0 &&
                opening.AirflowKilogramsPerSecond is > 0.0)
            {
                var density = opening.AirflowKilogramsPerSecond.Value / opening.AirflowCubicMetersPerSecond.Value;
                if (double.IsFinite(density) && density > 0.0)
                    densities.Add(density);
            }
        }

        if (densities.Count > 0)
            return densities.Average();

        return ResolveDensityFromEnvironment(environment);
    }

    private static double ResolveDensityFromEnvironment(NaturalVentilationEnvironment environment)
    {
        if (environment.OutdoorAirDensityKgPerCubicMeter is > 0.0)
            return environment.OutdoorAirDensityKgPerCubicMeter.Value;
        if (environment.IndoorAirDensityKgPerCubicMeter is > 0.0)
            return environment.IndoorAirDensityKgPerCubicMeter.Value;
        return 1.204;
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
                "AE-VENT-DISCLOSURE-OVERRIDE-SANITIZED",
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
        NaturalVentilationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            StandardCalculationStage.HeatTransfer,
            "NaturalVentilationAirflowCalculator");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        NaturalVentilationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.HeatTransfer,
            "NaturalVentilationAirflowCalculator");
}
