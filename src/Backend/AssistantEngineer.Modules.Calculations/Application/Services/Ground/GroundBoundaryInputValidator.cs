using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ground;

public sealed class GroundBoundaryInputValidator : IGroundBoundaryInputValidator
{
    public GroundBoundaryInputValidationResult Validate(GroundBoundaryCalculationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Geometry);
        ArgumentNullException.ThrowIfNull(input.Soil);
        ArgumentNullException.ThrowIfNull(input.Climate);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(input.Geometry.Diagnostics);
        diagnostics.AddRange(input.Soil.Diagnostics);
        diagnostics.AddRange(input.Climate.Diagnostics);

        if (string.IsNullOrWhiteSpace(input.BoundaryId))
        {
            diagnostics.Add(CreateError(
                "AE-GROUND-BOUNDARY-ID-MISSING",
                "BoundaryId is required for ground boundary calculation input."));
        }

        if (input.ContactKind == GroundContactKind.Unknown)
        {
            diagnostics.Add(CreateError(
                "AE-GROUND-CONTACT-KIND-UNKNOWN",
                "Ground contact kind must be specified for calculation-ready input."));
        }

        if (!string.IsNullOrWhiteSpace(input.AdjacentZoneId))
        {
            diagnostics.Add(CreateError(
                "AE-GROUND-ADJACENT-ZONE-FORBIDDEN",
                $"Ground boundary '{input.BoundaryId}' cannot specify adjacent zone '{input.AdjacentZoneId}'."));
        }

