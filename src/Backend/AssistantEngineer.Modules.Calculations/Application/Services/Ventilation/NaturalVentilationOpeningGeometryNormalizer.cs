using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public sealed class NaturalVentilationOpeningGeometryNormalizer : INaturalVentilationOpeningGeometryNormalizer
{
    private const double DefaultOpeningFraction = 1.0;
    private const double DefaultDischargeCoefficient = 0.60;
    private const double DefaultWindPressureCoefficient = 0.0;
    private const double DefaultOppositeWindPressureCoefficient = 0.0;

    public NaturalVentilationOpeningGeometry Normalize(NaturalVentilationOpeningGeometry opening)
    {
        ArgumentNullException.ThrowIfNull(opening);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(opening.Diagnostics);

        if (string.IsNullOrWhiteSpace(opening.OpeningId))
        {
            diagnostics.Add(CreateWarning(
                "AE-VENT-OPENING-ID-MISSING",
                "Opening id is required for deterministic ventilation calculation."));
        }

        var width = opening.OpeningWidthMeters;
        var height = opening.OpeningHeightMeters;
        var area = opening.OpeningAreaSquareMeters;
        var grossArea = opening.GrossOpeningAreaSquareMeters;

        if (opening.EffectiveOpeningAreaSquareMeters is > 0.0)
        {
            area = opening.EffectiveOpeningAreaSquareMeters.Value;
            diagnostics.Add(CreateInfo(
                "AE-VENT-OPENING-EFFECTIVE-AREA-USED",
                $"Opening '{opening.OpeningId}' effective opening area input was used as active area."));
        }
        else if (!(area > 0.0))
        {
            if (grossArea is > 0.0)
            {
                area = grossArea.Value;
                diagnostics.Add(CreateInfo(
                    "AE-VENT-OPENING-GROSS-AREA-USED",
                    $"Opening '{opening.OpeningId}' gross opening area was used because active area was non-positive."));
            }
            else if (width is > 0.0 && height is > 0.0)
            {
                area = width.Value * height.Value;
            }
            else
            {
                diagnostics.Add(CreateWarning(
                    "AE-VENT-OPENING-WIDTH-HEIGHT-INVALID",
                    $"Opening '{opening.OpeningId}' requires positive width/height when explicit area is non-positive."));
                diagnostics.Add(CreateWarning(
                    "AE-VENT-OPENING-AREA-NONPOSITIVE",
                    $"Opening '{opening.OpeningId}' has non-positive area."));
            }
        }

        var openingFraction = opening.OpeningFraction;
        if (!openingFraction.HasValue)
        {
            openingFraction = DefaultOpeningFraction;
            diagnostics.Add(CreateInfo(
                "AE-VENT-OPENING-FRACTION-DEFAULTED",
                $"Opening '{opening.OpeningId}' fraction was defaulted to {DefaultOpeningFraction:F2}."));
        }
        else if (!double.IsFinite(openingFraction.Value) || openingFraction.Value < 0.0 || openingFraction.Value > 1.0)
        {
            diagnostics.Add(CreateWarning(
                "AE-VENT-OPENING-FRACTION-INVALID",
                $"Opening '{opening.OpeningId}' fraction must be finite and between 0 and 1."));
        }

        if (opening.IsOperable == false)
        {
            openingFraction = 0.0;
            diagnostics.Add(CreateInfo(
                "AE-VENT-OPENING-NONOPERABLE-CLOSED",
                $"Opening '{opening.OpeningId}' is marked as non-operable and was forced closed."));
        }

        if (opening.MaximumOpeningFraction.HasValue)
        {
            if (!double.IsFinite(opening.MaximumOpeningFraction.Value) ||
                opening.MaximumOpeningFraction.Value < 0.0 ||
                opening.MaximumOpeningFraction.Value > 1.0)
            {
                diagnostics.Add(CreateWarning(
                    "AE-VENT-OPENING-MAX-FRACTION-INVALID",
                    $"Opening '{opening.OpeningId}' maximum opening fraction must be finite and within [0,1]."));
            }
            else if (openingFraction.HasValue &&
                     openingFraction.Value > opening.MaximumOpeningFraction.Value)
            {
                diagnostics.Add(CreateInfo(
                    "AE-VENT-OPENING-MAX-FRACTION-CLAMPED",
                    $"Opening '{opening.OpeningId}' opening fraction was clamped to configured maximum."));
                openingFraction = opening.MaximumOpeningFraction.Value;
            }
        }

        var dischargeCoefficient = opening.DischargeCoefficient;
        if (!dischargeCoefficient.HasValue)
        {
            diagnostics.Add(CreateWarning(
                "AE-VENT-DISCHARGE-COEFFICIENT-MISSING",
                $"Opening '{opening.OpeningId}' discharge coefficient was not provided."));
            dischargeCoefficient = DefaultDischargeCoefficient;
            diagnostics.Add(CreateInfo(
                "AE-VENT-DISCHARGE-COEFFICIENT-DEFAULTED",
                $"Opening '{opening.OpeningId}' discharge coefficient was defaulted to {DefaultDischargeCoefficient:F2}."));
        }
        else if (!(dischargeCoefficient.Value > 0.0))
        {
            diagnostics.Add(CreateWarning(
                "AE-VENT-DISCHARGE-COEFFICIENT-NONPOSITIVE",
                $"Opening '{opening.OpeningId}' discharge coefficient must be greater than zero."));
        }

        var windPressureCoefficient = opening.WindPressureCoefficient;
        if (!windPressureCoefficient.HasValue)
        {
            windPressureCoefficient = DefaultWindPressureCoefficient;
            diagnostics.Add(CreateInfo(
                "AE-VENT-WIND-CP-DEFAULTED",
                $"Opening '{opening.OpeningId}' wind pressure coefficient was defaulted to {DefaultWindPressureCoefficient:F2}."));
        }

        var oppositeWindPressureCoefficient = opening.OppositeWindPressureCoefficient;
        if (!oppositeWindPressureCoefficient.HasValue)
        {
            oppositeWindPressureCoefficient = DefaultOppositeWindPressureCoefficient;
            diagnostics.Add(CreateInfo(
                "AE-VENT-OPPOSITE-WIND-CP-DEFAULTED",
                $"Opening '{opening.OpeningId}' opposite wind pressure coefficient was defaulted to {DefaultOppositeWindPressureCoefficient:F2}."));
        }

        if (!(area > 0.0))
        {
            diagnostics.Add(CreateWarning(
                "AE-VENT-OPENING-AREA-NONPOSITIVE",
                $"Opening '{opening.OpeningId}' area remains non-positive after normalization."));
        }

        return opening with
        {
            BoundaryId = string.IsNullOrWhiteSpace(opening.BoundaryId) ? opening.SurfaceId : opening.BoundaryId,
            OpeningAreaSquareMeters = area,
            OpeningFraction = openingFraction,
            DischargeCoefficient = dischargeCoefficient,
            WindPressureCoefficient = windPressureCoefficient,
            OppositeWindPressureCoefficient = oppositeWindPressureCoefficient,
            Diagnostics = diagnostics
        };
    }

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        NaturalVentilationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            StandardCalculationStage.InputPreparation,
            "NaturalVentilationOpeningGeometryNormalizer");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        NaturalVentilationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.InputPreparation,
            "NaturalVentilationOpeningGeometryNormalizer");
}
