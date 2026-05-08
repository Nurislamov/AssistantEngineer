using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class SystemEnergyUsefulLoadValidator : ISystemEnergyUsefulLoadValidator
{
    private const string Source = "SystemEnergyUsefulLoadValidator";
    private const double AggregateTolerance = 0.05;

    public SystemEnergyUsefulLoadValidationResult Validate(SystemEnergyUsefulLoadSet input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var diagnostics = new List<StandardCalculationDiagnostic>();

        if (string.IsNullOrWhiteSpace(input.CalculationId))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-USEFUL-CALCULATION-ID-MISSING",
                "System-energy useful load-set calculation id is required."));
        }

        if (input.UsefulLoads.Count == 0 && input.AuxiliaryLoads.Count == 0)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-USEFUL-LOADS-MISSING",
                "At least one useful or auxiliary load is required."));
        }

        foreach (var usefulLoad in input.UsefulLoads)
        {
            ValidateUsefulLoad(usefulLoad, diagnostics);
        }

        foreach (var auxiliaryLoad in input.AuxiliaryLoads)
        {
            ValidateAuxiliaryLoad(auxiliaryLoad, diagnostics);
        }

        return new SystemEnergyUsefulLoadValidationResult(
            IsValid: diagnostics.All(diagnostic => diagnostic.Severity != CalculationDiagnosticSeverity.Error),
            Diagnostics: diagnostics);
    }

    private static void ValidateUsefulLoad(
        SystemEnergyUsefulLoadInput load,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(load.LoadId))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-USEFUL-LOAD-ID-MISSING",
                "Useful-load id is required."));
        }

        if (load.EndUse == SystemEnergyEndUse.Unknown)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-USEFUL-ENDUSE-UNKNOWN",
                $"Useful-load '{load.LoadId}' has unknown end use."));
        }

        if (!SystemEnergyProfileHelper.IsValidProfile(load.HourlyUsefulEnergyKWh8760, SystemEnergyProfileHelper.HoursPerYear))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-USEFUL-HOURLY-PROFILE-INVALID",
                $"Useful-load '{load.LoadId}' hourly useful profile must contain 8760 finite non-negative values."));
            return;
        }

        var annualFromHourly = load.HourlyUsefulEnergyKWh8760.Sum();

        if (load.MonthlyUsefulEnergyKWh is not null)
        {
            if (!SystemEnergyProfileHelper.IsValidProfile(load.MonthlyUsefulEnergyKWh, SystemEnergyProfileHelper.MonthsPerYear))
            {
                diagnostics.Add(CreateError(
                    "AE-SYS-USEFUL-MONTHLY-PROFILE-INVALID",
                    $"Useful-load '{load.LoadId}' monthly useful profile must contain 12 finite non-negative values."));
            }
            else
            {
                var monthlySum = load.MonthlyUsefulEnergyKWh.Sum();
                if (Math.Abs(monthlySum - annualFromHourly) > AggregateTolerance)
                {
                    diagnostics.Add(CreateWarning(
                        "AE-SYS-USEFUL-AGGREGATE-MISMATCH",
                        $"Useful-load '{load.LoadId}' monthly sum differs from hourly annual sum."));
                }
            }
        }

        if (load.AnnualUsefulEnergyKWh.HasValue)
        {
            if (!double.IsFinite(load.AnnualUsefulEnergyKWh.Value) || load.AnnualUsefulEnergyKWh.Value < 0.0)
            {
                diagnostics.Add(CreateError(
                    "AE-SYS-USEFUL-ANNUAL-INVALID",
                    $"Useful-load '{load.LoadId}' annual useful energy must be finite and non-negative."));
            }
            else if (Math.Abs(load.AnnualUsefulEnergyKWh.Value - annualFromHourly) > AggregateTolerance)
            {
                diagnostics.Add(CreateWarning(
                    "AE-SYS-USEFUL-AGGREGATE-MISMATCH",
                    $"Useful-load '{load.LoadId}' annual useful energy differs from hourly annual sum."));
            }
        }
    }

    private static void ValidateAuxiliaryLoad(
        SystemEnergyAuxiliaryLoadInput load,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(load.AuxiliaryId))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-AUXILIARY-ID-MISSING",
                "Auxiliary-load id is required."));
        }

        if (load.Carrier == SystemEnergyCarrier.Unknown)
        {
            diagnostics.Add(CreateError(
                "AE-SYS-AUXILIARY-CARRIER-UNKNOWN",
                $"Auxiliary-load '{load.AuxiliaryId}' must define a known carrier."));
        }

        if (!SystemEnergyProfileHelper.IsValidProfile(load.HourlyAuxiliaryEnergyKWh8760, SystemEnergyProfileHelper.HoursPerYear))
        {
            diagnostics.Add(CreateError(
                "AE-SYS-AUXILIARY-HOURLY-PROFILE-INVALID",
                $"Auxiliary-load '{load.AuxiliaryId}' hourly auxiliary profile must contain 8760 finite non-negative values."));
        }
    }

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Error,
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