        if (!(input.Geometry.AreaSquareMeters > 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-GROUND-AREA-NONPOSITIVE",
                "Ground contact area must be greater than zero."));
        }

        if (!(input.Soil.ConductivityWPerMeterKelvin > 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-GROUND-SOIL-CONDUCTIVITY-NONPOSITIVE",
                "Soil conductivity must be greater than zero."));
        }

        if (input.Climate.AnnualMeanGroundTemperatureCelsius.HasValue &&
            !double.IsFinite(input.Climate.AnnualMeanGroundTemperatureCelsius.Value))
        {
            diagnostics.Add(CreateError(
                "AE-GROUND-ANNUAL-MEAN-GROUND-INVALID",
                "Annual mean ground temperature must be finite when provided."));
        }

        if (input.Climate.GroundTemperatureAmplitudeCelsius.HasValue)
        {
            var amplitude = input.Climate.GroundTemperatureAmplitudeCelsius.Value;
            if (!double.IsFinite(amplitude))
            {
                diagnostics.Add(CreateError(
                    "AE-GROUND-AMPLITUDE-INVALID",
                    "Ground temperature amplitude must be finite when provided."));
            }
            else if (amplitude < 0.0)
            {
                diagnostics.Add(CreateError(
                    "AE-GROUND-AMPLITUDE-NEGATIVE",
                    "Ground temperature amplitude cannot be negative."));
            }
        }

        if (input.Climate.GroundTemperaturePhaseShiftDays.HasValue)
        {
            var phaseShift = input.Climate.GroundTemperaturePhaseShiftDays.Value;
            if (!double.IsFinite(phaseShift) || phaseShift < 0.0 || phaseShift > 365.0)
            {
                diagnostics.Add(CreateError(
                    "AE-GROUND-PHASE-SHIFT-OUT-OF-RANGE",
                    "Ground temperature phase shift must be finite and within [0, 365] days."));
            }
        }

        if (RequiresFloorUValue(input.ContactKind))
        {
            if (!input.Geometry.FloorUValueWPerSquareMeterKelvin.HasValue)
            {
                diagnostics.Add(CreateError(
                    "AE-GROUND-FLOOR-UVALUE-MISSING",
                    "Floor U-value is required for the selected ground contact kind."));
            }
            else if (!(input.Geometry.FloorUValueWPerSquareMeterKelvin.Value > 0.0))
            {
                diagnostics.Add(CreateError(
                    "AE-GROUND-FLOOR-UVALUE-NONPOSITIVE",
                    "Floor U-value must be greater than zero."));
            }
        }

        if (RequiresWallUValue(input.ContactKind))
        {
            if (!input.Geometry.WallUValueWPerSquareMeterKelvin.HasValue)
            {
                diagnostics.Add(CreateError(
                    "AE-GROUND-WALL-UVALUE-MISSING",
                    "Wall U-value is required for the selected basement/buried-wall contact kind."));
            }
            else if (!(input.Geometry.WallUValueWPerSquareMeterKelvin.Value > 0.0))
            {
                diagnostics.Add(CreateError(
                    "AE-GROUND-WALL-UVALUE-NONPOSITIVE",
                    "Wall U-value must be greater than zero."));
            }
        }

        if (NeedsExposedPerimeter(input.ContactKind) &&
            !((input.Geometry.ExposedPerimeterMeters ?? 0.0) > 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-GROUND-PERIMETER-MISSING",
                "Exposed perimeter must be greater than zero for the selected contact kind."));
        }

        var monthly = input.Climate.MonthlyOutdoorTemperaturesCelsius;
        if (monthly is not null)
        {
            if (monthly.Count != 12 || monthly.Any(value => !double.IsFinite(value)))
            {
                diagnostics.Add(CreateError(
                    "AE-GROUND-MONTHLY-PROFILE-INVALID",
                    "Monthly outdoor temperature profile must contain 12 finite values."));
            }
        }

        var hourly = input.Climate.HourlyOutdoorTemperaturesCelsius;
        if (hourly is not null)
        {
            if (hourly.Count != 8760 || hourly.Any(value => !double.IsFinite(value)))
            {
                diagnostics.Add(CreateError(
                    "AE-GROUND-HOURLY-PROFILE-INVALID",
                    "Hourly outdoor temperature profile must contain 8760 finite values."));
            }
        }

        var annualMeanProvided = input.Climate.AnnualMeanOutdoorTemperatureCelsius.HasValue &&
            double.IsFinite(input.Climate.AnnualMeanOutdoorTemperatureCelsius.Value);
        var hasMonthly = monthly is { Count: > 0 };
        var hasHourly = hourly is { Count: > 0 };

        if (!hasMonthly && !hasHourly && !annualMeanProvided)
        {
            diagnostics.Add(CreateError(
                "AE-GROUND-CLIMATE-MISSING",
                "Climate input must provide monthly outdoor profile, hourly outdoor profile, or annual mean outdoor temperature."));
        }

        if (input.Climate.AnnualMeanOutdoorTemperatureCelsius.HasValue &&
            !double.IsFinite(input.Climate.AnnualMeanOutdoorTemperatureCelsius.Value))
        {
            diagnostics.Add(CreateError(
                "AE-GROUND-CLIMATE-MISSING",
                "Annual mean outdoor temperature must be finite when provided."));
        }

        if (input.Geometry.InsulationPlacement != GroundInsulationPlacement.None)
        {
            var hasThickness = input.Geometry.EdgeInsulationThicknessMeters is > 0.0;
            var hasConductivity = input.Geometry.EdgeInsulationConductivityWPerMeterKelvin is > 0.0;
            if (!hasThickness || !hasConductivity)
            {
                diagnostics.Add(CreateError(
                    "AE-GROUND-INSULATION-INCOMPLETE",
                    "Insulation placement requires both insulation thickness and insulation conductivity."));
            }
        }

        var orderedDiagnostics = diagnostics
            .OrderByDescending(diagnostic => diagnostic.Severity)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToArray();

        return new GroundBoundaryInputValidationResult(
            IsValid: diagnostics.All(diagnostic => diagnostic.Severity != CalculationDiagnosticSeverity.Error),
            Diagnostics: orderedDiagnostics);
    }

    private static bool RequiresFloorUValue(GroundContactKind kind) =>
        kind is GroundContactKind.SlabOnGround
            or GroundContactKind.SuspendedFloor
            or GroundContactKind.Crawlspace
            or GroundContactKind.HeatedBasement
            or GroundContactKind.UnheatedBasement;

    private static bool RequiresWallUValue(GroundContactKind kind) =>
        kind is GroundContactKind.HeatedBasement
            or GroundContactKind.UnheatedBasement
            or GroundContactKind.BuriedWall;

    private static bool NeedsExposedPerimeter(GroundContactKind kind) =>
        kind is GroundContactKind.SlabOnGround
            or GroundContactKind.HeatedBasement
            or GroundContactKind.UnheatedBasement
            or GroundContactKind.BuriedWall
            or GroundContactKind.Crawlspace
            or GroundContactKind.SuspendedFloor;

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        GroundCalculationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Error,
            code,
            message,
            StandardCalculationStage.InputPreparation,
            "GroundBoundaryInputValidator");
}
