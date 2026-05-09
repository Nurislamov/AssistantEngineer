using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public sealed class NaturalVentilationControlRuleValidator : INaturalVentilationControlRuleValidator
{
    public NaturalVentilationControlRuleValidationResult Validate(
        IReadOnlyList<NaturalVentilationOpeningControlRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        foreach (var rule in rules)
        {
            diagnostics.AddRange(rule.Diagnostics);

            if (string.IsNullOrWhiteSpace(rule.RuleId))
            {
                diagnostics.Add(CreateError(
                    "AE-VENT-CONTROL-RULE-ID-MISSING",
                    "Natural ventilation control rule id is required."));
            }

            if (rule.ControlMode == NaturalVentilationControlMode.Unknown)
            {
                diagnostics.Add(CreateError(
                    "AE-VENT-CONTROL-MODE-UNKNOWN",
                    $"Control rule '{rule.RuleId}' control mode must not be Unknown."));
            }

            if (string.IsNullOrWhiteSpace(rule.OpeningId) &&
                string.IsNullOrWhiteSpace(rule.RoomId) &&
                string.IsNullOrWhiteSpace(rule.ZoneId))
            {
                diagnostics.Add(CreateError(
                    "AE-VENT-CONTROL-TARGET-MISSING",
                    $"Control rule '{rule.RuleId}' must target opening, room, or zone."));
            }

            ValidateFractionRange(
                rule.FixedOpeningFraction,
                $"Control rule '{rule.RuleId}' fixed opening fraction must be within [0,1].",
                diagnostics);
            ValidateFractionRange(
                rule.MinimumOpeningFraction,
                $"Control rule '{rule.RuleId}' minimum opening fraction must be within [0,1].",
                diagnostics);
            ValidateFractionRange(
                rule.MaximumOpeningFraction,
                $"Control rule '{rule.RuleId}' maximum opening fraction must be within [0,1].",
                diagnostics);

            if (rule.MinimumOpeningFraction.HasValue &&
                rule.MaximumOpeningFraction.HasValue &&
                rule.MinimumOpeningFraction.Value > rule.MaximumOpeningFraction.Value)
            {
                diagnostics.Add(CreateError(
                    "AE-VENT-CONTROL-MIN-GREATER-THAN-MAX",
                    $"Control rule '{rule.RuleId}' minimum opening fraction must not exceed maximum opening fraction."));
            }

            if (rule.ControlMode == NaturalVentilationControlMode.Temperature ||
                rule.ControlMode == NaturalVentilationControlMode.TemperatureDriven ||
                rule.ControlMode == NaturalVentilationControlMode.CoolingAssist ||
                rule.ControlMode == NaturalVentilationControlMode.OccupancyAndTemperature ||
                rule.ControlMode == NaturalVentilationControlMode.NightVentilation ||
                rule.ControlMode == NaturalVentilationControlMode.NightPurge)
            {
                var hasThreshold =
                    rule.IndoorTemperatureOpenAboveCelsius.HasValue ||
                    rule.IndoorTemperatureCloseBelowCelsius.HasValue ||
                    rule.OutdoorTemperatureMinimumCelsius.HasValue ||
                    rule.OutdoorTemperatureMaximumCelsius.HasValue ||
                    rule.IndoorOutdoorTemperatureDifferenceMinimumKelvin.HasValue;

                if (!hasThreshold)
                {
                    diagnostics.Add(CreateWarning(
                        "AE-VENT-CONTROL-TEMPERATURE-THRESHOLD-MISSING",
                        $"Control rule '{rule.RuleId}' temperature-driven mode has no thresholds configured."));
                }
            }

            if (rule.ControlMode == NaturalVentilationControlMode.Schedule &&
                string.IsNullOrWhiteSpace(rule.ScheduleId))
            {
                diagnostics.Add(CreateWarning(
                    "AE-VENT-CONTROL-SCHEDULE-MISSING",
                    $"Control rule '{rule.RuleId}' schedule mode has no ScheduleId; runtime context schedule fraction is expected."));
            }

            if ((rule.ControlMode == NaturalVentilationControlMode.Occupancy ||
                 rule.ControlMode == NaturalVentilationControlMode.OccupancyAndTemperature) &&
                string.IsNullOrWhiteSpace(rule.OccupancyProfileId))
            {
                diagnostics.Add(CreateWarning(
                    "AE-VENT-CONTROL-OCCUPANCY-MISSING",
                    $"Control rule '{rule.RuleId}' occupancy mode has no OccupancyProfileId; runtime context occupancy fraction is expected."));
            }

            if (rule.ControlMode == NaturalVentilationControlMode.NightVentilation &&
                (rule.NightVentilationMode == NaturalVentilationNightVentilationMode.Disabled ||
                 rule.NightVentilationMode == NaturalVentilationNightVentilationMode.Unknown))
            {
                diagnostics.Add(CreateWarning(
                    "AE-VENT-CONTROL-NIGHT-MODE-INCOMPLETE",
                    $"Control rule '{rule.RuleId}' night ventilation mode is not fully configured."));
            }

            if (rule.MaximumWindSpeedMetersPerSecond.HasValue &&
                (!double.IsFinite(rule.MaximumWindSpeedMetersPerSecond.Value) ||
                 rule.MaximumWindSpeedMetersPerSecond.Value < 0.0))
            {
                diagnostics.Add(CreateError(
                    "AE-VENT-CONTROL-WIND-LIMIT-INVALID",
                    $"Control rule '{rule.RuleId}' maximum wind speed must be finite and non-negative."));
            }

            ValidateFractionRange(
                rule.FallbackOpeningFraction,
                $"Control rule '{rule.RuleId}' fallback opening fraction must be within [0,1].",
                diagnostics);
        }

        diagnostics = diagnostics
            .OrderByDescending(diagnostic => diagnostic.Severity)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToList();

        return new NaturalVentilationControlRuleValidationResult(
            IsValid: diagnostics.All(diagnostic => diagnostic.Severity != CalculationDiagnosticSeverity.Error),
            Diagnostics: diagnostics);
    }

    private static void ValidateFractionRange(
        double? fraction,
        string message,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (!fraction.HasValue)
            return;

        if (!double.IsFinite(fraction.Value) || fraction.Value < 0.0 || fraction.Value > 1.0)
        {
            diagnostics.Add(CreateError("AE-VENT-CONTROL-FRACTION-INVALID", message));
        }
    }

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        NaturalVentilationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Error,
            code,
            message,
            StandardCalculationStage.InputPreparation,
            "NaturalVentilationControlRuleValidator");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        NaturalVentilationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.InputPreparation,
            "NaturalVentilationControlRuleValidator");
}
