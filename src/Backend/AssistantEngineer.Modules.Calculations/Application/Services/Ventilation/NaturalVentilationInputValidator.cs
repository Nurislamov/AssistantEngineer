using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public sealed class NaturalVentilationInputValidator : INaturalVentilationInputValidator
{
    public NaturalVentilationInputValidationResult Validate(NaturalVentilationCalculationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Environment);
        ArgumentNullException.ThrowIfNull(input.Openings);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(input.Environment.Diagnostics);

        if (string.IsNullOrWhiteSpace(input.CalculationId))
        {
            diagnostics.Add(CreateError(
                "AE-VENT-CALCULATION-ID-MISSING",
                "Calculation id is required."));
        }

        if (input.FlowConfiguration == NaturalVentilationFlowConfiguration.Unknown)
        {
            diagnostics.Add(CreateError(
                "AE-VENT-FLOW-CONFIGURATION-UNKNOWN",
                "Flow configuration must not be Unknown for calculation-ready input."));
        }

        if (input.Openings.Count == 0)
        {
            diagnostics.Add(CreateError(
                "AE-VENT-OPENINGS-MISSING",
                "At least one opening is required."));
        }

        foreach (var opening in input.Openings)
        {
            diagnostics.AddRange(opening.Diagnostics);

            if (!(opening.OpeningAreaSquareMeters > 0.0))
            {
                diagnostics.Add(CreateError(
                    "AE-VENT-OPENING-AREA-NONPOSITIVE",
                    $"Opening '{opening.OpeningId}' area must be greater than zero."));
            }

            var openingFraction = opening.OpeningFraction ?? 0.0;
            var effectiveArea = opening.OpeningAreaSquareMeters * openingFraction;
            if (effectiveArea < 0.0 || !double.IsFinite(effectiveArea))
            {
                diagnostics.Add(CreateError(
                    "AE-VENT-OPENING-FRACTION-INVALID",
                    $"Opening '{opening.OpeningId}' produced invalid effective area."));
            }

            if (!(opening.DischargeCoefficient is > 0.0))
            {
                diagnostics.Add(CreateError(
                    "AE-VENT-DISCHARGE-COEFFICIENT-NONPOSITIVE",
                    $"Opening '{opening.OpeningId}' discharge coefficient must be greater than zero."));
            }
        }

        if (input.FlowConfiguration == NaturalVentilationFlowConfiguration.CrossVentilation &&
            !HasSufficientCrossVentilationMetadata(input.Openings))
        {
            diagnostics.Add(CreateError(
                "AE-VENT-CROSS-VENTILATION-INSUFFICIENT-OPENINGS",
                "Cross ventilation requires at least two openings or one opening with opposite wind pressure coefficient metadata."));
        }

        var environment = input.Environment;
        if (!double.IsFinite(environment.IndoorTemperatureCelsius) ||
            !double.IsFinite(environment.OutdoorTemperatureCelsius))
        {
            diagnostics.Add(CreateError(
                "AE-VENT-ENVIRONMENT-TEMPERATURE-INVALID",
                "Indoor and outdoor temperatures must be finite values."));
        }

        if (!double.IsFinite(environment.WindSpeedMetersPerSecond) ||
            environment.WindSpeedMetersPerSecond < 0.0)
        {
            diagnostics.Add(CreateError(
                "AE-VENT-WIND-SPEED-INVALID",
                "Wind speed must be finite and non-negative."));
        }

        if (environment.OutdoorAirDensityKgPerCubicMeter.HasValue &&
            (!double.IsFinite(environment.OutdoorAirDensityKgPerCubicMeter.Value) ||
             !(environment.OutdoorAirDensityKgPerCubicMeter.Value > 0.0)))
        {
            diagnostics.Add(CreateError(
                "AE-VENT-AIR-DENSITY-INVALID",
                "Outdoor air density must be finite and greater than zero when provided."));
        }

        if (environment.IndoorAirDensityKgPerCubicMeter.HasValue &&
            (!double.IsFinite(environment.IndoorAirDensityKgPerCubicMeter.Value) ||
             !(environment.IndoorAirDensityKgPerCubicMeter.Value > 0.0)))
        {
            diagnostics.Add(CreateError(
                "AE-VENT-AIR-DENSITY-INVALID",
                "Indoor air density must be finite and greater than zero when provided."));
        }

        if (environment.AtmosphericPressurePa.HasValue &&
            (!double.IsFinite(environment.AtmosphericPressurePa.Value) ||
             !(environment.AtmosphericPressurePa.Value > 0.0)))
        {
            diagnostics.Add(CreateError(
                "AE-VENT-ATMOSPHERIC-PRESSURE-INVALID",
                "Atmospheric pressure must be finite and greater than zero when provided."));
        }

        if (RequiresStackPressure(input.FlowConfiguration) &&
            input.Openings.Any(opening => !CanResolveStackHeight(opening, environment)))
        {
            diagnostics.Add(CreateError(
                "AE-VENT-STACK-HEIGHT-MISSING",
                "Stack pressure calculation requires resolvable opening height difference metadata."));
        }

        if (RequiresWindPressure(input.FlowConfiguration) &&
            input.Openings.Any(opening => !HasWindPressureData(opening)))
        {
            diagnostics.Add(CreateError(
                "AE-VENT-WIND-PRESSURE-DATA-MISSING",
                "Wind pressure calculation requires wind pressure coefficients."));
        }

        return new NaturalVentilationInputValidationResult(
            IsValid: diagnostics.All(diagnostic => diagnostic.Severity != CalculationDiagnosticSeverity.Error),
            Diagnostics: diagnostics);
    }

    private static bool HasSufficientCrossVentilationMetadata(
        IReadOnlyList<NaturalVentilationOpeningGeometry> openings) =>
        openings.Count >= 2 ||
        openings.Any(opening => opening.OppositeWindPressureCoefficient.HasValue);

    private static bool RequiresStackPressure(NaturalVentilationFlowConfiguration configuration) =>
        configuration is NaturalVentilationFlowConfiguration.StackOnly
            or NaturalVentilationFlowConfiguration.CombinedWindAndStack
            or NaturalVentilationFlowConfiguration.SingleSided;

    private static bool RequiresWindPressure(NaturalVentilationFlowConfiguration configuration) =>
        configuration is NaturalVentilationFlowConfiguration.WindOnly
            or NaturalVentilationFlowConfiguration.CombinedWindAndStack
            or NaturalVentilationFlowConfiguration.CrossVentilation
            or NaturalVentilationFlowConfiguration.SingleSided;

    private static bool HasWindPressureData(NaturalVentilationOpeningGeometry opening) =>
        opening.WindPressureCoefficient.HasValue ||
        opening.OppositeWindPressureCoefficient.HasValue;

    private static bool CanResolveStackHeight(
        NaturalVentilationOpeningGeometry opening,
        NaturalVentilationEnvironment environment)
    {
        if (opening.TopHeightMeters.HasValue &&
            opening.BottomHeightMeters.HasValue &&
            double.IsFinite(opening.TopHeightMeters.Value) &&
            double.IsFinite(opening.BottomHeightMeters.Value) &&
            Math.Abs(opening.TopHeightMeters.Value - opening.BottomHeightMeters.Value) > 0.0)
        {
            return true;
        }

        if (opening.OpeningHeightMeters is > 0.0)
            return true;

        if (opening.OpeningCenterHeightMeters.HasValue &&
            environment.OpeningReferenceHeightMeters.HasValue &&
            double.IsFinite(opening.OpeningCenterHeightMeters.Value) &&
            double.IsFinite(environment.OpeningReferenceHeightMeters.Value) &&
            Math.Abs(opening.OpeningCenterHeightMeters.Value - environment.OpeningReferenceHeightMeters.Value) > 0.0)
        {
            return true;
        }

        return false;
    }

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        NaturalVentilationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Error,
            code,
            message,
            StandardCalculationStage.InputPreparation,
            "NaturalVentilationInputValidator");
}
