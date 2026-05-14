using AssistantEngineer.Modules.Calculations.Application.Abstractions.Standards;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public sealed class NaturalVentilationZoneLoadCalculator : INaturalVentilationZoneLoadCalculator
{
    private static readonly IReadOnlyList<string> RequiredForbiddenClaims =
    [
        "Full ISO compliance",
        "Full EN compliance",
        "StandardReference equivalence",
        "EnergyPlus comparison workflow",
        "ASHRAE 140 / BESTEST-style validation anchor"
    ];

    private readonly INaturalVentilationZoneIntegrationValidator _validator;
    private readonly INaturalVentilationOpeningControlEvaluator _controlEvaluator;
    private readonly INaturalVentilationHourlyInputBuilder _hourlyInputBuilder;
    private readonly INaturalVentilationAirflowCalculator _airflowCalculator;
    private readonly IStandardCalculationDisclosureFactory _disclosureFactory;

    public NaturalVentilationZoneLoadCalculator(
        INaturalVentilationZoneIntegrationValidator validator,
        INaturalVentilationOpeningControlEvaluator controlEvaluator,
        INaturalVentilationHourlyInputBuilder hourlyInputBuilder,
        INaturalVentilationAirflowCalculator airflowCalculator,
        IStandardCalculationDisclosureFactory disclosureFactory)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _controlEvaluator = controlEvaluator ?? throw new ArgumentNullException(nameof(controlEvaluator));
        _hourlyInputBuilder = hourlyInputBuilder ?? throw new ArgumentNullException(nameof(hourlyInputBuilder));
        _airflowCalculator = airflowCalculator ?? throw new ArgumentNullException(nameof(airflowCalculator));
        _disclosureFactory = disclosureFactory ?? throw new ArgumentNullException(nameof(disclosureFactory));
    }

    public NaturalVentilationZoneIntegrationResult Calculate(
        NaturalVentilationZoneIntegrationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        var validation = _validator.Validate(input);
        diagnostics.AddRange(validation.Diagnostics);

        var controlContexts = NaturalVentilationZoneInputNormalizer.BuildControlContexts(input.HourlyEnvironments);

        var controlEvaluation = _controlEvaluator.Evaluate(
            new NaturalVentilationControlEvaluationInput(
                Rules: input.ControlRules,
                HourlyContexts: controlContexts,
                DisclosureOverride: input.DisclosureOverride,
                Source: input.Source));
        diagnostics.AddRange(controlEvaluation.Diagnostics);

        var openingResults = new List<NaturalVentilationHourlyOpeningCalculationResult>();
        foreach (var environment in input.HourlyEnvironments.OrderBy(env => env.HourIndex))
        {
            var operationsForHour = controlEvaluation.Operations
                .Where(operation => operation.HourIndex == environment.HourIndex)
                .Where(operation => NaturalVentilationZoneInputNormalizer.MatchesEnvironment(operation, environment))
                .ToArray();

            var hourlyInput = _hourlyInputBuilder.BuildHourlyAirflowInput(input, environment, operationsForHour);
            diagnostics.AddRange(hourlyInput.Environment.Diagnostics);

            var openingFractionsById = hourlyInput.Openings
                .Where(opening => !string.IsNullOrWhiteSpace(opening.OpeningId))
                .ToDictionary(
                    opening => opening.OpeningId,
                    opening => opening.OpeningFraction.GetValueOrDefault(0.0),
                    StringComparer.Ordinal);

            var cp = NaturalVentilationZoneInputNormalizer.ResolveAirSpecificHeat(input, environment, diagnostics, environment.HourIndex);
            var density = NaturalVentilationZoneInputNormalizer.ResolveAirDensity(input, environment, diagnostics, environment.HourIndex);
            var deltaTemperature = environment.IndoorTemperatureCelsius - environment.OutdoorTemperatureCelsius;

            var hourlyOpeningResults = NaturalVentilationScenarioEvaluator.Evaluate(
                input,
                hourlyInput,
                environment,
                density,
                diagnostics,
                _airflowCalculator);

            foreach (var opening in hourlyOpeningResults)
            {
                var openingDiagnostics = new List<StandardCalculationDiagnostic>();
                openingDiagnostics.AddRange(opening.Diagnostics);

                var airflowM3PerS = Math.Max(0.0, opening.AirflowCubicMetersPerSecond ?? 0.0);
                var airflowM3PerH = Math.Max(0.0, opening.AirflowCubicMetersPerHour ?? airflowM3PerS * 3600.0);
                var airflowKgPerS = Math.Max(0.0, opening.AirflowKilogramsPerSecond ?? airflowM3PerS * density);

                var hve = airflowKgPerS * cp;
                var sensibleLoad = hve * deltaTemperature;

                openingDiagnostics.Add(NaturalVentilationZoneDiagnosticsBuilder.Info(
                    "AE-VENT-ZONE-HVE-CALCULATED",
                    $"Opening '{opening.OpeningId}' hour {environment.HourIndex} ventilation heat transfer coefficient was calculated."));
                openingDiagnostics.Add(NaturalVentilationZoneDiagnosticsBuilder.Info(
                    "AE-VENT-ZONE-SENSIBLE-LOAD-CALCULATED",
                    $"Opening '{opening.OpeningId}' hour {environment.HourIndex} sensible ventilation load was calculated."));

                var openingFraction = openingFractionsById.TryGetValue(opening.OpeningId, out var resolvedOpeningFraction)
                    ? resolvedOpeningFraction
                    : 0.0;

                openingResults.Add(new NaturalVentilationHourlyOpeningCalculationResult(
                    HourIndex: environment.HourIndex,
                    OpeningId: opening.OpeningId,
                    RoomId: opening.RoomId ?? environment.RoomId,
                    ZoneId: opening.ZoneId ?? environment.ZoneId,
                    OpeningFraction: openingFraction,
                    AirflowCubicMetersPerSecond: airflowM3PerS,
                    AirflowCubicMetersPerHour: airflowM3PerH,
                    AirflowKilogramsPerSecond: airflowKgPerS,
                    VentilationHeatTransferCoefficientWPerKelvin: hve,
                    SensibleVentilationLoadWatts: sensibleLoad,
                    Diagnostics: openingDiagnostics));
            }

            diagnostics.Add(NaturalVentilationZoneDiagnosticsBuilder.Info(
                "AE-VENT-ZONE-HOURLY-RESULT-CALCULATED",
                $"Natural ventilation hourly result was calculated for hour {environment.HourIndex}."));
        }

        diagnostics.AddRange(openingResults.SelectMany(result => result.Diagnostics));

        var roomAggregation = NaturalVentilationZoneResultAggregator.BuildRoomResults(input, openingResults);
        var roomResults = roomAggregation.RoomResults;
        diagnostics.AddRange(roomResults.SelectMany(result => result.Diagnostics));

        var zoneAggregation = NaturalVentilationZoneResultAggregator.BuildZoneResultsAndProfiles(
            input,
            openingResults,
            roomResults,
            diagnostics);

        var disclosure = MergeDisclosure(
            _disclosureFactory.CreateNaturalVentilationEn16798Disclosure(),
            input.DisclosureOverride,
            diagnostics);

        return new NaturalVentilationZoneIntegrationResult(
            CalculationId: input.CalculationId,
            HourlyZones: zoneAggregation.HourlyZones,
            UnassignedRooms: roomAggregation.UnassignedRooms,
            UnassignedOpenings: roomAggregation.UnassignedOpenings,
            ZoneAirflowCubicMetersPerHourProfiles: zoneAggregation.ZoneAirflowProfiles,
            ZoneVentilationHeatTransferCoefficientProfilesWPerKelvin: zoneAggregation.ZoneHveProfiles,
            ZoneSensibleVentilationLoadProfilesWatts: zoneAggregation.ZoneLoadProfiles,
            ZoneAirChangesPerHourProfiles: zoneAggregation.ZoneAchProfiles,
            Disclosure: disclosure,
            Diagnostics: diagnostics);
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
            diagnostics.Add(NaturalVentilationZoneDiagnosticsBuilder.Warning(
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
}
