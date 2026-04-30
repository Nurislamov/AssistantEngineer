using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class SystemEnergyEngine
{
    public Result<SystemEnergyResult> Calculate(SystemEnergyInput input)
    {
        if (input is null)
            return Result<SystemEnergyResult>.Validation("System energy input is required.");

        var diagnostics = Validate(input);
        var assumptions = new List<string>
        {
            "System energy is a simplified engineering model.",
            "Final energy is useful energy divided by efficiency or COP when a system assumption is supplied.",
            "If no system assumption is supplied for an end use, useful energy is returned as final energy with diagnostics."
        };

        var finalHeating = ConvertUsefulToFinal(
            input.UsefulHeatingEnergyKWh,
            input.HeatingEfficiency,
            input.HeatingCop,
            "SystemEnergy.HeatingAssumptionMissing",
            "No heating efficiency or COP was supplied; useful heating energy was carried through as final energy.",
            input.DiagnosticsContext,
            diagnostics);
        var finalCooling = ConvertUsefulToFinal(
            input.UsefulCoolingEnergyKWh,
            efficiency: null,
            cop: input.CoolingCop,
            "SystemEnergy.CoolingAssumptionMissing",
            "No cooling COP was supplied; useful cooling energy was carried through as final energy.",
            input.DiagnosticsContext,
            diagnostics);
        var finalDhw = ConvertUsefulToFinal(
            input.UsefulDhwEnergyKWh,
            input.DhwEfficiency,
            input.DhwCop,
            "SystemEnergy.DhwAssumptionMissing",
            "No DHW efficiency or COP was supplied; useful DHW energy was carried through as final energy.",
            input.DiagnosticsContext,
            diagnostics);

        var finalFan = Math.Max(0, input.FanEnergyKWh);
        var totalFinal = finalHeating + finalCooling + finalDhw + finalFan;
        var primary = input.PrimaryEnergyFactor.HasValue
            ? totalFinal * input.PrimaryEnergyFactor.Value
            : (double?)null;

        return Result<SystemEnergyResult>.Success(new SystemEnergyResult(
            Round(Math.Max(0, input.UsefulHeatingEnergyKWh)),
            Round(Math.Max(0, input.UsefulCoolingEnergyKWh)),
            Round(Math.Max(0, input.UsefulDhwEnergyKWh)),
            Round(finalHeating),
            Round(finalCooling),
            Round(finalDhw),
            Round(finalFan),
            Round(totalFinal),
            primary.HasValue ? Round(primary.Value) : null,
            diagnostics,
            assumptions));
    }

    private static List<CalculationDiagnostic> Validate(SystemEnergyInput input)
    {
        var diagnostics = new List<CalculationDiagnostic>();

        ValidateNonNegative(diagnostics, input.UsefulHeatingEnergyKWh, "SystemEnergy.InvalidUsefulHeating", "Useful heating energy cannot be negative.", input.DiagnosticsContext);
        ValidateNonNegative(diagnostics, input.UsefulCoolingEnergyKWh, "SystemEnergy.InvalidUsefulCooling", "Useful cooling energy cannot be negative.", input.DiagnosticsContext);
        ValidateNonNegative(diagnostics, input.UsefulDhwEnergyKWh, "SystemEnergy.InvalidUsefulDhw", "Useful DHW energy cannot be negative.", input.DiagnosticsContext);
        ValidateNonNegative(diagnostics, input.FanEnergyKWh, "SystemEnergy.InvalidFanEnergy", "Fan energy cannot be negative.", input.DiagnosticsContext);

        ValidatePositiveIfSupplied(diagnostics, input.HeatingEfficiency, "SystemEnergy.InvalidHeatingEfficiency", "Heating efficiency must be greater than zero.", input.DiagnosticsContext);
        ValidatePositiveIfSupplied(diagnostics, input.HeatingCop, "SystemEnergy.InvalidHeatingCop", "Heating COP must be greater than zero.", input.DiagnosticsContext);
        ValidatePositiveIfSupplied(diagnostics, input.CoolingCop, "SystemEnergy.InvalidCoolingCop", "Cooling COP must be greater than zero.", input.DiagnosticsContext);
        ValidatePositiveIfSupplied(diagnostics, input.DhwEfficiency, "SystemEnergy.InvalidDhwEfficiency", "DHW efficiency must be greater than zero.", input.DiagnosticsContext);
        ValidatePositiveIfSupplied(diagnostics, input.DhwCop, "SystemEnergy.InvalidDhwCop", "DHW COP must be greater than zero.", input.DiagnosticsContext);
        ValidatePositiveIfSupplied(diagnostics, input.PrimaryEnergyFactor, "SystemEnergy.InvalidPrimaryEnergyFactor", "Primary energy factor must be greater than zero.", input.DiagnosticsContext);

        return diagnostics;
    }

    private static double ConvertUsefulToFinal(
        double useful,
        double? efficiency,
        double? cop,
        string missingCode,
        string missingMessage,
        string? context,
        List<CalculationDiagnostic> diagnostics)
    {
        useful = Math.Max(0, useful);
        if (useful == 0)
            return 0;

        if (efficiency is > 0)
            return useful / efficiency.Value;

        if (cop is > 0)
            return useful / cop.Value;

        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Warning,
            missingCode,
            missingMessage,
            context));
        return useful;
    }

    private static void ValidateNonNegative(
        ICollection<CalculationDiagnostic> diagnostics,
        double value,
        string code,
        string message,
        string? context)
    {
        if (value < 0)
            diagnostics.Add(new CalculationDiagnostic(CalculationDiagnosticSeverity.Error, code, message, context));
    }

    private static void ValidatePositiveIfSupplied(
        ICollection<CalculationDiagnostic> diagnostics,
        double? value,
        string code,
        string message,
        string? context)
    {
        if (value is <= 0)
            diagnostics.Add(new CalculationDiagnostic(CalculationDiagnosticSeverity.Error, code, message, context));
    }

    private static double Round(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);
}
