using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

public sealed class AdjacentUnconditionedZoneTemperatureCalculator : IAdjacentUnconditionedZoneTemperatureCalculator
{
    public AdjacentUnconditionedZoneTemperatureProfileResult Calculate(
        AdjacentUnconditionedZoneTemperatureProfileRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        var assumptions = new List<string>();

        if (string.IsNullOrWhiteSpace(request.ConditionId))
        {
            diagnostics.Add(CreateError(
                "Iso52016.MultiZone.AdjacentUnconditioned.ConditionIdMissing",
                "Adjacent-unconditioned condition id is required."));
        }

        var conditionedProfile = request.ConditionedZoneTemperatureProfileCelsius ?? [];
        var exteriorProfile = request.ExteriorTemperatureProfileCelsius ?? [];

        if (conditionedProfile.Count == 0)
        {
            diagnostics.Add(CreateError(
                "Iso52016.MultiZone.AdjacentUnconditioned.ConditionedProfileMissing",
                "Conditioned-zone temperature profile is required."));
        }

        if (exteriorProfile.Count == 0)
        {
            diagnostics.Add(CreateError(
                "Iso52016.MultiZone.AdjacentUnconditioned.ExteriorProfileMissing",
                "Exterior temperature profile is required."));
        }

        var hourCount = Math.Max(conditionedProfile.Count, exteriorProfile.Count);
        if (hourCount is not (1 or 8760))
        {
            diagnostics.Add(CreateError(
                "Iso52016.MultiZone.AdjacentUnconditioned.ProfileLengthUnsupported",
                $"Profiles must resolve to 1 or 8760 values, but resolved {hourCount}."));
        }

        if (diagnostics.Any(diagnostic => diagnostic.Severity == CalculationDiagnosticSeverity.Error))
        {
            return new AdjacentUnconditionedZoneTemperatureProfileResult(
                ConditionId: request.ConditionId,
                TemperatureProfileCelsius: [],
                Assumptions: assumptions,
                Diagnostics: diagnostics);
        }

        var profile = new double[hourCount];
        switch (request.Mode)
        {
            case AdjacentUnconditionedTemperatureMode.ReductionFactor:
                {
                    var reductionFactor = request.ReductionFactorB;
                    if (!reductionFactor.HasValue)
                    {
                        diagnostics.Add(CreateError(
                            "Iso52016.MultiZone.AdjacentUnconditioned.ReductionFactorMissing",
                            "Reduction-factor mode requires b-factor input."));
                        break;
                    }

                    if (reductionFactor.Value < 0.0 || reductionFactor.Value > 1.0)
                    {
                        diagnostics.Add(CreateError(
                            "Iso52016.MultiZone.AdjacentUnconditioned.ReductionFactorOutOfRange",
                            $"Reduction factor b must be within [0, 1], but got {reductionFactor.Value:0.###}."));
                        break;
                    }

                    assumptions.Add("Reduction-factor convention: T_adj = T_conditioned - b * (T_conditioned - T_exterior).");
                    for (var hour = 0; hour < hourCount; hour++)
                    {
                        var conditioned = ResolveProfileValue(conditionedProfile, hour);
                        var exterior = ResolveProfileValue(exteriorProfile, hour);
                        profile[hour] = conditioned - reductionFactor.Value * (conditioned - exterior);
                    }

                    break;
                }

            case AdjacentUnconditionedTemperatureMode.DeterministicFallback:
                {
                    var weight = Math.Clamp(request.FallbackExteriorWeight, 0.0, 1.0);
                    assumptions.Add("Deterministic fallback: weighted blend of conditioned and exterior with explicit weight/offset.");
                    assumptions.Add($"Fallback parameters: exteriorWeight={weight:0.###}, offsetC={request.FallbackOffsetCelsius:0.###}.");

                    diagnostics.Add(CreateWarning(
                        "Iso52016.MultiZone.AdjacentUnconditioned.DeterministicFallbackAssumption",
                        "Deterministic fallback mode applied due to missing or incomplete adjacent-zone data."));

                    for (var hour = 0; hour < hourCount; hour++)
                    {
                        var conditioned = ResolveProfileValue(conditionedProfile, hour);
                        var exterior = ResolveProfileValue(exteriorProfile, hour);
                        profile[hour] = ((1.0 - weight) * conditioned) + (weight * exterior) + request.FallbackOffsetCelsius;
                    }

                    break;
                }

            default:
                diagnostics.Add(CreateError(
                    "Iso52016.MultiZone.AdjacentUnconditioned.ModeUnsupported",
                    $"Unsupported adjacent-unconditioned mode '{request.Mode}'."));
                break;
        }

        if (diagnostics.Any(diagnostic => diagnostic.Severity == CalculationDiagnosticSeverity.Error))
        {
            return new AdjacentUnconditionedZoneTemperatureProfileResult(
                ConditionId: request.ConditionId,
                TemperatureProfileCelsius: [],
                Assumptions: assumptions,
                Diagnostics: diagnostics);
        }

        return new AdjacentUnconditionedZoneTemperatureProfileResult(
            ConditionId: request.ConditionId,
            TemperatureProfileCelsius: profile,
            Assumptions: assumptions,
            Diagnostics: diagnostics.OrderByDescending(item => item.Severity).ThenBy(item => item.Code, StringComparer.Ordinal).ToArray());
    }

    private static double ResolveProfileValue(
        IReadOnlyList<double> profile,
        int hour)
    {
        if (profile.Count == 1)
            return profile[0];

        if (hour >= 0 && hour < profile.Count)
            return profile[hour];

        return profile[^1];
    }

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        new(
            Severity: CalculationDiagnosticSeverity.Error,
            Code: code,
            Message: message,
            Context: "AdjacentUnconditionedZoneTemperatureCalculator",
            Source: "AdjacentUnconditionedZoneTemperatureCalculator",
            Family: StandardCalculationFamily.ISO52016,
            Stage: StandardCalculationStage.BoundaryCondition);

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        new(
            Severity: CalculationDiagnosticSeverity.Warning,
            Code: code,
            Message: message,
            Context: "AdjacentUnconditionedZoneTemperatureCalculator",
            Source: "AdjacentUnconditionedZoneTemperatureCalculator",
            Family: StandardCalculationFamily.ISO52016,
            Stage: StandardCalculationStage.BoundaryCondition);
}
