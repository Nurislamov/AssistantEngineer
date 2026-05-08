using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public sealed class NaturalVentilationHourlyInputBuilder : INaturalVentilationHourlyInputBuilder
{
    private readonly INaturalVentilationControlledAirflowInputBuilder? _controlledAirflowInputBuilder;

    public NaturalVentilationHourlyInputBuilder(
        INaturalVentilationControlledAirflowInputBuilder? controlledAirflowInputBuilder = null)
    {
        _controlledAirflowInputBuilder = controlledAirflowInputBuilder;
    }

    public NaturalVentilationCalculationInput BuildHourlyAirflowInput(
        NaturalVentilationZoneIntegrationInput input,
        NaturalVentilationHourlyZoneEnvironment environment,
        IReadOnlyList<NaturalVentilationOpeningOperationResult> operationsForHour)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(operationsForHour);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(environment.Diagnostics);

        var matchingOpenings = input.Openings
            .Where(opening => MatchesEnvironment(opening, environment))
            .ToArray();

        if (matchingOpenings.Length == 0)
        {
            diagnostics.Add(CreateWarning(
                "AE-VENT-ZONE-NO-MATCHING-OPENINGS",
                $"No openings matched hourly environment target room '{environment.RoomId}' zone '{environment.ZoneId}' at hour {environment.HourIndex}."));
        }

        var mappedOpenings = new List<NaturalVentilationOpeningGeometry>();
        foreach (var opening in matchingOpenings)
        {
            var openingDiagnostics = new List<StandardCalculationDiagnostic>();
            openingDiagnostics.AddRange(opening.Diagnostics);

            var candidates = operationsForHour
                .Where(operation => operation.HourIndex == environment.HourIndex)
                .Where(operation => MatchesOperation(opening, operation, environment))
                .ToArray();

            double openingFraction;
            if (candidates.Length == 0)
            {
                openingFraction = 0.0;
                openingDiagnostics.Add(CreateWarning(
                    "AE-VENT-ZONE-OPENING-NO-OPERATION",
                    $"Opening '{opening.OpeningId}' has no matching operation at hour {environment.HourIndex}; opening fraction defaulted to zero."));
            }
            else
            {
                openingFraction = candidates.Max(operation => operation.OpeningFraction);
                if (candidates.Length > 1)
                {
                    openingDiagnostics.Add(CreateInfo(
                        "AE-VENT-ZONE-MULTIPLE-OPERATIONS-MAX-FRACTION-USED",
                        $"Opening '{opening.OpeningId}' has multiple operations at hour {environment.HourIndex}; maximum opening fraction was used."));
                }
            }

            mappedOpenings.Add(opening with
            {
                OpeningFraction = openingFraction,
                Diagnostics = openingDiagnostics
            });
        }

        var airflowEnvironment = new NaturalVentilationEnvironment(
            IndoorTemperatureCelsius: environment.IndoorTemperatureCelsius,
            OutdoorTemperatureCelsius: environment.OutdoorTemperatureCelsius,
            WindSpeedMetersPerSecond: environment.WindSpeedMetersPerSecond,
            WindSpeedHeightMeters: null,
            OpeningReferenceHeightMeters: null,
            OutdoorAirDensityKgPerCubicMeter: environment.AirDensityKgPerCubicMeter ?? input.DefaultAirDensityKgPerCubicMeter,
            IndoorAirDensityKgPerCubicMeter: environment.AirDensityKgPerCubicMeter ?? input.DefaultAirDensityKgPerCubicMeter,
            AtmosphericPressurePa: null,
            Source: environment.Source ?? input.Source,
            Diagnostics: diagnostics);

        var hourlyInput = new NaturalVentilationCalculationInput(
            CalculationId: $"{input.CalculationId}:hour:{environment.HourIndex}:{environment.RoomId ?? environment.ZoneId ?? "global"}",
            FlowConfiguration: input.FlowConfiguration,
            Openings: mappedOpenings,
            Environment: airflowEnvironment,
            DisclosureOverride: input.DisclosureOverride,
            Source: input.Source);

        NaturalVentilationCalculationInput resultInput;
        if (_controlledAirflowInputBuilder is not null)
        {
            resultInput = _controlledAirflowInputBuilder.BuildHourlyAirflowInput(hourlyInput, operationsForHour);
        }
        else
        {
            resultInput = hourlyInput;
        }

        var resultDiagnostics = new List<StandardCalculationDiagnostic>();
        resultDiagnostics.AddRange(resultInput.Environment.Diagnostics);
        resultDiagnostics.Add(CreateInfo(
            "AE-VENT-ZONE-HOURLY-AIRFLOW-INPUT-BUILT",
            $"Hourly airflow input was built for hour {environment.HourIndex}."));

        return resultInput with
        {
            Environment = resultInput.Environment with
            {
                Diagnostics = resultDiagnostics
            }
        };
    }

    private static bool MatchesEnvironment(
        NaturalVentilationOpeningGeometry opening,
        NaturalVentilationHourlyZoneEnvironment environment)
    {
        if (!string.IsNullOrWhiteSpace(environment.RoomId))
        {
            if (string.Equals(opening.RoomId, environment.RoomId, StringComparison.Ordinal))
                return true;

            if (string.IsNullOrWhiteSpace(opening.RoomId))
            {
                if (string.IsNullOrWhiteSpace(opening.ZoneId))
                    return true;

                return string.IsNullOrWhiteSpace(environment.ZoneId) ||
                       string.Equals(opening.ZoneId, environment.ZoneId, StringComparison.Ordinal);
            }

            return false;
        }

        if (!string.IsNullOrWhiteSpace(environment.ZoneId))
        {
            if (!string.IsNullOrWhiteSpace(opening.RoomId))
                return false;

            if (string.IsNullOrWhiteSpace(opening.ZoneId))
                return true;

            return string.Equals(opening.ZoneId, environment.ZoneId, StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(opening.RoomId) && string.IsNullOrWhiteSpace(opening.ZoneId);
    }

    private static bool MatchesOperation(
        NaturalVentilationOpeningGeometry opening,
        NaturalVentilationOpeningOperationResult operation,
        NaturalVentilationHourlyZoneEnvironment environment)
    {
        if (!string.IsNullOrWhiteSpace(operation.OpeningId))
        {
            return string.Equals(operation.OpeningId, opening.OpeningId, StringComparison.Ordinal);
        }

        var resolvedRoomId = opening.RoomId ?? environment.RoomId;
        var resolvedZoneId = opening.ZoneId ?? environment.ZoneId;

        var roomMatches = string.IsNullOrWhiteSpace(operation.RoomId) ||
                          string.Equals(operation.RoomId, resolvedRoomId, StringComparison.Ordinal);
        var zoneMatches = string.IsNullOrWhiteSpace(operation.ZoneId) ||
                          string.Equals(operation.ZoneId, resolvedZoneId, StringComparison.Ordinal);

        return roomMatches && zoneMatches;
    }

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        NaturalVentilationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            StandardCalculationStage.InputPreparation,
            "NaturalVentilationHourlyInputBuilder");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        NaturalVentilationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.InputPreparation,
            "NaturalVentilationHourlyInputBuilder");
}
