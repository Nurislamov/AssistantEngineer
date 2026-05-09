using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;
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
    private readonly En15316HeatingSystemCircuitCalculator _en15316CircuitCalculator;

    public SystemEnergyEngine(
        IOptions<SystemEnergyOptions> options,
        En15316SystemEnergyChainCalculator en15316Calculator,
        En15316SystemEnergyApplicationAdapter en15316Adapter,
        En15316HeatingSystemCircuitCalculator en15316CircuitCalculator)
    {
        _options = options.Value;
        _en15316Calculator = en15316Calculator;
        _en15316Adapter = en15316Adapter;
        _en15316CircuitCalculator = en15316CircuitCalculator;
    }

    public SystemEnergyEngine()
        : this(
            Microsoft.Extensions.Options.Options.Create(new SystemEnergyOptions()),
            new En15316SystemEnergyChainCalculator(new En15316SystemEnergyReferenceDataProvider()),
            new En15316SystemEnergyApplicationAdapter(),
            new En15316HeatingSystemCircuitCalculator(
                new En15316HeatingSystemInputValidator(),
                new En15316SystemEnergyReferenceDataProvider()))
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
        if (_options.UseEn15316CircuitLevelCalculator &&
            input.En15316HeatingCircuitInput is not null)
        {
            var circuitResult = _en15316CircuitCalculator.Calculate(input.En15316HeatingCircuitInput);
            if (circuitResult.IsFailure)
                return Result<SystemEnergyResult>.Failure(circuitResult);

            return Result<SystemEnergyResult>.Success(MapCircuitResultWithAdditionalEndUses(input, circuitResult.Value));
        }

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

    private static SystemEnergyResult MapCircuitResultWithAdditionalEndUses(
        SystemEnergyInput source,
        En15316HeatingSystemResult circuitResult)
    {
        var diagnostics = circuitResult.Diagnostics
            .Select(item => new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                item.Code,
                item.Message,
                source.DiagnosticsContext))
            .ToList();

        var assumptions = circuitResult.AssumptionsUsed
            .Concat(["Circuit-level EN15316-style path was used through explicit opt-in."])
            .ToList();

        var finalCooling = ConvertUsefulToFinal(
            source.UsefulCoolingEnergyKWh,
            efficiency: null,
            cop: source.CoolingCop,
            missingCode: "SystemEnergy.CircuitPathCoolingAssumptionMissing",
            missingMessage: "Circuit-level path received cooling useful energy without cooling COP; cooling useful energy was carried through as final energy.",
            context: source.DiagnosticsContext,
            diagnostics: diagnostics);
        var finalFan = Math.Max(0, source.FanEnergyKWh);
        var totalFinal = circuitResult.AnnualFinalEnergyKWh + finalCooling + finalFan;

        var primary = circuitResult.AnnualPrimaryEnergyKWh;
        if (source.PrimaryEnergyFactor is > 0)
        {
            var additionalPrimary = (finalCooling + finalFan) * source.PrimaryEnergyFactor.Value;
            primary += additionalPrimary;
            assumptions.Add("Cooling/fan primary energy contribution was evaluated with SystemEnergyInput primary factor on the circuit-level opt-in path.");
        }
        else if (source.PrimaryEnergyFactor.HasValue)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "SystemEnergy.CircuitPathPrimaryFactorInvalid",
                "Primary energy factor must be greater than zero to evaluate cooling/fan primary energy on circuit-level opt-in path.",
                source.DiagnosticsContext));
        }
        else if (source.UsefulCoolingEnergyKWh > 0 || source.FanEnergyKWh > 0)
        {
            assumptions.Add("Cooling/fan primary energy was not calculated because primary factor was not supplied on the circuit-level opt-in path.");
        }

        return new SystemEnergyResult(
            UsefulHeatingKWh: Round(circuitResult.AnnualUsefulHeatingEnergyKWh),
            UsefulCoolingKWh: Round(Math.Max(0, source.UsefulCoolingEnergyKWh)),
            UsefulDhwKWh: Round(circuitResult.AnnualUsefulDhwEnergyKWh),
            FinalHeatingEnergyKWh: Round(circuitResult.AnnualFinalEnergyKWh),
            FinalCoolingEnergyKWh: Round(finalCooling),
            FinalDhwEnergyKWh: 0.0,
            FinalFanEnergyKWh: Round(finalFan),
            TotalFinalEnergyKWh: Round(totalFinal),
            PrimaryEnergyKWh: Round(primary),
            Diagnostics: diagnostics,
            AssumptionsUsed: assumptions);
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
