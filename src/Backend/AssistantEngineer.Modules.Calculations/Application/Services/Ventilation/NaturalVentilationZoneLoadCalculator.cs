using AssistantEngineer.Modules.Calculations.Application.Abstractions.Standards;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public sealed class NaturalVentilationZoneLoadCalculator : INaturalVentilationZoneLoadCalculator
{
    private const double DefaultAirSpecificHeatJPerKgKelvin = 1005.0;

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

        var controlContexts = input.HourlyEnvironments
            .Select(environment => new NaturalVentilationHourlyControlContext(
                HourIndex: environment.HourIndex,
                IndoorTemperatureCelsius: environment.IndoorTemperatureCelsius,
                OutdoorTemperatureCelsius: environment.OutdoorTemperatureCelsius,
                WindSpeedMetersPerSecond: environment.WindSpeedMetersPerSecond,
                OccupancyFraction: environment.OccupancyFraction,
                ScheduleFraction: environment.ScheduleFraction,
                IsNightHour: environment.IsNightHour,
                RoomId: environment.RoomId,
                ZoneId: environment.ZoneId,
                Diagnostics: environment.Diagnostics))
            .ToArray();

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
                .Where(operation => MatchesEnvironment(operation, environment))
                .ToArray();

            var hourlyInput = _hourlyInputBuilder.BuildHourlyAirflowInput(input, environment, operationsForHour);
            diagnostics.AddRange(hourlyInput.Environment.Diagnostics);

            var airflowResult = _airflowCalculator.Calculate(hourlyInput);
            diagnostics.AddRange(airflowResult.Diagnostics);
            var openingFractionsById = hourlyInput.Openings
                .Where(opening => !string.IsNullOrWhiteSpace(opening.OpeningId))
                .ToDictionary(
                    opening => opening.OpeningId,
                    opening => opening.OpeningFraction.GetValueOrDefault(0.0),
                    StringComparer.Ordinal);

            var cp = ResolveAirSpecificHeat(input, environment, diagnostics, environment.HourIndex);
            var deltaTemperature = environment.IndoorTemperatureCelsius - environment.OutdoorTemperatureCelsius;

            foreach (var opening in airflowResult.Openings)
            {
                var openingDiagnostics = new List<StandardCalculationDiagnostic>();
                openingDiagnostics.AddRange(opening.Diagnostics);

                var airflowM3PerS = Math.Max(0.0, opening.AirflowCubicMetersPerSecond ?? 0.0);
                var airflowM3PerH = Math.Max(0.0, opening.AirflowCubicMetersPerHour ?? airflowM3PerS * 3600.0);
                var airflowKgPerS = Math.Max(0.0, opening.AirflowKilogramsPerSecond ?? 0.0);

                var hve = airflowKgPerS * cp;
                var sensibleLoad = hve * deltaTemperature;

                openingDiagnostics.Add(CreateInfo(
                    "AE-VENT-ZONE-HVE-CALCULATED",
                    $"Opening '{opening.OpeningId}' hour {environment.HourIndex} ventilation heat transfer coefficient was calculated."));
                openingDiagnostics.Add(CreateInfo(
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

            diagnostics.Add(CreateInfo(
                "AE-VENT-ZONE-HOURLY-RESULT-CALCULATED",
                $"Natural ventilation hourly result was calculated for hour {environment.HourIndex}."));
        }

        diagnostics.AddRange(openingResults.SelectMany(result => result.Diagnostics));

        var roomsById = input.Topology.Rooms
            .Where(room => !string.IsNullOrWhiteSpace(room.RoomId))
            .ToDictionary(room => room.RoomId, room => room, StringComparer.Ordinal);

        var roomResults = new List<NaturalVentilationHourlyRoomResult>();
        foreach (var roomGroup in openingResults
                     .Where(opening => !string.IsNullOrWhiteSpace(opening.RoomId))
                     .GroupBy(opening => new { opening.HourIndex, RoomId = opening.RoomId! }))
        {
            var roomDiagnostics = new List<StandardCalculationDiagnostic>();
            var roomOpenings = roomGroup.ToArray();
            var zoneId = roomOpenings.Select(opening => opening.ZoneId).FirstOrDefault(zone => !string.IsNullOrWhiteSpace(zone));

            var totalM3PerS = roomOpenings.Sum(opening => opening.AirflowCubicMetersPerSecond);
            var totalM3PerH = roomOpenings.Sum(opening => opening.AirflowCubicMetersPerHour);
            var totalKgPerS = roomOpenings.Sum(opening => opening.AirflowKilogramsPerSecond);
            var totalHve = roomOpenings.Sum(opening => opening.VentilationHeatTransferCoefficientWPerKelvin ?? 0.0);
            var totalLoad = roomOpenings.Sum(opening => opening.SensibleVentilationLoadWatts ?? 0.0);

            double? ach = null;
            if (roomsById.TryGetValue(roomGroup.Key.RoomId, out var room) &&
                room.VolumeCubicMeters is > 0.0)
            {
                ach = totalM3PerH / room.VolumeCubicMeters.Value;
                roomDiagnostics.Add(CreateInfo(
                    "AE-VENT-ZONE-ACH-CALCULATED",
                    $"Room '{room.RoomId}' ACH was calculated for hour {roomGroup.Key.HourIndex}."));
            }
            else
            {
                roomDiagnostics.Add(CreateWarning(
                    "AE-VENT-ZONE-VOLUME-MISSING",
                    $"Room '{roomGroup.Key.RoomId}' has missing/non-positive volume; ACH is unavailable."));
            }

            roomResults.Add(new NaturalVentilationHourlyRoomResult(
                HourIndex: roomGroup.Key.HourIndex,
                RoomId: roomGroup.Key.RoomId,
                ZoneId: zoneId,
                TotalAirflowCubicMetersPerSecond: totalM3PerS,
                TotalAirflowCubicMetersPerHour: totalM3PerH,
                TotalAirflowKilogramsPerSecond: totalKgPerS,
                AirChangesPerHour: ach,
                VentilationHeatTransferCoefficientWPerKelvin: totalHve,
                SensibleVentilationLoadWatts: totalLoad,
                Openings: roomOpenings,
                Diagnostics: roomDiagnostics));
        }

        diagnostics.AddRange(roomResults.SelectMany(result => result.Diagnostics));

        var unassignedOpenings = openingResults
            .Where(opening => string.IsNullOrWhiteSpace(opening.RoomId) && string.IsNullOrWhiteSpace(opening.ZoneId))
            .OrderBy(opening => opening.HourIndex)
            .ThenBy(opening => opening.OpeningId, StringComparer.Ordinal)
            .ToArray();

        var unassignedRooms = roomResults
            .Where(room => string.IsNullOrWhiteSpace(room.ZoneId))
            .OrderBy(room => room.HourIndex)
            .ThenBy(room => room.RoomId, StringComparer.Ordinal)
            .ToArray();

        var zoneIds = new HashSet<string>(
            input.Topology.Zones
                .Select(zone => zone.ZoneId)
                .Where(zoneId => !string.IsNullOrWhiteSpace(zoneId)),
            StringComparer.Ordinal);
        foreach (var room in roomResults.Where(room => !string.IsNullOrWhiteSpace(room.ZoneId)))
        {
            zoneIds.Add(room.ZoneId!);
        }
        foreach (var opening in openingResults.Where(opening => !string.IsNullOrWhiteSpace(opening.ZoneId)))
        {
            zoneIds.Add(opening.ZoneId!);
        }

        var hourlyZones = new List<NaturalVentilationHourlyZoneResult>();
        foreach (var zoneId in zoneIds)
        {
            var zoneRoomVolumes = input.Topology.Rooms
                .Where(room => string.Equals(room.ZoneId, zoneId, StringComparison.Ordinal))
                .Select(room => room.VolumeCubicMeters ?? 0.0)
                .Where(volume => volume > 0.0)
                .ToArray();
            var zoneVolume = zoneRoomVolumes.Sum();

            var hours = roomResults
                .Where(room => string.Equals(room.ZoneId, zoneId, StringComparison.Ordinal))
                .Select(room => room.HourIndex)
                .Union(openingResults
                    .Where(opening => string.IsNullOrWhiteSpace(opening.RoomId) &&
                                      string.Equals(opening.ZoneId, zoneId, StringComparison.Ordinal))
                    .Select(opening => opening.HourIndex))
                .Distinct()
                .OrderBy(hour => hour)
                .ToArray();

            foreach (var hour in hours)
            {
                var zoneDiagnostics = new List<StandardCalculationDiagnostic>();
                var rooms = roomResults
                    .Where(room => room.HourIndex == hour &&
                                   string.Equals(room.ZoneId, zoneId, StringComparison.Ordinal))
                    .ToArray();
                var zoneUnassignedOpenings = openingResults
                    .Where(opening => opening.HourIndex == hour &&
                                      string.IsNullOrWhiteSpace(opening.RoomId) &&
                                      string.Equals(opening.ZoneId, zoneId, StringComparison.Ordinal))
                    .ToArray();

                var totalM3PerS = rooms.Sum(room => room.TotalAirflowCubicMetersPerSecond) +
                                  zoneUnassignedOpenings.Sum(opening => opening.AirflowCubicMetersPerSecond);
                var totalM3PerH = rooms.Sum(room => room.TotalAirflowCubicMetersPerHour) +
                                  zoneUnassignedOpenings.Sum(opening => opening.AirflowCubicMetersPerHour);
                var totalKgPerS = rooms.Sum(room => room.TotalAirflowKilogramsPerSecond) +
                                  zoneUnassignedOpenings.Sum(opening => opening.AirflowKilogramsPerSecond);
                var totalHve = rooms.Sum(room => room.VentilationHeatTransferCoefficientWPerKelvin) +
                               zoneUnassignedOpenings.Sum(opening => opening.VentilationHeatTransferCoefficientWPerKelvin ?? 0.0);
                var totalLoad = rooms.Sum(room => room.SensibleVentilationLoadWatts) +
                                zoneUnassignedOpenings.Sum(opening => opening.SensibleVentilationLoadWatts ?? 0.0);

                double? ach = null;
                if (zoneVolume > 0.0)
                {
                    ach = totalM3PerH / zoneVolume;
                }
                else
                {
                    zoneDiagnostics.Add(CreateWarning(
                        "AE-VENT-ZONE-VOLUME-MISSING",
                        $"Zone '{zoneId}' has missing/non-positive volume; ACH is unavailable at hour {hour}."));
                }

                hourlyZones.Add(new NaturalVentilationHourlyZoneResult(
                    HourIndex: hour,
                    ZoneId: zoneId,
                    TotalAirflowCubicMetersPerSecond: totalM3PerS,
                    TotalAirflowCubicMetersPerHour: totalM3PerH,
                    TotalAirflowKilogramsPerSecond: totalKgPerS,
                    AirChangesPerHour: ach,
                    VentilationHeatTransferCoefficientWPerKelvin: totalHve,
                    SensibleVentilationLoadWatts: totalLoad,
                    Rooms: rooms,
                    UnassignedOpenings: zoneUnassignedOpenings,
                    Diagnostics: zoneDiagnostics));
            }
        }

        hourlyZones = hourlyZones
            .OrderBy(zone => zone.HourIndex)
            .ThenBy(zone => zone.ZoneId, StringComparer.Ordinal)
            .ToList();
        diagnostics.AddRange(hourlyZones.SelectMany(zone => zone.Diagnostics));

        var zoneAirflowProfiles = new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal);
        var zoneHveProfiles = new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal);
        var zoneLoadProfiles = new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal);
        var zoneAchProfiles = new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal);

        foreach (var zoneGroup in hourlyZones.GroupBy(zone => zone.ZoneId, StringComparer.Ordinal))
        {
            var zoneHourly = zoneGroup
                .OrderBy(result => result.HourIndex)
                .ToArray();

            zoneAirflowProfiles[zoneGroup.Key] = zoneHourly
                .Select(result => result.TotalAirflowCubicMetersPerHour)
                .ToArray();
            zoneHveProfiles[zoneGroup.Key] = zoneHourly
                .Select(result => result.VentilationHeatTransferCoefficientWPerKelvin)
                .ToArray();
            zoneLoadProfiles[zoneGroup.Key] = zoneHourly
                .Select(result => result.SensibleVentilationLoadWatts)
                .ToArray();
            zoneAchProfiles[zoneGroup.Key] = zoneHourly
                .Select(result => result.AirChangesPerHour ?? 0.0)
                .ToArray();

            var profileLength = zoneHourly.Length;
            if (profileLength != 24 && profileLength != 8760)
            {
                diagnostics.Add(CreateWarning(
                    "AE-VENT-ZONE-PROFILE-LENGTH-NONSTANDARD",
                    $"Zone '{zoneGroup.Key}' has nonstandard profile length {profileLength}."));
            }
        }

        diagnostics.Add(CreateInfo(
            "AE-VENT-ZONE-PROFILE-BUILT",
            $"Zone profiles were built for {zoneAirflowProfiles.Count} zone(s)."));

        var disclosure = MergeDisclosure(
            _disclosureFactory.CreateNaturalVentilationEn16798Disclosure(),
            input.DisclosureOverride,
            diagnostics);

        return new NaturalVentilationZoneIntegrationResult(
            CalculationId: input.CalculationId,
            HourlyZones: hourlyZones,
            UnassignedRooms: unassignedRooms,
            UnassignedOpenings: unassignedOpenings,
            ZoneAirflowCubicMetersPerHourProfiles: zoneAirflowProfiles,
            ZoneVentilationHeatTransferCoefficientProfilesWPerKelvin: zoneHveProfiles,
            ZoneSensibleVentilationLoadProfilesWatts: zoneLoadProfiles,
            ZoneAirChangesPerHourProfiles: zoneAchProfiles,
            Disclosure: disclosure,
            Diagnostics: diagnostics);
    }

    private static bool MatchesEnvironment(
        NaturalVentilationOpeningOperationResult operation,
        NaturalVentilationHourlyZoneEnvironment environment)
    {
        if (!string.IsNullOrWhiteSpace(environment.RoomId))
        {
            if (!string.IsNullOrWhiteSpace(operation.RoomId) &&
                !string.Equals(operation.RoomId, environment.RoomId, StringComparison.Ordinal))
            {
                return false;
            }
        }
        else if (!string.IsNullOrWhiteSpace(operation.RoomId))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(environment.ZoneId))
        {
            if (!string.IsNullOrWhiteSpace(operation.ZoneId) &&
                !string.Equals(operation.ZoneId, environment.ZoneId, StringComparison.Ordinal))
            {
                return false;
            }
        }
        else if (!string.IsNullOrWhiteSpace(operation.ZoneId))
        {
            return false;
        }

        return true;
    }

    private static double ResolveAirSpecificHeat(
        NaturalVentilationZoneIntegrationInput input,
        NaturalVentilationHourlyZoneEnvironment environment,
        ICollection<StandardCalculationDiagnostic> diagnostics,
        int hourIndex)
    {
        if (environment.AirSpecificHeatJPerKgKelvin is > 0.0)
            return environment.AirSpecificHeatJPerKgKelvin.Value;

        if (input.DefaultAirSpecificHeatJPerKgKelvin is > 0.0)
            return input.DefaultAirSpecificHeatJPerKgKelvin.Value;

        diagnostics.Add(CreateInfo(
            "AE-VENT-ZONE-AIR-CP-DEFAULTED",
            $"Air specific heat defaulted to {DefaultAirSpecificHeatJPerKgKelvin:F1} J/(kg.K) at hour {hourIndex}."));
        return DefaultAirSpecificHeatJPerKgKelvin;
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
            StandardCalculationStage.Aggregation,
            "NaturalVentilationZoneLoadCalculator");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        NaturalVentilationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.Aggregation,
            "NaturalVentilationZoneLoadCalculator");
}
