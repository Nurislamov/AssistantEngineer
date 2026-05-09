using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public sealed class NaturalVentilationZoneIntegrationValidator : INaturalVentilationZoneIntegrationValidator
{
    public NaturalVentilationZoneIntegrationValidationResult Validate(
        NaturalVentilationZoneIntegrationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var diagnostics = new List<StandardCalculationDiagnostic>();

        if (string.IsNullOrWhiteSpace(input.CalculationId))
        {
            diagnostics.Add(CreateError(
                "AE-VENT-ZONE-CALCULATION-ID-MISSING",
                "Natural ventilation zone integration calculation id is required."));
        }

        if (input.Topology is null ||
            (input.Topology.Rooms.Count == 0 && input.Topology.Zones.Count == 0))
        {
            diagnostics.Add(CreateError(
                "AE-VENT-ZONE-TOPOLOGY-MISSING",
                "Natural ventilation zone integration requires topology with rooms or zones."));
            var orderedEarlyDiagnostics = diagnostics
                .OrderByDescending(diagnostic => diagnostic.Severity)
                .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
                .ToArray();
            return new NaturalVentilationZoneIntegrationValidationResult(
                IsValid: diagnostics.All(diagnostic => diagnostic.Severity != CalculationDiagnosticSeverity.Error),
                Diagnostics: orderedEarlyDiagnostics);
        }

        diagnostics.AddRange(input.Topology.Diagnostics);

        if (input.Openings.Count == 0)
        {
            diagnostics.Add(CreateError(
                "AE-VENT-ZONE-OPENINGS-MISSING",
                "Natural ventilation zone integration requires at least one opening."));
        }

        if (input.HourlyEnvironments.Count == 0)
        {
            diagnostics.Add(CreateError(
                "AE-VENT-ZONE-HOURLY-ENVIRONMENTS-MISSING",
                "Natural ventilation zone integration requires hourly environments."));
        }

        var roomsById = input.Topology.Rooms
            .Where(room => !string.IsNullOrWhiteSpace(room.RoomId))
            .ToDictionary(room => room.RoomId, room => room, StringComparer.Ordinal);
        var zonesById = input.Topology.Zones
            .Where(zone => !string.IsNullOrWhiteSpace(zone.ZoneId))
            .ToDictionary(zone => zone.ZoneId, zone => zone, StringComparer.Ordinal);
        var openingsById = input.Openings
            .Where(opening => !string.IsNullOrWhiteSpace(opening.OpeningId))
            .ToDictionary(opening => opening.OpeningId, opening => opening, StringComparer.Ordinal);
        var surfacesById = input.Topology.Surfaces
            .Where(surface => !string.IsNullOrWhiteSpace(surface.SurfaceId))
            .ToDictionary(surface => surface.SurfaceId, surface => surface, StringComparer.Ordinal);

        foreach (var opening in input.Openings)
        {
            diagnostics.AddRange(opening.Diagnostics);

            if (!string.IsNullOrWhiteSpace(opening.RoomId) && !roomsById.ContainsKey(opening.RoomId))
            {
                diagnostics.Add(CreateError(
                    "AE-VENT-ZONE-OPENING-ROOM-MISSING",
                    $"Opening '{opening.OpeningId}' references missing topology room '{opening.RoomId}'."));
            }

            if (!string.IsNullOrWhiteSpace(opening.ZoneId) && !zonesById.ContainsKey(opening.ZoneId))
            {
                diagnostics.Add(CreateError(
                    "AE-VENT-ZONE-OPENING-ZONE-MISSING",
                    $"Opening '{opening.OpeningId}' references missing topology zone '{opening.ZoneId}'."));
            }

            if (!(opening.OpeningAreaSquareMeters > 0.0))
            {
                diagnostics.Add(CreateError(
                    "AE-VENT-ZONE-OPENING-AREA-NONPOSITIVE",
                    $"Opening '{opening.OpeningId}' opening area must be greater than zero."));
            }

            if (opening.OpeningFraction.HasValue &&
                (!double.IsFinite(opening.OpeningFraction.Value) ||
                 opening.OpeningFraction.Value < 0.0 ||
                 opening.OpeningFraction.Value > 1.0))
            {
                diagnostics.Add(CreateError(
                    "AE-VENT-ZONE-OPENING-FRACTION-INVALID",
                    $"Opening '{opening.OpeningId}' opening fraction must be within [0,1]."));
            }

            if (opening.MaximumOpeningFraction.HasValue &&
                (!double.IsFinite(opening.MaximumOpeningFraction.Value) ||
                 opening.MaximumOpeningFraction.Value < 0.0 ||
                 opening.MaximumOpeningFraction.Value > 1.0))
            {
                diagnostics.Add(CreateError(
                    "AE-VENT-ZONE-OPENING-MAX-FRACTION-INVALID",
                    $"Opening '{opening.OpeningId}' maximum opening fraction must be within [0,1]."));
            }

            var boundaryId = string.IsNullOrWhiteSpace(opening.BoundaryId)
                ? opening.SurfaceId
                : opening.BoundaryId;

            if (input.StrictBoundaryValidation && string.IsNullOrWhiteSpace(boundaryId))
            {
                diagnostics.Add(CreateError(
                    "AE-VENT-ZONE-OPENING-BOUNDARY-ID-MISSING",
                    $"Opening '{opening.OpeningId}' requires boundary id/surface id for topology validation."));
            }

            if (!string.IsNullOrWhiteSpace(boundaryId))
            {
                if (!surfacesById.TryGetValue(boundaryId, out var surface))
                {
                    if (input.StrictBoundaryValidation)
                    {
                        diagnostics.Add(CreateError(
                            "AE-VENT-ZONE-OPENING-BOUNDARY-MISSING",
                            $"Opening '{opening.OpeningId}' references missing topology boundary '{boundaryId}'."));
                    }
                    else
                    {
                        diagnostics.Add(CreateWarning(
                            "AE-VENT-ZONE-OPENING-BOUNDARY-MISSING",
                            $"Opening '{opening.OpeningId}' references missing topology boundary '{boundaryId}'."));
                    }
                }
                else
                {
                    ValidateOpeningBoundaryKind(opening, surface, diagnostics);
                }
            }
        }

        foreach (var environment in input.HourlyEnvironments)
        {
            diagnostics.AddRange(environment.Diagnostics);

            if (environment.HourIndex < 0)
            {
                diagnostics.Add(CreateError(
                    "AE-VENT-ZONE-HOUR-INDEX-INVALID",
                    "Hourly environment hour index must be >= 0."));
            }

            if (!double.IsFinite(environment.IndoorTemperatureCelsius) ||
                !double.IsFinite(environment.OutdoorTemperatureCelsius))
            {
                diagnostics.Add(CreateError(
                    "AE-VENT-ZONE-TEMPERATURE-INVALID",
                    $"Hourly environment hour {environment.HourIndex} has invalid indoor/outdoor temperatures."));
            }

            if (!double.IsFinite(environment.WindSpeedMetersPerSecond) ||
                environment.WindSpeedMetersPerSecond < 0.0)
            {
                diagnostics.Add(CreateError(
                    "AE-VENT-ZONE-WIND-SPEED-INVALID",
                    $"Hourly environment hour {environment.HourIndex} has invalid wind speed."));
            }

            if (environment.PrescribedAirflowCubicMetersPerSecond.HasValue &&
                (!double.IsFinite(environment.PrescribedAirflowCubicMetersPerSecond.Value) ||
                 environment.PrescribedAirflowCubicMetersPerSecond.Value < 0.0))
            {
                diagnostics.Add(CreateError(
                    "AE-VENT-ZONE-PRESCRIBED-AIRFLOW-INVALID",
                    $"Hourly environment hour {environment.HourIndex} has invalid prescribed airflow."));
            }

            if (environment.AirDensityKgPerCubicMeter.HasValue &&
                (!double.IsFinite(environment.AirDensityKgPerCubicMeter.Value) ||
                 environment.AirDensityKgPerCubicMeter.Value <= 0.0))
            {
                diagnostics.Add(CreateError(
                    "AE-VENT-ZONE-AIR-DENSITY-INVALID",
                    $"Hourly environment hour {environment.HourIndex} has invalid air density."));
            }

            if (environment.AirSpecificHeatJPerKgKelvin.HasValue &&
                (!double.IsFinite(environment.AirSpecificHeatJPerKgKelvin.Value) ||
                 environment.AirSpecificHeatJPerKgKelvin.Value <= 0.0))
            {
                diagnostics.Add(CreateError(
                    "AE-VENT-ZONE-AIR-CP-INVALID",
                    $"Hourly environment hour {environment.HourIndex} has invalid air specific heat."));
            }

            if (!string.IsNullOrWhiteSpace(environment.RoomId) &&
                !roomsById.ContainsKey(environment.RoomId))
            {
                diagnostics.Add(CreateError(
                    "AE-VENT-ZONE-ENVIRONMENT-TARGET-MISSING",
                    $"Hourly environment hour {environment.HourIndex} references missing room '{environment.RoomId}'."));
            }

            if (!string.IsNullOrWhiteSpace(environment.ZoneId) &&
                !zonesById.ContainsKey(environment.ZoneId))
            {
                diagnostics.Add(CreateError(
                    "AE-VENT-ZONE-ENVIRONMENT-TARGET-MISSING",
                    $"Hourly environment hour {environment.HourIndex} references missing zone '{environment.ZoneId}'."));
            }
        }

        var duplicateTargetHours = input.HourlyEnvironments
            .GroupBy(environment => BuildTargetHourKey(environment), StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);
        foreach (var key in duplicateTargetHours)
        {
            diagnostics.Add(CreateWarning(
                "AE-VENT-ZONE-HOUR-DUPLICATE",
                $"Duplicate hourly environment target/hour key detected: '{key}'."));
        }

        foreach (var group in input.HourlyEnvironments
                     .GroupBy(environment => BuildTargetKey(environment), StringComparer.Ordinal))
        {
            var count = group.Count();
            if (count != 24 && count != 8760)
            {
                diagnostics.Add(CreateWarning(
                    "AE-VENT-ZONE-PROFILE-LENGTH-NONSTANDARD",
                    $"Hourly environment target '{group.Key}' has nonstandard profile length {count}."));
            }
        }

        foreach (var rule in input.ControlRules)
        {
            diagnostics.AddRange(rule.Diagnostics);

            var matched = false;
            if (!string.IsNullOrWhiteSpace(rule.OpeningId))
            {
                matched = openingsById.ContainsKey(rule.OpeningId);
            }
            else if (!string.IsNullOrWhiteSpace(rule.RoomId))
            {
                matched = roomsById.ContainsKey(rule.RoomId) ||
                          input.Openings.Any(opening => string.Equals(opening.RoomId, rule.RoomId, StringComparison.Ordinal));
            }
            else if (!string.IsNullOrWhiteSpace(rule.ZoneId))
            {
                matched = zonesById.ContainsKey(rule.ZoneId) ||
                          input.Openings.Any(opening => string.Equals(opening.ZoneId, rule.ZoneId, StringComparison.Ordinal));
            }

            if (!matched)
            {
                diagnostics.Add(CreateWarning(
                    "AE-VENT-ZONE-CONTROL-TARGET-UNMATCHED",
                    $"Control rule '{rule.RuleId}' target could not be matched to openings/topology."));
            }
        }

        var orderedDiagnostics = diagnostics
            .OrderByDescending(diagnostic => diagnostic.Severity)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToArray();

        return new NaturalVentilationZoneIntegrationValidationResult(
            IsValid: diagnostics.All(diagnostic => diagnostic.Severity != CalculationDiagnosticSeverity.Error),
            Diagnostics: orderedDiagnostics);
    }

    private static void ValidateOpeningBoundaryKind(
        NaturalVentilationOpeningGeometry opening,
        ThermalTopologySurface surface,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (!string.IsNullOrWhiteSpace(opening.RoomId) &&
            !string.IsNullOrWhiteSpace(surface.RoomId) &&
            !string.Equals(opening.RoomId, surface.RoomId, StringComparison.Ordinal))
        {
            diagnostics.Add(CreateError(
                "AE-VENT-ZONE-OPENING-BOUNDARY-ROOM-MISMATCH",
                $"Opening '{opening.OpeningId}' room '{opening.RoomId}' does not match boundary room '{surface.RoomId}'."));
        }

        if (!string.IsNullOrWhiteSpace(opening.ZoneId) &&
            !string.IsNullOrWhiteSpace(surface.ZoneId) &&
            !string.Equals(opening.ZoneId, surface.ZoneId, StringComparison.Ordinal))
        {
            diagnostics.Add(CreateError(
                "AE-VENT-ZONE-OPENING-BOUNDARY-ZONE-MISMATCH",
                $"Opening '{opening.OpeningId}' zone '{opening.ZoneId}' does not match boundary zone '{surface.ZoneId}'."));
        }

        if (surface.BoundaryKind == ThermalBoundaryKind.Outdoor)
            return;

        var code = surface.BoundaryKind switch
        {
            ThermalBoundaryKind.Ground => "AE-VENT-ZONE-OPENING-BOUNDARY-GROUND-UNSUPPORTED",
            ThermalBoundaryKind.Adiabatic => "AE-VENT-ZONE-OPENING-BOUNDARY-ADIABATIC-UNSUPPORTED",
            ThermalBoundaryKind.AdjacentConditionedZone => "AE-VENT-ZONE-OPENING-BOUNDARY-ADJACENT-CONDITIONED-UNSUPPORTED",
            ThermalBoundaryKind.AdjacentUnconditionedZone => "AE-VENT-ZONE-OPENING-BOUNDARY-ADJACENT-UNCONDITIONED-UNSUPPORTED",
            ThermalBoundaryKind.InternalPartition => "AE-VENT-ZONE-OPENING-BOUNDARY-INTERNAL-UNSUPPORTED",
            _ => "AE-VENT-ZONE-OPENING-BOUNDARY-NONEXTERIOR-UNSUPPORTED"
        };

        diagnostics.Add(CreateError(
            code,
            $"Opening '{opening.OpeningId}' is attached to non-exterior boundary '{surface.SurfaceId}' ({surface.BoundaryKind})."));
    }

    private static string BuildTargetKey(NaturalVentilationHourlyZoneEnvironment environment)
    {
        if (!string.IsNullOrWhiteSpace(environment.RoomId))
            return $"room:{environment.RoomId}";
        if (!string.IsNullOrWhiteSpace(environment.ZoneId))
            return $"zone:{environment.ZoneId}";
        return "global";
    }

    private static string BuildTargetHourKey(NaturalVentilationHourlyZoneEnvironment environment) =>
        $"{BuildTargetKey(environment)}:hour:{environment.HourIndex}";

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        NaturalVentilationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Error,
            code,
            message,
            StandardCalculationStage.InputPreparation,
            "NaturalVentilationZoneIntegrationValidator");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        NaturalVentilationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.InputPreparation,
            "NaturalVentilationZoneIntegrationValidator");
}
