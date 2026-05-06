using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy.En15316;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class SystemEnergyEngine
{
    private readonly SystemEnergyOptions _options;
    private readonly En15316SystemEnergyChainCalculator _en15316Calculator;
    private readonly En15316SystemEnergyApplicationAdapter _en15316Adapter;

    public SystemEnergyEngine(
        IOptions<SystemEnergyOptions> options,
        En15316SystemEnergyChainCalculator en15316Calculator,
        En15316SystemEnergyApplicationAdapter en15316Adapter)
    {
        _options = options.Value;
        _en15316Calculator = en15316Calculator;
        _en15316Adapter = en15316Adapter;
    }

    public SystemEnergyEngine()
        : this(
            Microsoft.Extensions.Options.Options.Create(new SystemEnergyOptions()),
            new En15316SystemEnergyChainCalculator(new En15316SystemEnergyReferenceDataProvider()),
            new En15316SystemEnergyApplicationAdapter())
    {
    }

    public Result<SystemEnergyResult> Calculate(SystemEnergyInput input)
    {
        if (input is null)
            return Result<SystemEnergyResult>.Validation("System energy input is required.");

        if (!_options.UseEn15316InspiredChain)
            return CalculateCompatibility(input);

        return CalculateEn15316Inspired(input);
    }

    private Result<SystemEnergyResult> CalculateEn15316Inspired(SystemEnergyInput input)
    {
        var diagnostics = Validate(input);

        if (HasErrorDiagnostics(diagnostics))
        {
            return Result<SystemEnergyResult>.Validation(
                BuildValidationFailureMessage(
                    "System energy validation failed",
                    diagnostics));
        }

        var en15316Input = _en15316Adapter.MapToEn15316Input(input, _options);
        if (en15316Input.EndUses.Count == 0)
            return CalculateCompatibilityFromValidatedInput(input, diagnostics);

        var en15316Result = _en15316Calculator.Calculate(en15316Input);
        if (en15316Result.IsFailure)
            return Result<SystemEnergyResult>.Failure(en15316Result);

        return Result<SystemEnergyResult>.Success(
            _en15316Adapter.MapToSystemEnergyResult(en15316Result.Value, input));
    }

    private static Result<SystemEnergyResult> CalculateCompatibility(SystemEnergyInput input)
    {
        var diagnostics = Validate(input);

        if (HasErrorDiagnostics(diagnostics))
        {
            return Result<SystemEnergyResult>.Validation(
                BuildValidationFailureMessage(
                    "System energy validation failed",
                    diagnostics));
        }

        return CalculateCompatibilityFromValidatedInput(input, diagnostics);
    }

    private static Result<SystemEnergyResult> CalculateCompatibilityFromValidatedInput(
        SystemEnergyInput input,
        List<CalculationDiagnostic> diagnostics)
    {
        var assumptions = new List<string>
        {
            "System energy is a simplified engineering model.",
            "Final energy is useful energy divided by efficiency or COP when a system assumption is supplied.",
            "If no system assumption is supplied for an end use, useful energy is returned as final energy with diagnostics.",
            "If both efficiency and COP are supplied for the same end use, efficiency takes precedence and a warning is emitted."
        };

        AddDualPerformanceAssumptionWarning(
            input.UsefulHeatingEnergyKWh,
            input.HeatingEfficiency,
            input.HeatingCop,
            "SystemEnergy.HeatingDualPerformanceAssumption",
            "Both heating efficiency and heating COP were supplied; heating efficiency is used for final energy conversion.",
            input.DiagnosticsContext,
            diagnostics);

        AddDualPerformanceAssumptionWarning(
            input.UsefulDhwEnergyKWh,
            input.DhwEfficiency,
            input.DhwCop,
            "SystemEnergy.DhwDualPerformanceAssumption",
            "Both DHW efficiency and DHW COP were supplied; DHW efficiency is used for final energy conversion.",
            input.DiagnosticsContext,
            diagnostics);

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

        ValidateNonNegative(
            diagnostics,
            input.UsefulHeatingEnergyKWh,
            "SystemEnergy.InvalidUsefulHeating",
            "Useful heating energy cannot be negative.",
            input.DiagnosticsContext);

        ValidateNonNegative(
            diagnostics,
            input.UsefulCoolingEnergyKWh,
            "SystemEnergy.InvalidUsefulCooling",
            "Useful cooling energy cannot be negative.",
            input.DiagnosticsContext);

        ValidateNonNegative(
            diagnostics,
            input.UsefulDhwEnergyKWh,
            "SystemEnergy.InvalidUsefulDhw",
            "Useful DHW energy cannot be negative.",
            input.DiagnosticsContext);

        ValidateNonNegative(
            diagnostics,
            input.FanEnergyKWh,
            "SystemEnergy.InvalidFanEnergy",
            "Fan energy cannot be negative.",
            input.DiagnosticsContext);

        ValidatePositiveIfSupplied(
            diagnostics,
            input.HeatingEfficiency,
            "SystemEnergy.InvalidHeatingEfficiency",
            "Heating efficiency must be greater than zero.",
            input.DiagnosticsContext);

        ValidatePositiveIfSupplied(
            diagnostics,
            input.HeatingCop,
            "SystemEnergy.InvalidHeatingCop",
            "Heating COP must be greater than zero.",
            input.DiagnosticsContext);

        ValidatePositiveIfSupplied(
            diagnostics,
            input.CoolingCop,
            "SystemEnergy.InvalidCoolingCop",
            "Cooling COP must be greater than zero.",
            input.DiagnosticsContext);

        ValidatePositiveIfSupplied(
            diagnostics,
            input.DhwEfficiency,
            "SystemEnergy.InvalidDhwEfficiency",
            "DHW efficiency must be greater than zero.",
            input.DiagnosticsContext);

        ValidatePositiveIfSupplied(
            diagnostics,
            input.DhwCop,
            "SystemEnergy.InvalidDhwCop",
            "DHW COP must be greater than zero.",
            input.DiagnosticsContext);

        ValidatePositiveIfSupplied(
            diagnostics,
            input.PrimaryEnergyFactor,
            "SystemEnergy.InvalidPrimaryEnergyFactor",
            "Primary energy factor must be greater than zero.",
            input.DiagnosticsContext);

        return diagnostics;
    }

    private static void AddDualPerformanceAssumptionWarning(
        double useful,
        double? efficiency,
        double? cop,
        string code,
        string message,
        string? context,
        List<CalculationDiagnostic> diagnostics)
    {
        if (useful <= 0)
            return;

        if (efficiency is > 0 && cop is > 0)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                code,
                message,
                context));
        }
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
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Error,
                code,
                message,
                context));
        }
    }

    private static void ValidatePositiveIfSupplied(
        ICollection<CalculationDiagnostic> diagnostics,
        double? value,
        string code,
        string message,
        string? context)
    {
        if (value is <= 0)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Error,
                code,
                message,
                context));
        }
    }

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

    private static double Round(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);
}
