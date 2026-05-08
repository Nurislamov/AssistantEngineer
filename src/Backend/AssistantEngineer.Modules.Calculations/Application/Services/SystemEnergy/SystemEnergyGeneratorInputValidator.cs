using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class SystemEnergyGeneratorInputValidator : ISystemEnergyGeneratorInputValidator
{
    private const string Source = "SystemEnergyGeneratorInputValidator";

    public SystemEnergyGeneratorInputValidationResult Validate(SystemEnergyGeneratorCalculationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var diagnostics = new List<StandardCalculationDiagnostic>();

        if (string.IsNullOrWhiteSpace(input.CalculationId))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-GEN-CALCULATION-ID-MISSING",
                "Generator calculation id is required."));
        }

        if (input.GenerationHandoff is null)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-GEN-HANDOFF-MISSING",
                "Generation handoff is required."));
        }
        else
        {
            foreach (var entry in input.GenerationHandoff.HourlySystemLoadBeforeGenerationByEndUseKWh8760)
            {
                if (!SystemEnergyProfileHelper.IsValidProfile(entry.Value, SystemEnergyProfileHelper.HoursPerYear))
                {
                    diagnostics.Add(CreateError(
                        "AE-SYS-GEN-HANDOFF-PROFILE-INVALID",
                        $"Generation handoff end-use '{entry.Key}' hourly profile must have 8760 finite non-negative values."));
                }
            }
        }

        if (input.GeneratorSet is null)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-GEN-SET-MISSING",
                "Generator set is required."));
            return BuildResult(diagnostics);
        }

        if (string.IsNullOrWhiteSpace(input.GeneratorSet.GeneratorSetId))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-GEN-SET-ID-MISSING",
                "Generator set id is required."));
        }

        if (input.GeneratorSet.LoadSplitMode == SystemEnergyLoadSplitMode.Unknown)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-GEN-SPLIT-MODE-UNKNOWN",
                "Generator load split mode cannot be Unknown for calculation-ready input."));
        }

        if (input.GeneratorSet.Generators.Count == 0)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-GEN-GENERATORS-MISSING",
                "At least one generator is required."));
        }

        foreach (var generator in input.GeneratorSet.Generators)
        {
            ValidateGenerator(generator, diagnostics);
        }

        return BuildResult(diagnostics);
    }

    private static void ValidateGenerator(
        SystemEnergyGeneratorInput generator,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(generator.GeneratorId))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-GEN-ID-MISSING",
                "Generator id is required."));
        }

        if (generator.GeneratorKind == SystemEnergyGeneratorKind.Unknown)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-GEN-KIND-UNKNOWN",
                $"Generator '{generator.GeneratorId}' kind cannot be Unknown."));
        }

        if (generator.CalculationMode == SystemEnergyGeneratorCalculationMode.Unknown)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-GEN-MODE-UNKNOWN",
                $"Generator '{generator.GeneratorId}' calculation mode cannot be Unknown."));
        }

        if (generator.CalculationMode is not SystemEnergyGeneratorCalculationMode.Disabled and not SystemEnergyGeneratorCalculationMode.HandoffOnly &&
            generator.FinalEnergyCarrier == SystemEnergyCarrier.Unknown)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-GEN-CARRIER-UNKNOWN",
                $"Generator '{generator.GeneratorId}' final-energy carrier cannot be Unknown for active calculation."));
        }

        if (generator.CalculationMode != SystemEnergyGeneratorCalculationMode.Disabled &&
            generator.ServedEndUses.Count == 0)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-GEN-ENDUSES-MISSING",
                $"Generator '{generator.GeneratorId}' must declare served end uses."));
        }

        if (generator.Priority < 0)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-GEN-PRIORITY-INVALID",
                $"Generator '{generator.GeneratorId}' priority must be >= 0."));
        }

        if (generator.LoadFraction.HasValue &&
            (!double.IsFinite(generator.LoadFraction.Value) || generator.LoadFraction.Value < 0.0 || generator.LoadFraction.Value > 1.0))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-GEN-LOAD-FRACTION-INVALID",
                $"Generator '{generator.GeneratorId}' load fraction must be within [0, 1]."));
        }

        if (generator.NominalCapacityKWhPerHour.HasValue &&
            (!double.IsFinite(generator.NominalCapacityKWhPerHour.Value) || generator.NominalCapacityKWhPerHour.Value <= 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-GEN-CAPACITY-INVALID",
                $"Generator '{generator.GeneratorId}' nominal capacity must be > 0."));
        }

        if (generator.Efficiency.HasValue &&
            (!double.IsFinite(generator.Efficiency.Value) || generator.Efficiency.Value <= 0.0 || generator.Efficiency.Value > 1.0))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-GEN-EFFICIENCY-INVALID",
                $"Generator '{generator.GeneratorId}' efficiency must be within (0, 1]."));
        }

        if (generator.Cop.HasValue &&
            (!double.IsFinite(generator.Cop.Value) || generator.Cop.Value <= 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-GEN-COP-INVALID",
                $"Generator '{generator.GeneratorId}' COP must be > 0."));
        }

        if (generator.Eer.HasValue &&
            (!double.IsFinite(generator.Eer.Value) || generator.Eer.Value <= 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-GEN-EER-INVALID",
                $"Generator '{generator.GeneratorId}' EER must be > 0."));
        }

        if (generator.SeasonalPerformanceFactor.HasValue &&
            (!double.IsFinite(generator.SeasonalPerformanceFactor.Value) || generator.SeasonalPerformanceFactor.Value <= 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-GEN-SPF-INVALID",
                $"Generator '{generator.GeneratorId}' SPF must be > 0."));
        }

        if (generator.AuxiliaryElectricityFraction.HasValue &&
            (!double.IsFinite(generator.AuxiliaryElectricityFraction.Value) || generator.AuxiliaryElectricityFraction.Value < 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-GEN-AUXILIARY-INVALID",
                $"Generator '{generator.GeneratorId}' auxiliary electricity fraction must be >= 0."));
        }

        if (generator.AuxiliaryElectricityKWhPerKWhOutput.HasValue &&
            (!double.IsFinite(generator.AuxiliaryElectricityKWhPerKWhOutput.Value) || generator.AuxiliaryElectricityKWhPerKWhOutput.Value < 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-GEN-AUXILIARY-INVALID",
                $"Generator '{generator.GeneratorId}' auxiliary kWh per kWh output must be >= 0."));
        }

        if (generator.HourlyLoadFraction8760 is not null)
        {
            var validHourlyFraction = generator.HourlyLoadFraction8760.Count == SystemEnergyProfileHelper.HoursPerYear &&
                                      generator.HourlyLoadFraction8760.All(value => double.IsFinite(value) && value >= 0.0 && value <= 1.0);
            if (!validHourlyFraction)
            {
                diagnostics.Add(CreateError(
                    "AE-SYS-GEN-HOURLY-FRACTION-PROFILE-INVALID",
                    $"Generator '{generator.GeneratorId}' hourly load-fraction profile must have 8760 finite values in [0, 1]."));
            }
        }

        if (generator.HourlyFinalEnergyProfileKWh8760 is not null &&
            !SystemEnergyProfileHelper.IsValidProfile(generator.HourlyFinalEnergyProfileKWh8760, SystemEnergyProfileHelper.HoursPerYear))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-GEN-DIRECT-FINAL-PROFILE-INVALID",
                $"Generator '{generator.GeneratorId}' direct final-energy profile must have 8760 finite non-negative values."));
        }
    }

    private static SystemEnergyGeneratorInputValidationResult BuildResult(
        IReadOnlyList<StandardCalculationDiagnostic> diagnostics) =>
        new(
            IsValid: diagnostics.All(diagnostic => diagnostic.Severity != CalculationDiagnosticSeverity.Error),
            Diagnostics: diagnostics);

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Error,
            code,
            message,
            Source);
}
