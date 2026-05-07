using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.EquipmentSizing;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.EquipmentSizing;

public sealed class EquipmentSizingEngine
{
    private const double DocumentedDefaultSafetyFactor = 1.10;
    private readonly TimeProvider _timeProvider;

    public EquipmentSizingEngine(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Result<EquipmentSizingResult> Calculate(EquipmentSizingInput input)
    {
        if (input is null)
            return Result<EquipmentSizingResult>.Validation("Equipment sizing input is required.");

        if (input.Candidates is null)
            return Result<EquipmentSizingResult>.Validation("Equipment sizing candidates are required.");

        var diagnostics = Validate(input);

        if (HasErrorDiagnostics(diagnostics))
        {
            return Result<EquipmentSizingResult>.Validation(
                BuildValidationFailureMessage(
                    "Equipment sizing validation failed",
                    diagnostics));
        }

        var heatingSafetyFactor = input.HeatingSafetyFactor ?? input.SafetyFactor ?? DocumentedDefaultSafetyFactor;
        var coolingSafetyFactor = input.CoolingSafetyFactor ?? input.SafetyFactor ?? DocumentedDefaultSafetyFactor;

        if (!input.HeatingSafetyFactor.HasValue && !input.SafetyFactor.HasValue)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "EquipmentSizing.DefaultHeatingSafetyFactorUsed",
                $"Heating safety factor was not supplied and documented default {DocumentedDefaultSafetyFactor} was used.",
                input.DiagnosticsContext));
        }

        if (!input.CoolingSafetyFactor.HasValue && !input.SafetyFactor.HasValue)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "EquipmentSizing.DefaultCoolingSafetyFactorUsed",
                $"Cooling safety factor was not supplied and documented default {DocumentedDefaultSafetyFactor} was used.",
                input.DiagnosticsContext));
        }

        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Info,
            "EquipmentSizing.HeatingSafetyFactorApplied",
            FormattableString.Invariant($"Heating safety factor {Round(heatingSafetyFactor)} was applied."),
            input.DiagnosticsContext));

        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Info,
            "EquipmentSizing.CoolingSafetyFactorApplied",
            FormattableString.Invariant($"Cooling safety factor {Round(coolingSafetyFactor)} was applied."),
            input.DiagnosticsContext));

        if (input.MaximumOversizingPercent.HasValue)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Info,
                "EquipmentSizing.MaximumOversizingLimitApplied",
                FormattableString.Invariant($"Maximum oversizing limit {Round(input.MaximumOversizingPercent.Value)}% was applied."),
                input.DiagnosticsContext));
        }

        var requiredHeatingWithReserve = Math.Max(0, input.RequiredHeatingLoadW) * heatingSafetyFactor;
        var requiredCoolingWithReserve = Math.Max(0, input.RequiredCoolingLoadW) * coolingSafetyFactor;

        var accepted = new List<EquipmentSizingRecommendedItem>();
        var rejected = new List<EquipmentSizingRejectedItem>();

        if (input.Candidates.Count == 0)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "EquipmentSizing.NoEquipmentFound",
                "No equipment candidates were supplied.",
                input.DiagnosticsContext));
        }

        foreach (var candidate in input.Candidates)
        {
            var reasons = RejectReasons(
                candidate,
                input,
                requiredHeatingWithReserve,
                requiredCoolingWithReserve,
                input.MaximumOversizingPercent);

            if (reasons.Count > 0)
            {
                rejected.Add(new EquipmentSizingRejectedItem(
                    candidate.EquipmentId,
                    candidate.Name,
                    candidate.Model,
                    reasons));

                continue;
            }

            var heatingMargin = (candidate.HeatingCapacityW ?? 0) - requiredHeatingWithReserve;
            var coolingMargin = (candidate.CoolingCapacityW ?? 0) - requiredCoolingWithReserve;

            var heatingMarginPercent = requiredHeatingWithReserve > 0
                ? heatingMargin / requiredHeatingWithReserve * 100.0
                : (double?)null;

            var coolingMarginPercent = requiredCoolingWithReserve > 0
                ? coolingMargin / requiredCoolingWithReserve * 100.0
                : (double?)null;

            var score = CalculateScore(
                heatingMarginPercent,
                coolingMarginPercent);

            accepted.Add(new EquipmentSizingRecommendedItem(
                candidate.EquipmentId,
                candidate.Name,
                candidate.Model,
                candidate.HeatingCapacityW,
                candidate.CoolingCapacityW,
                Round(heatingMargin),
                Round(coolingMargin),
                heatingMarginPercent.HasValue ? Round(heatingMarginPercent.Value) : null,
                coolingMarginPercent.HasValue ? Round(coolingMarginPercent.Value) : null,
                Round(score),
                ["Accepted by deterministic capacity sizing."]));
        }

        var recommended = accepted
            .OrderBy(item => PositiveMarginSum(item))
            .ThenByDescending(item => item.Score)
            .ThenBy(item => item.EquipmentId)
            .ToArray();

        var bestMatch = recommended.FirstOrDefault();

        if (recommended.Length == 0 && input.Candidates.Count > 0)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "EquipmentSizing.NoRecommendedEquipment",
                "No equipment candidate satisfied the sizing requirements.",
                input.DiagnosticsContext));
        }

        return Result<EquipmentSizingResult>.Success(new EquipmentSizingResult(
            input.TargetId,
            input.TargetType,
            Round(Math.Max(0, input.RequiredHeatingLoadW)),
            Round(Math.Max(0, input.RequiredCoolingLoadW)),
            Round(coolingSafetyFactor),
            Round(heatingSafetyFactor),
            Round(coolingSafetyFactor),
            Round(requiredHeatingWithReserve),
            Round(requiredCoolingWithReserve),
            recommended,
            rejected,
            bestMatch,
            diagnostics,
            _timeProvider.GetUtcNow()));
    }

    private static List<CalculationDiagnostic> Validate(EquipmentSizingInput input)
    {
        var diagnostics = new List<CalculationDiagnostic>();

        if (input.TargetId < 0)
        {
            diagnostics.Add(Error(
                "EquipmentSizing.InvalidTargetId",
                "Target id must not be negative.",
                input.DiagnosticsContext));
        }

        if (input.RequiredHeatingLoadW < 0)
        {
            diagnostics.Add(Error(
                "EquipmentSizing.InvalidHeatingLoad",
                "Required heating load cannot be negative.",
                input.DiagnosticsContext));
        }

        if (input.RequiredCoolingLoadW < 0)
        {
            diagnostics.Add(Error(
                "EquipmentSizing.InvalidCoolingLoad",
                "Required cooling load cannot be negative.",
                input.DiagnosticsContext));
        }

        if (input.SafetyFactor is <= 0)
        {
            diagnostics.Add(Error(
                "EquipmentSizing.InvalidSafetyFactor",
                "Safety factor must be greater than zero.",
                input.DiagnosticsContext));
        }

        if (input.HeatingSafetyFactor is <= 0)
        {
            diagnostics.Add(Error(
                "EquipmentSizing.InvalidHeatingSafetyFactor",
                "Heating safety factor must be greater than zero.",
                input.DiagnosticsContext));
        }

        if (input.CoolingSafetyFactor is <= 0)
        {
            diagnostics.Add(Error(
                "EquipmentSizing.InvalidCoolingSafetyFactor",
                "Cooling safety factor must be greater than zero.",
                input.DiagnosticsContext));
        }

        if (input.MaximumOversizingPercent is <= 0)
        {
            diagnostics.Add(Error(
                "EquipmentSizing.InvalidMaximumOversizingPercent",
                "Maximum oversizing percent must be greater than zero.",
                input.DiagnosticsContext));
        }


        return diagnostics;
    }

    private static List<string> RejectReasons(
        EquipmentSizingCandidateInput candidate,
        EquipmentSizingInput input,
        double requiredHeatingWithReserve,
        double requiredCoolingWithReserve,
        double? maximumOversizingPercent)
    {
        var reasons = new List<string>();

        if (!candidate.IsActive)
            reasons.Add("inactive equipment");

        if (!string.IsNullOrWhiteSpace(input.EquipmentType) &&
            !string.Equals(input.EquipmentType, candidate.EquipmentType, StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("wrong type");
        }

        if (requiredHeatingWithReserve > 0)
        {
            if (!candidate.HeatingCapacityW.HasValue || candidate.HeatingCapacityW.Value <= 0)
                reasons.Add("missing heating capacity");
            else if (candidate.HeatingCapacityW.Value < requiredHeatingWithReserve)
                reasons.Add("insufficient heating capacity");
            else if (IsOversized(candidate.HeatingCapacityW.Value, requiredHeatingWithReserve, maximumOversizingPercent))
                reasons.Add("excessive heating oversizing");
        }

        if (requiredCoolingWithReserve > 0)
        {
            if (!candidate.CoolingCapacityW.HasValue || candidate.CoolingCapacityW.Value <= 0)
                reasons.Add("missing cooling capacity");
            else if (candidate.CoolingCapacityW.Value < requiredCoolingWithReserve)
                reasons.Add("insufficient cooling capacity");
            else if (IsOversized(candidate.CoolingCapacityW.Value, requiredCoolingWithReserve, maximumOversizingPercent))
                reasons.Add("excessive cooling oversizing");
        }

        if (requiredHeatingWithReserve <= 0 && requiredCoolingWithReserve <= 0)
            reasons.Add("no matching mode");

        return reasons;
    }

    private static bool IsOversized(
        double capacityW,
        double requiredWithReserveW,
        double? maximumOversizingPercent)
    {
        if (!maximumOversizingPercent.HasValue || requiredWithReserveW <= 0)
            return false;

        var marginPercent = (capacityW - requiredWithReserveW) / requiredWithReserveW * 100.0;

        return marginPercent > maximumOversizingPercent.Value;
    }

    private static double CalculateScore(
        double? heatingMarginPercent,
        double? coolingMarginPercent)
    {
        var margins = new[] { heatingMarginPercent, coolingMarginPercent }
            .Where(value => value.HasValue)
            .Select(value => Math.Abs(value!.Value))
            .ToArray();

        if (margins.Length == 0)
            return 0;

        return Math.Max(0, 100.0 - margins.Average());
    }

    private static double PositiveMarginSum(
        EquipmentSizingRecommendedItem item) =>
        Math.Max(0, item.HeatingMarginW) + Math.Max(0, item.CoolingMarginW);

    private static CalculationDiagnostic Error(
        string code,
        string message,
        string? context) =>
        new(CalculationDiagnosticSeverity.Error, code, message, context);

    private static bool HasErrorDiagnostics(
        IEnumerable<CalculationDiagnostic> diagnostics) =>
        diagnostics.Any(diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Error);

    private static string BuildValidationFailureMessage(
        string prefix,
        IEnumerable<CalculationDiagnostic> diagnostics)
    {
        var errorCodes = diagnostics
            .Where(diagnostic => diagnostic.Severity == CalculationDiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.Code)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return errorCodes.Length == 0
            ? prefix + "."
            : $"{prefix}: {string.Join(", ", errorCodes)}.";
    }

    private static double Round(
        double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);
}


