using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class SystemEnergyModuleCalculator : ISystemEnergyModuleCalculator
{
    private const string Source = "SystemEnergyModuleCalculator";

    public SystemEnergyModuleResult Calculate(
        SystemEnergyModuleInput module,
        IReadOnlyList<double> hourlyInputEnergyKWh8760)
    {
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(hourlyInputEnergyKWh8760);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        var input = SystemEnergyProfileHelper.Ensure8760(hourlyInputEnergyKWh8760);
        var loss = new double[SystemEnergyProfileHelper.HoursPerYear];
        var output = new double[SystemEnergyProfileHelper.HoursPerYear];

        switch (module.CalculationMode)
        {
            case SystemEnergyModuleCalculationMode.Disabled:
                CopyProfile(input, output);
                diagnostics.Add(CreateInfo("AE-SYS-MODULE-DISABLED", $"Module '{module.ModuleId}' is disabled and was passed through."));
                break;

            case SystemEnergyModuleCalculationMode.LossFraction:
                if (module.LossFraction is { } lossFraction &&
                    double.IsFinite(lossFraction) &&
                    lossFraction >= 0.0 &&
                    lossFraction < 1.0)
                {
                    for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
                    {
                        loss[hour] = input[hour] * lossFraction;
                        output[hour] = input[hour] + loss[hour];
                    }

                    diagnostics.Add(CreateInfo("AE-SYS-MODULE-LOSS-FRACTION-USED", $"Module '{module.ModuleId}' applied loss-fraction mode."));
                }
                else
                {
                    CopyProfile(input, output);
                    diagnostics.Add(CreateWarning("AE-SYS-MODULE-INPUT-INCOMPLETE", $"Module '{module.ModuleId}' missing valid loss fraction; pass-through applied."));
                }

                break;

            case SystemEnergyModuleCalculationMode.FixedEfficiency:
                if (module.Efficiency is { } efficiency &&
                    double.IsFinite(efficiency) &&
                    efficiency > 0.0 &&
                    efficiency <= 1.0)
                {
                    for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
                    {
                        output[hour] = input[hour] / efficiency;
                        loss[hour] = Math.Max(0.0, output[hour] - input[hour]);
                    }

                    diagnostics.Add(CreateInfo("AE-SYS-MODULE-FIXED-EFFICIENCY-USED", $"Module '{module.ModuleId}' applied fixed-efficiency mode."));
                }
                else
                {
                    CopyProfile(input, output);
                    diagnostics.Add(CreateWarning("AE-SYS-MODULE-INPUT-INCOMPLETE", $"Module '{module.ModuleId}' missing valid efficiency; pass-through applied."));
                }

                break;

            case SystemEnergyModuleCalculationMode.FixedLoss:
                ResolveFixedLossProfile(module, loss, diagnostics);
                for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
                {
                    output[hour] = input[hour] + loss[hour];
                }

                diagnostics.Add(CreateInfo("AE-SYS-MODULE-FIXED-LOSS-USED", $"Module '{module.ModuleId}' applied fixed-loss mode."));
                break;

            case SystemEnergyModuleCalculationMode.DirectProfile:
                if (SystemEnergyProfileHelper.IsValidProfile(module.HourlyLossProfileKWh8760, SystemEnergyProfileHelper.HoursPerYear))
                {
                    loss = module.HourlyLossProfileKWh8760!.ToArray();
                    for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
                    {
                        output[hour] = input[hour] + loss[hour];
                    }

                    diagnostics.Add(CreateInfo("AE-SYS-MODULE-DIRECT-PROFILE-USED", $"Module '{module.ModuleId}' applied direct hourly loss profile."));
                }
                else
                {
                    CopyProfile(input, output);
                    diagnostics.Add(CreateWarning("AE-SYS-MODULE-INPUT-INCOMPLETE", $"Module '{module.ModuleId}' direct-profile mode had no valid hourly loss profile; pass-through applied."));
                }

                break;

            case SystemEnergyModuleCalculationMode.HandoffOnly:
                CopyProfile(input, output);
                diagnostics.Add(CreateInfo("AE-SYS-MODULE-HANDOFF-ONLY", $"Module '{module.ModuleId}' is handoff-only and was passed through."));
                break;

            case SystemEnergyModuleCalculationMode.CoefficientBased:
                if (module.LossFraction is { } coefficientLossFraction &&
                    double.IsFinite(coefficientLossFraction) &&
                    coefficientLossFraction >= 0.0 &&
                    coefficientLossFraction < 1.0)
                {
                    for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
                    {
                        loss[hour] = input[hour] * coefficientLossFraction;
                        output[hour] = input[hour] + loss[hour];
                    }
                }
                else
                {
                    CopyProfile(input, output);
                    diagnostics.Add(CreateWarning("AE-SYS-MODULE-INPUT-INCOMPLETE", $"Module '{module.ModuleId}' coefficient-based mode had no valid coefficient; pass-through applied."));
                }

                diagnostics.Add(CreateInfo("AE-SYS-MODULE-LOSS-FRACTION-USED", $"Module '{module.ModuleId}' applied coefficient-based deterministic loss-fraction behavior."));
                break;

            case SystemEnergyModuleCalculationMode.Other:
            case SystemEnergyModuleCalculationMode.Unknown:
            default:
                CopyProfile(input, output);
                diagnostics.Add(CreateWarning("AE-SYS-MODULE-UNKNOWN-NO-FALLBACK", $"Module '{module.ModuleId}' has unsupported calculation mode and was passed through without additional losses."));
                break;
        }

        var recoverableFraction = 0.0;
        if (module.RecoverableFraction is { } recoverable && double.IsFinite(recoverable) && recoverable >= 0.0 && recoverable <= 1.0)
        {
            recoverableFraction = recoverable;
        }
        else
        {
            diagnostics.Add(CreateInfo("AE-SYS-MODULE-RECOVERABLE-FRACTION-DEFAULTED", $"Module '{module.ModuleId}' recoverable-loss fraction defaulted to 0.0."));
        }

        var recoverableLoss = new double[SystemEnergyProfileHelper.HoursPerYear];
        var nonRecoverableLoss = new double[SystemEnergyProfileHelper.HoursPerYear];
        for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
        {
            recoverableLoss[hour] = loss[hour] * recoverableFraction;
            nonRecoverableLoss[hour] = loss[hour] - recoverableLoss[hour];
        }

        var monthlyInput = SystemEnergyProfileHelper.AggregateMonthly(input);
        var monthlyOutput = SystemEnergyProfileHelper.AggregateMonthly(output);
        var monthlyLoss = SystemEnergyProfileHelper.AggregateMonthly(loss);

        diagnostics.Add(CreateInfo("AE-SYS-MODULE-CALCULATED", $"Module '{module.ModuleId}' hourly and aggregate results were calculated."));

        return new SystemEnergyModuleResult(
            ModuleId: module.ModuleId,
            ModuleKind: module.ModuleKind,
            EndUse: module.EndUse,
            CalculationMode: module.CalculationMode,
            HourlyInputEnergyKWh8760: input,
            HourlyOutputEnergyKWh8760: output,
            HourlyLossEnergyKWh8760: loss,
            HourlyRecoverableLossKWh8760: recoverableLoss,
            HourlyNonRecoverableLossKWh8760: nonRecoverableLoss,
            AnnualInputEnergyKWh: input.Sum(),
            AnnualOutputEnergyKWh: output.Sum(),
            AnnualLossEnergyKWh: loss.Sum(),
            AnnualRecoverableLossKWh: recoverableLoss.Sum(),
            AnnualNonRecoverableLossKWh: nonRecoverableLoss.Sum(),
            MonthlyInputEnergyKWh: monthlyInput,
            MonthlyOutputEnergyKWh: monthlyOutput,
            MonthlyLossEnergyKWh: monthlyLoss,
            Diagnostics: diagnostics);
    }

    private static void ResolveFixedLossProfile(
        SystemEnergyModuleInput module,
        double[] loss,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (SystemEnergyProfileHelper.IsValidProfile(module.HourlyLossProfileKWh8760, SystemEnergyProfileHelper.HoursPerYear))
        {
            var hourlyLoss = module.HourlyLossProfileKWh8760!;
            for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
            {
                loss[hour] = hourlyLoss[hour];
            }

            return;
        }

        if (SystemEnergyProfileHelper.IsValidProfile(module.MonthlyLossProfileKWh, SystemEnergyProfileHelper.MonthsPerYear))
        {
            var expanded = SystemEnergyProfileHelper.ExpandMonthlyToHourly(module.MonthlyLossProfileKWh!);
            for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
            {
                loss[hour] = expanded[hour];
            }

            return;
        }

        if (module.FixedAnnualLossKWh is { } annualLoss && double.IsFinite(annualLoss) && annualLoss >= 0.0)
        {
            var hourlyLoss = annualLoss / SystemEnergyProfileHelper.HoursPerYear;
            for (var hour = 0; hour < SystemEnergyProfileHelper.HoursPerYear; hour++)
            {
                loss[hour] = hourlyLoss;
            }

            return;
        }

        diagnostics.Add(CreateWarning(
            "AE-SYS-MODULE-INPUT-INCOMPLETE",
            $"Module '{module.ModuleId}' fixed-loss mode had no valid fixed or profile loss input; zero loss applied."));
    }

    private static void CopyProfile(IReadOnlyList<double> source, double[] destination)
    {
        for (var index = 0; index < destination.Length; index++)
        {
            destination[index] = source[index];
        }
    }

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            Source);

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            Source);
}
