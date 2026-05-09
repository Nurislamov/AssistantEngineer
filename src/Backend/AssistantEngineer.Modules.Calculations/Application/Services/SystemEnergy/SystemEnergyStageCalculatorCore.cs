using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

internal static class SystemEnergyStageCalculatorCore
{
    public static SystemEnergyStageCalculationResult CalculateStage(
        SystemEnergyStageCalculationRequest request,
        string source)
    {
        var assumptions = new List<string>();
        var warnings = new List<string>();
        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(request.StageDefinition.Diagnostics ?? []);

        var input = request.InputProfileKWh
            .Select(value => double.IsFinite(value) && value > 0.0 ? value : 0.0)
            .ToArray();
        var loss = new double[input.Length];
        var recovered = new double[input.Length];
        var auxiliary = new double[input.Length];
        var output = new double[input.Length];

        var stage = request.StageDefinition;
        if (stage.CalculationMode == SystemEnergyModuleCalculationMode.FixedEfficiency &&
            (stage.Efficiency is <= 0.0 or > 1.0))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-STAGE-EFFICIENCY-INVALID",
                $"Stage '{stage.StageId}' efficiency must be within (0, 1].",
                source));
        }

        if (stage.LossFraction is < 0.0)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-STAGE-LOSS-FRACTION-INVALID",
                $"Stage '{stage.StageId}' loss fraction must be >= 0.",
                source));
        }

        var recoveredFraction = stage.RecoveredLossFraction ?? 0.0;
        if (!double.IsFinite(recoveredFraction) || recoveredFraction < 0.0 || recoveredFraction > 1.0)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-STAGE-RECOVERED-FRACTION-INVALID",
                $"Stage '{stage.StageId}' recovered-loss fraction must be within [0,1].",
                source));
            recoveredFraction = Math.Clamp(recoveredFraction, 0.0, 1.0);
        }

        var fixedLoss = BuildStageProfile(
            stage.FixedLossProfile,
            input.Length,
            source,
            "AE-SYS-STAGE-LOSS-PROFILE-LENGTH-MISMATCH",
            diagnostics);
        var auxiliaryProfile = BuildStageProfile(
            stage.AuxiliaryEnergyProfile,
            input.Length,
            source,
            "AE-SYS-STAGE-AUXILIARY-PROFILE-LENGTH-MISMATCH",
            diagnostics);

        for (var index = 0; index < input.Length; index++)
        {
            var useful = input[index];
            double stageInput;
            double stageLoss;

            if (stage.CalculationMode == SystemEnergyModuleCalculationMode.FixedEfficiency && stage.Efficiency is > 0.0 and <= 1.0)
            {
                stageInput = useful / stage.Efficiency.Value;
                stageLoss = Math.Max(0.0, stageInput - useful);
            }
            else if (stage.CalculationMode == SystemEnergyModuleCalculationMode.LossFraction && stage.LossFraction is >= 0.0)
            {
                stageLoss = useful * stage.LossFraction.Value;
                stageInput = useful + stageLoss;
            }
            else
            {
                stageLoss = fixedLoss[index];
                stageInput = useful + stageLoss;
            }

            loss[index] = Math.Max(0.0, stageLoss);
            recovered[index] = loss[index] * recoveredFraction;
            auxiliary[index] = auxiliaryProfile[index];
            output[index] = Math.Max(0.0, stageInput - recovered[index]);
        }

        assumptions.Add("Stage convention: output = useful/input-to-stage + losses - recovered losses.");
        assumptions.Add("Recovered losses reduce downstream load and never increase final purchased energy.");
        assumptions.Add("Auxiliary energy is tracked separately from thermal stage output.");

        return new SystemEnergyStageCalculationResult(
            SubsystemKind: stage.SubsystemKind,
            UseKind: request.UseKind,
            InputProfileKWh: input,
            OutputProfileKWh: output,
            LossesProfileKWh: loss,
            RecoveredLossesProfileKWh: recovered,
            AuxiliaryEnergyProfileKWh: auxiliary,
            Diagnostics: SortDiagnostics(diagnostics),
            Assumptions: assumptions.ToArray(),
            Warnings: warnings.ToArray());
    }

    public static IReadOnlyList<double> BuildStageProfile(
        IReadOnlyList<double>? profile,
        int expectedLength,
        string source,
        string mismatchCode,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (profile is null)
            return new double[expectedLength];

        if (profile.Count == expectedLength)
            return profile.Select(value => double.IsFinite(value) && value >= 0.0 ? value : 0.0).ToArray();

        diagnostics.Add(CreateError(
            mismatchCode,
            $"Profile length mismatch: expected {expectedLength}, got {profile.Count}.",
            source));

        if (profile.Count == 12 && expectedLength == 8760)
            return SystemEnergyProfileHelper.ExpandMonthlyToHourly(profile).ToArray();

        var values = new double[expectedLength];
        if (profile.Count == 0)
            return values;

        for (var index = 0; index < expectedLength; index++)
            values[index] = profile[index % profile.Count];

        return values;
    }

    public static IReadOnlyList<StandardCalculationDiagnostic> SortDiagnostics(
        IEnumerable<StandardCalculationDiagnostic> diagnostics) =>
        diagnostics
            .OrderByDescending(item => item.Severity)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.Message, StringComparer.Ordinal)
            .ToArray();

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message,
        string source) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Error,
            code,
            message,
            source);
}
