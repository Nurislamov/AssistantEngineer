using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class SystemEnergyModuleChainInputValidator : ISystemEnergyModuleChainInputValidator
{
    private const string Source = "SystemEnergyModuleChainInputValidator";
    private readonly ISystemEnergyUsefulLoadValidator _usefulLoadValidator;

    public SystemEnergyModuleChainInputValidator(ISystemEnergyUsefulLoadValidator usefulLoadValidator)
    {
        _usefulLoadValidator = usefulLoadValidator ?? throw new ArgumentNullException(nameof(usefulLoadValidator));
    }

    public SystemEnergyModuleChainValidationResult Validate(SystemEnergyModuleChainInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var diagnostics = new List<StandardCalculationDiagnostic>();

        if (string.IsNullOrWhiteSpace(input.CalculationId))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-CHAIN-CALCULATION-ID-MISSING",
                "System-energy module-chain calculation id is required."));
        }

        if (input.UsefulLoadSet is null)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-CHAIN-USEFUL-LOADSET-MISSING",
                "Module-chain useful load-set is required."));
        }
        else
        {
            var usefulValidation = _usefulLoadValidator.Validate(input.UsefulLoadSet);
            diagnostics.AddRange(usefulValidation.Diagnostics);
        }

        foreach (var module in input.Modules)
        {
            ValidateModule(module, diagnostics);
        }

        return new SystemEnergyModuleChainValidationResult(
            IsValid: diagnostics.All(diagnostic => diagnostic.Severity != CalculationDiagnosticSeverity.Error),
            Diagnostics: diagnostics);
    }

    private static void ValidateModule(
        SystemEnergyModuleInput module,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(module.ModuleId))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-MODULE-ID-MISSING",
                "System-energy module id is required."));
        }

        if (module.ModuleKind == SystemEnergyModuleKind.Unknown)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-MODULE-KIND-UNKNOWN",
                $"Module '{module.ModuleId}' kind cannot be Unknown."));
        }

        if (module.EndUse == SystemEnergyEndUse.Unknown)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-MODULE-ENDUSE-UNKNOWN",
                $"Module '{module.ModuleId}' end-use cannot be Unknown."));
        }

        if (module.CalculationMode == SystemEnergyModuleCalculationMode.Unknown)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-MODULE-MODE-UNKNOWN",
                $"Module '{module.ModuleId}' calculation mode cannot be Unknown."));
        }

        if (module.LossFraction.HasValue &&
            (!double.IsFinite(module.LossFraction.Value) || module.LossFraction.Value < 0.0 || module.LossFraction.Value >= 1.0))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-MODULE-LOSS-FRACTION-INVALID",
                $"Module '{module.ModuleId}' loss fraction must be within [0, 1)."));
        }

        if (module.Efficiency.HasValue &&
            (!double.IsFinite(module.Efficiency.Value) || module.Efficiency.Value <= 0.0 || module.Efficiency.Value > 1.0))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-MODULE-EFFICIENCY-INVALID",
                $"Module '{module.ModuleId}' efficiency must be within (0, 1]."));
        }

        if (module.FixedAnnualLossKWh.HasValue &&
            (!double.IsFinite(module.FixedAnnualLossKWh.Value) || module.FixedAnnualLossKWh.Value < 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-MODULE-FIXED-LOSS-INVALID",
                $"Module '{module.ModuleId}' fixed annual loss must be finite and non-negative."));
        }

        if (module.HourlyLossProfileKWh8760 is not null &&
            !SystemEnergyProfileHelper.IsValidProfile(module.HourlyLossProfileKWh8760, SystemEnergyProfileHelper.HoursPerYear))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-MODULE-HOURLY-LOSS-PROFILE-INVALID",
                $"Module '{module.ModuleId}' hourly loss profile must contain 8760 finite non-negative values."));
        }

        if (module.MonthlyLossProfileKWh is not null &&
            !SystemEnergyProfileHelper.IsValidProfile(module.MonthlyLossProfileKWh, SystemEnergyProfileHelper.MonthsPerYear))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-MODULE-MONTHLY-LOSS-PROFILE-INVALID",
                $"Module '{module.ModuleId}' monthly loss profile must contain 12 finite non-negative values."));
        }

        if (module.RecoverableFraction.HasValue &&
            (!double.IsFinite(module.RecoverableFraction.Value) || module.RecoverableFraction.Value < 0.0 || module.RecoverableFraction.Value > 1.0))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-MODULE-RECOVERABLE-FRACTION-INVALID",
                $"Module '{module.ModuleId}' recoverable-loss fraction must be within [0, 1]."));
        }

        if (!HasSufficientModeInput(module))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-MODULE-INPUT-INCOMPLETE",
                $"Module '{module.ModuleId}' mode '{module.CalculationMode}' is missing required input values."));
        }
    }

    private static bool HasSufficientModeInput(SystemEnergyModuleInput module) =>
        module.CalculationMode switch
        {
            SystemEnergyModuleCalculationMode.Disabled => true,
            SystemEnergyModuleCalculationMode.HandoffOnly => true,
            SystemEnergyModuleCalculationMode.LossFraction => module.LossFraction.HasValue,
            SystemEnergyModuleCalculationMode.FixedEfficiency => module.Efficiency.HasValue,
            SystemEnergyModuleCalculationMode.FixedLoss =>
                module.FixedAnnualLossKWh.HasValue ||
                module.HourlyLossProfileKWh8760 is not null ||
                module.MonthlyLossProfileKWh is not null,
            SystemEnergyModuleCalculationMode.DirectProfile => module.HourlyLossProfileKWh8760 is not null,
            SystemEnergyModuleCalculationMode.CoefficientBased => module.LossFraction.HasValue || module.Efficiency.HasValue,
            SystemEnergyModuleCalculationMode.Other => true,
            SystemEnergyModuleCalculationMode.Unknown => false,
            _ => false
        };

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Error,
            code,
            message,
            Source);
}
