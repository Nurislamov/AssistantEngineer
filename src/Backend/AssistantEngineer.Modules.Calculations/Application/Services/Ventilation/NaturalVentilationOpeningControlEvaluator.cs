using AssistantEngineer.Modules.Calculations.Application.Abstractions.Standards;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public sealed class NaturalVentilationOpeningControlEvaluator : INaturalVentilationOpeningControlEvaluator
{
    private static readonly IReadOnlyList<string> RequiredForbiddenClaims =
    [
        "Full ISO compliance",
        "Full EN compliance",
        "StandardReference equivalence",
        "EnergyPlus comparison workflow",
        "ASHRAE 140 / BESTEST-style validation anchor"
    ];

    private readonly INaturalVentilationControlRuleValidator _ruleValidator;
    private readonly IStandardCalculationDisclosureFactory _disclosureFactory;

    public NaturalVentilationOpeningControlEvaluator(
        INaturalVentilationControlRuleValidator ruleValidator,
        IStandardCalculationDisclosureFactory disclosureFactory)
    {
        _ruleValidator = ruleValidator ?? throw new ArgumentNullException(nameof(ruleValidator));
        _disclosureFactory = disclosureFactory ?? throw new ArgumentNullException(nameof(disclosureFactory));
    }

    public NaturalVentilationOpeningOperationResult Evaluate(
        NaturalVentilationOpeningControlRule rule,
        NaturalVentilationHourlyControlContext context)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(context);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(rule.Diagnostics);
        diagnostics.AddRange(context.Diagnostics);

        var reasons = new List<string>();
        var fraction = 0.0;

        switch (rule.ControlMode)
        {
            case NaturalVentilationControlMode.AlwaysClosed:
                fraction = 0.0;
                reasons.Add("AlwaysClosed");
                break;

            case NaturalVentilationControlMode.AlwaysOpen:
                fraction = 1.0;
                reasons.Add("AlwaysOpen");
                break;

            case NaturalVentilationControlMode.FixedFraction:
                if (rule.FixedOpeningFraction.HasValue)
                {
                    fraction = rule.FixedOpeningFraction.Value;
                }
                else
                {
                    diagnostics.Add(CreateWarning(
                        "AE-VENT-CONTROL-FIXED-FRACTION-MISSING",
                        $"Control rule '{rule.RuleId}' fixed fraction mode has no fixed opening fraction configured."));
                }

                reasons.Add("FixedFraction");
                break;

            case NaturalVentilationControlMode.Schedule:
                if (context.ScheduleFraction.HasValue)
                {
                    fraction = context.ScheduleFraction.Value;
                }
                else
                {
                    diagnostics.Add(CreateWarning(
                        "AE-VENT-CONTROL-SCHEDULE-FRACTION-MISSING",
                        $"Control rule '{rule.RuleId}' schedule mode requires context schedule fraction."));
                }

                reasons.Add("Schedule");
                break;

            case NaturalVentilationControlMode.Occupancy:
                fraction = EvaluateOccupancy(rule, context, diagnostics);
                reasons.Add("Occupancy");
                break;

            case NaturalVentilationControlMode.Temperature:
            case NaturalVentilationControlMode.TemperatureDriven:
                fraction = EvaluateTemperature(rule, context, diagnostics);
                reasons.Add("Temperature");
                break;

            case NaturalVentilationControlMode.CoolingAssist:
                fraction = EvaluateCoolingAssist(rule, context, diagnostics);
                reasons.Add("CoolingAssist");
                break;

            case NaturalVentilationControlMode.OccupancyAndTemperature:
                var occupancyFraction = EvaluateOccupancy(rule, context, diagnostics);
                var temperatureFraction = EvaluateTemperature(rule, context, diagnostics);
                fraction = occupancyFraction > 0.0 && temperatureFraction > 0.0
                    ? Math.Min(occupancyFraction, temperatureFraction)
                    : 0.0;
                reasons.Add("OccupancyAndTemperature");
                break;

            case NaturalVentilationControlMode.NightVentilation:
            case NaturalVentilationControlMode.NightPurge:
                if (!context.IsNightHour)
                {
                    diagnostics.Add(CreateInfo(
                        "AE-VENT-CONTROL-NIGHT-INACTIVE",
                        $"Control rule '{rule.RuleId}' night ventilation is inactive because current hour is not night."));
                    fraction = 0.0;
                }
                else
                {
                    var nightFraction = EvaluateNightVentilation(rule, context, diagnostics);
                    fraction = nightFraction;
                }

                reasons.Add("NightVentilation");
                break;

            case NaturalVentilationControlMode.Manual:
            case NaturalVentilationControlMode.Custom:
                if (rule.FixedOpeningFraction.HasValue)
                {
                    fraction = rule.FixedOpeningFraction.Value;
                }
                else if (rule.FallbackOpeningFraction.HasValue)
                {
                    fraction = rule.FallbackOpeningFraction.Value;
                    diagnostics.Add(CreateInfo(
                        "AE-VENT-CONTROL-FALLBACK-FRACTION-USED",
                        $"Control rule '{rule.RuleId}' used configured fallback opening fraction."));
                }
                else
                {
                    diagnostics.Add(CreateWarning(
                        "AE-VENT-CONTROL-FIXED-FRACTION-MISSING",
                        $"Control rule '{rule.RuleId}' manual mode requires fixed opening fraction."));
                }

                reasons.Add("Manual");
                break;

            case NaturalVentilationControlMode.Other:
            case NaturalVentilationControlMode.Unknown:
            default:
                diagnostics.Add(CreateWarning(
                    "AE-VENT-CONTROL-UNKNOWN-NO-FALLBACK",
                    $"Control rule '{rule.RuleId}' has unsupported control mode '{rule.ControlMode}'."));
                fraction = 0.0;
                reasons.Add("Unsupported");
                break;
        }

        fraction = ApplyGlobalLockouts(rule, context, fraction, diagnostics);

        fraction = ApplyFractionLimits(rule, fraction, diagnostics);
        var isOpen = fraction > 0.0;

        var isNightVentilationActive =
            isOpen &&
            context.IsNightHour &&
            (rule.ControlMode == NaturalVentilationControlMode.NightVentilation ||
             rule.ControlMode == NaturalVentilationControlMode.NightPurge ||
             rule.NightVentilationMode is NaturalVentilationNightVentilationMode.Enabled
                 or NaturalVentilationNightVentilationMode.ScheduleDriven
                 or NaturalVentilationNightVentilationMode.TemperatureDriven);

        return new NaturalVentilationOpeningOperationResult(
            RuleId: rule.RuleId,
            OpeningId: rule.OpeningId,
            RoomId: rule.RoomId,
            ZoneId: rule.ZoneId,
            HourIndex: context.HourIndex,
            ControlMode: rule.ControlMode,
            OpeningFraction: fraction,
            IsOpen: isOpen,
            IsNightVentilationActive: isNightVentilationActive,
            ActiveReasons: reasons,
            Diagnostics: diagnostics);
    }

    public NaturalVentilationControlEvaluationResult Evaluate(
        NaturalVentilationControlEvaluationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Rules);
        ArgumentNullException.ThrowIfNull(input.HourlyContexts);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        var validation = _ruleValidator.Validate(input.Rules);
        diagnostics.AddRange(validation.Diagnostics);

        var operations = new List<NaturalVentilationOpeningOperationResult>();
        foreach (var rule in input.Rules)
        {
            foreach (var context in input.HourlyContexts)
            {
                if (!MatchesTarget(rule, context, diagnostics))
                    continue;

                var operation = Evaluate(rule, context);
                operations.Add(operation);
                diagnostics.AddRange(operation.Diagnostics);
            }
        }

        var disclosure = MergeDisclosure(
            _disclosureFactory.CreateNaturalVentilationEn16798Disclosure(),
            input.DisclosureOverride,
            diagnostics);

        return new NaturalVentilationControlEvaluationResult(
            Operations: operations,
            OpeningFractionProfilesByOpeningId: new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal),
            RoomOpeningFractionProfilesByRoomId: new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal),
            ZoneOpeningFractionProfilesByZoneId: new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal),
            Disclosure: disclosure,
            Diagnostics: diagnostics);
    }

    private static bool MatchesTarget(
        NaturalVentilationOpeningControlRule rule,
        NaturalVentilationHourlyControlContext context,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (!string.IsNullOrWhiteSpace(rule.RoomId))
        {
            if (string.IsNullOrWhiteSpace(context.RoomId))
            {
                diagnostics.Add(CreateWarning(
                    "AE-VENT-CONTROL-CONTEXT-ROOM-MISSING",
                    $"Control rule '{rule.RuleId}' targets room '{rule.RoomId}' but context has no room id; rule is still evaluated."));
            }
            else if (!string.Equals(rule.RoomId, context.RoomId, StringComparison.Ordinal))
            {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(rule.ZoneId))
        {
            if (string.IsNullOrWhiteSpace(context.ZoneId))
            {
                diagnostics.Add(CreateWarning(
                    "AE-VENT-CONTROL-CONTEXT-ZONE-MISSING",
                    $"Control rule '{rule.RuleId}' targets zone '{rule.ZoneId}' but context has no zone id; rule is still evaluated."));
            }
            else if (!string.Equals(rule.ZoneId, context.ZoneId, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static double EvaluateOccupancy(
        NaturalVentilationOpeningControlRule rule,
        NaturalVentilationHourlyControlContext context,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (!context.OccupancyFraction.HasValue)
        {
            diagnostics.Add(CreateWarning(
                "AE-VENT-CONTROL-OCCUPANCY-FRACTION-MISSING",
                $"Control rule '{rule.RuleId}' occupancy mode requires context occupancy fraction."));
            return 0.0;
        }

        var occupancy = context.OccupancyFraction.Value;
        if (!double.IsFinite(occupancy))
            return 0.0;

        if (rule.RequiresOccupancy == true)
        {
            return occupancy > 0.0 ? 1.0 : 0.0;
        }

        return occupancy;
    }

    private static double EvaluateTemperature(
        NaturalVentilationOpeningControlRule rule,
        NaturalVentilationHourlyControlContext context,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        var blockedByOutdoorMin = rule.OutdoorTemperatureMinimumCelsius.HasValue &&
                                  context.OutdoorTemperatureCelsius < rule.OutdoorTemperatureMinimumCelsius.Value;
        var blockedByOutdoorMax = rule.OutdoorTemperatureMaximumCelsius.HasValue &&
                                  context.OutdoorTemperatureCelsius > rule.OutdoorTemperatureMaximumCelsius.Value;
        if (blockedByOutdoorMin || blockedByOutdoorMax)
        {
            diagnostics.Add(CreateInfo(
                "AE-VENT-CONTROL-TEMPERATURE-BLOCKED",
                $"Control rule '{rule.RuleId}' temperature opening blocked by outdoor temperature limits."));
            return 0.0;
        }

        var indoorOpenSatisfied = !rule.IndoorTemperatureOpenAboveCelsius.HasValue ||
                                  context.IndoorTemperatureCelsius >= rule.IndoorTemperatureOpenAboveCelsius.Value;
        var indoorCloseBlocked = rule.IndoorTemperatureCloseBelowCelsius.HasValue &&
                                 context.IndoorTemperatureCelsius <= rule.IndoorTemperatureCloseBelowCelsius.Value;

        var deltaTSatisfied = !rule.IndoorOutdoorTemperatureDifferenceMinimumKelvin.HasValue ||
                              Math.Abs(context.IndoorTemperatureCelsius - context.OutdoorTemperatureCelsius) >=
                              rule.IndoorOutdoorTemperatureDifferenceMinimumKelvin.Value;

        if (indoorCloseBlocked)
            return 0.0;

        return indoorOpenSatisfied && deltaTSatisfied ? 1.0 : 0.0;
    }

    private static double EvaluateCoolingAssist(
        NaturalVentilationOpeningControlRule rule,
        NaturalVentilationHourlyControlContext context,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (context.OutdoorTemperatureCelsius >= context.IndoorTemperatureCelsius)
        {
            diagnostics.Add(CreateInfo(
                "AE-VENT-CONTROL-COOLING-ASSIST-OUTDOOR-HOTTER",
                $"Control rule '{rule.RuleId}' cooling-assist opening is blocked because outdoor air is not cooler than indoor air."));
            return 0.0;
        }

        return EvaluateTemperature(rule, context, diagnostics);
    }

    private static double EvaluateNightVentilation(
        NaturalVentilationOpeningControlRule rule,
        NaturalVentilationHourlyControlContext context,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (rule.NightVentilationMode == NaturalVentilationNightVentilationMode.Disabled ||
            rule.NightVentilationMode == NaturalVentilationNightVentilationMode.Unknown)
        {
            diagnostics.Add(CreateInfo(
                "AE-VENT-CONTROL-NIGHT-INACTIVE",
                $"Control rule '{rule.RuleId}' night ventilation mode is disabled or unknown."));
            return 0.0;
        }

        var temperatureFraction = EvaluateTemperature(rule, context, diagnostics);
        if (rule.NightVentilationMode == NaturalVentilationNightVentilationMode.TemperatureDriven)
            return temperatureFraction;

        if (rule.NightVentilationMode == NaturalVentilationNightVentilationMode.ScheduleDriven)
            return context.ScheduleFraction.GetValueOrDefault();

        return Math.Max(temperatureFraction, rule.FixedOpeningFraction ?? 1.0);
    }

    private static double ApplyGlobalLockouts(
        NaturalVentilationOpeningControlRule rule,
        NaturalVentilationHourlyControlContext context,
        double fraction,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (fraction <= 0.0)
            return 0.0;

        if (rule.MaximumWindSpeedMetersPerSecond.HasValue &&
            context.WindSpeedMetersPerSecond.HasValue &&
            context.WindSpeedMetersPerSecond.Value > rule.MaximumWindSpeedMetersPerSecond.Value)
        {
            diagnostics.Add(CreateInfo(
                "AE-VENT-CONTROL-WIND-LIMIT-BLOCKED",
                $"Control rule '{rule.RuleId}' opening is blocked by maximum wind speed constraint."));
            return 0.0;
        }

        if (rule.HeatingLockoutEnabled == true &&
            context.HeatingModeActive == true)
        {
            diagnostics.Add(CreateInfo(
                "AE-VENT-CONTROL-HEATING-LOCKOUT-BLOCKED",
                $"Control rule '{rule.RuleId}' opening is blocked by heating lockout."));
            return 0.0;
        }

        if (rule.CoolingLockoutEnabled == true &&
            context.CoolingModeActive == true)
        {
            diagnostics.Add(CreateInfo(
                "AE-VENT-CONTROL-COOLING-LOCKOUT-BLOCKED",
                $"Control rule '{rule.RuleId}' opening is blocked by cooling lockout."));
            return 0.0;
        }

        return fraction;
    }

    private static double ApplyFractionLimits(
        NaturalVentilationOpeningControlRule rule,
        double fraction,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        var original = fraction;

        if (fraction > 0.0 && rule.MinimumOpeningFraction.HasValue)
        {
            fraction = Math.Max(fraction, rule.MinimumOpeningFraction.Value);
        }

        if (rule.MaximumOpeningFraction.HasValue)
        {
            fraction = Math.Min(fraction, rule.MaximumOpeningFraction.Value);
        }

        var clamped = Math.Clamp(fraction, 0.0, 1.0);
        if (!NearlyEqual(original, clamped))
        {
            diagnostics.Add(CreateInfo(
                "AE-VENT-CONTROL-FRACTION-CLAMPED",
                $"Control rule '{rule.RuleId}' opening fraction was clamped from {original:F3} to {clamped:F3}."));
        }

        return clamped;
    }

    private static bool NearlyEqual(double left, double right) =>
        Math.Abs(left - right) < 1e-12;

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
            StandardCalculationStage.ProfileExpansion,
            "NaturalVentilationOpeningControlEvaluator");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        NaturalVentilationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.ProfileExpansion,
            "NaturalVentilationOpeningControlEvaluator");
}
