using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ground;

public sealed class GroundBoundaryHeatTransferCalculator : IGroundBoundaryHeatTransferCalculator
{
    public GroundHeatTransferResult Calculate(GroundHeatTransferRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Boundary);
        ArgumentNullException.ThrowIfNull(request.ZoneIndoorTemperatureProfileCelsius);
        ArgumentNullException.ThrowIfNull(request.GroundTemperatureProfileCelsius);

        var assumptions = new List<string>();
        var warnings = new List<string>();
        var diagnostics = new List<StandardCalculationDiagnostic>();

        ValidateInputs(request, warnings, diagnostics);

        var boundary = request.Boundary;
        var coefficient = ResolveEquivalentCoefficient(boundary, assumptions, warnings, diagnostics);
        var flowProfile = BuildHeatFlowProfile(request, coefficient, diagnostics);

        var annualHeatLossKWh = flowProfile.Where(value => value < 0.0).Sum(value => -value) * request.TimeStepHours / 1000.0;
        var annualHeatGainKWh = flowProfile.Where(value => value > 0.0).Sum() * request.TimeStepHours / 1000.0;

        assumptions.Add("Heat-flow sign convention: positive means ground-to-zone heat gain, negative means zone-to-ground loss.");

        return new GroundHeatTransferResult(
            GroundTemperatureProfileCelsius: request.GroundTemperatureProfileCelsius.ToArray(),
            EquivalentGroundHeatTransferCoefficientWPerKelvin: coefficient,
            HeatFlowProfileWatts: flowProfile,
            AnnualHeatLossKiloWattHours: annualHeatLossKWh,
            AnnualHeatGainKiloWattHours: annualHeatGainKWh,
            Assumptions: assumptions.ToArray(),
            Warnings: warnings.ToArray(),
            Diagnostics: SortDiagnostics(diagnostics));
    }

    private static double ResolveEquivalentCoefficient(
        GroundBoundaryDefinition boundary,
        ICollection<string> assumptions,
        ICollection<string> warnings,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        var uValue = boundary.ThermalTransmittanceUValueWPerSquareMeterKelvin;
        var area = Math.Max(boundary.AreaSquareMeters, 0.0);

        if (boundary.BoundaryType == GroundBoundaryType.Unsupported)
        {
            warnings.Add("Unsupported boundary type fell back to GenericGroundContact lane.");
            diagnostics.Add(CreateWarning(
                "AE-GROUND-HEAT-TRANSFER-BOUNDARY-TYPE-UNSUPPORTED",
                $"Boundary '{boundary.BoundaryId}' type Unsupported fell back to generic ground-contact lane."));
        }

        if (uValue is not > 0.0)
        {
            var fallbackU = boundary.SoilThermalConductivityWPerMeterKelvin > 0.0 &&
                            boundary.CharacteristicDimensionMeters is > 0.0
                ? boundary.SoilThermalConductivityWPerMeterKelvin / (boundary.CharacteristicDimensionMeters.Value + 1.0)
                : 0.2;
            warnings.Add("U-value was missing/non-positive and deterministic fallback U was used.");
            diagnostics.Add(CreateWarning(
                "AE-GROUND-HEAT-TRANSFER-UVALUE-FALLBACK",
                $"Boundary '{boundary.BoundaryId}' used deterministic fallback U-value."));
            uValue = fallbackU;
        }

        var baseCoefficient = area * uValue.Value;
        var mode = ResolveMode(boundary);

        return mode switch
        {
            GroundBoundaryCalculationMode.SimplifiedSlabOnGround => ComputeSlabCoefficient(boundary, baseCoefficient, assumptions, warnings, diagnostics),
            GroundBoundaryCalculationMode.SimplifiedBasement => ComputeBasementCoefficient(boundary, baseCoefficient, assumptions, warnings, diagnostics),
            _ => ComputeGenericCoefficient(boundary, baseCoefficient, assumptions, diagnostics)
        };
    }

    private static GroundBoundaryCalculationMode ResolveMode(GroundBoundaryDefinition boundary)
    {
        if (boundary.CalculationMode != GroundBoundaryCalculationMode.Auto)
            return boundary.CalculationMode;

        return boundary.BoundaryType switch
        {
            GroundBoundaryType.SlabOnGround => GroundBoundaryCalculationMode.SimplifiedSlabOnGround,
            GroundBoundaryType.HeatedBasementFloor or GroundBoundaryType.HeatedBasementWall or GroundBoundaryType.UnheatedBasementCeiling
                => GroundBoundaryCalculationMode.SimplifiedBasement,
            _ => GroundBoundaryCalculationMode.GenericConductance
        };
    }

    private static double ComputeGenericCoefficient(
        GroundBoundaryDefinition boundary,
        double baseCoefficient,
        ICollection<string> assumptions,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        assumptions.Add("Generic ground-contact lane uses H_ground = Area * U.");
        diagnostics.Add(CreateInfo(
            "AE-GROUND-HEAT-TRANSFER-GENERIC",
            $"Boundary '{boundary.BoundaryId}' used generic H = A * U ground-contact lane."));
        return Math.Max(0.0, baseCoefficient);
    }

    private static double ComputeSlabCoefficient(
        GroundBoundaryDefinition boundary,
        double baseCoefficient,
        ICollection<string> assumptions,
        ICollection<string> warnings,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        if (boundary.ExposedPerimeterMeters is > 0.0 && boundary.CharacteristicDimensionMeters is > 0.0)
        {
            var perimeterFactor = 1.0 + Math.Min(0.20, boundary.ExposedPerimeterMeters.Value / Math.Max(boundary.AreaSquareMeters, 0.1) * 0.3);
            var characteristicFactor = 1.0 + Math.Min(0.15, 1.0 / Math.Max(1.0, boundary.CharacteristicDimensionMeters.Value));
            assumptions.Add("Slab-on-ground lane used perimeter/characteristic-dimension correction on H.");
            diagnostics.Add(CreateInfo(
                "AE-GROUND-HEAT-TRANSFER-SLAB-SIMPLIFIED",
                $"Boundary '{boundary.BoundaryId}' used simplified slab-on-ground equivalent coefficient lane."));
            return Math.Max(0.0, baseCoefficient * perimeterFactor * characteristicFactor);
        }

        warnings.Add("Slab-on-ground metadata was incomplete; generic H = A * U fallback was used.");
        diagnostics.Add(CreateWarning(
            "AE-GROUND-HEAT-TRANSFER-SLAB-FALLBACK-GENERIC",
            $"Boundary '{boundary.BoundaryId}' lacked perimeter or characteristic dimension and fell back to generic lane."));
        return Math.Max(0.0, baseCoefficient);
    }

    private static double ComputeBasementCoefficient(
        GroundBoundaryDefinition boundary,
        double baseCoefficient,
        ICollection<string> assumptions,
        ICollection<string> warnings,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        var hasDepthData = boundary.FloorDepthBelowGradeMeters is > 0.0 || boundary.WallHeightBelowGradeMeters is > 0.0;
        if (!hasDepthData)
        {
            warnings.Add("Basement simplified lane had insufficient depth/height metadata and fell back to generic lane.");
            diagnostics.Add(CreateWarning(
                "AE-GROUND-HEAT-TRANSFER-BASEMENT-FALLBACK-GENERIC",
                $"Boundary '{boundary.BoundaryId}' lacked basement depth/height metadata and fell back to generic lane."));
            return Math.Max(0.0, baseCoefficient);
        }

        var depth = Math.Max(0.0, boundary.FloorDepthBelowGradeMeters ?? 0.0);
        var height = Math.Max(0.0, boundary.WallHeightBelowGradeMeters ?? 0.0);
        var depthHeightFactor = 1.0 + Math.Min(0.20, depth * 0.04 + height * 0.03);
        assumptions.Add("Basement lane used simplified below-grade depth/height factor.");
        diagnostics.Add(CreateInfo(
            "AE-GROUND-HEAT-TRANSFER-BASEMENT-SIMPLIFIED",
            $"Boundary '{boundary.BoundaryId}' used simplified basement ground-contact lane."));

        return Math.Max(0.0, baseCoefficient * depthHeightFactor);
    }

    private static IReadOnlyList<double> BuildHeatFlowProfile(
        GroundHeatTransferRequest request,
        double coefficient,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        var profileLength = Math.Min(
            request.ZoneIndoorTemperatureProfileCelsius.Count,
            request.GroundTemperatureProfileCelsius.Count);

        if (profileLength == 0)
            return [];

        if (request.ZoneIndoorTemperatureProfileCelsius.Count != request.GroundTemperatureProfileCelsius.Count)
        {
            diagnostics.Add(CreateError(
                "AE-GROUND-HEAT-TRANSFER-PROFILE-LENGTH-MISMATCH",
                $"Ground and zone profiles have mismatched lengths ({request.GroundTemperatureProfileCelsius.Count} vs {request.ZoneIndoorTemperatureProfileCelsius.Count})."));
        }

        var result = new double[profileLength];
        for (var index = 0; index < profileLength; index++)
        {
            var tGround = request.GroundTemperatureProfileCelsius[index];
            var tZone = request.ZoneIndoorTemperatureProfileCelsius[index];
            result[index] = coefficient * (tGround - tZone);
        }

        return result;
    }

    private static void ValidateInputs(
        GroundHeatTransferRequest request,
        ICollection<string> warnings,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        var boundary = request.Boundary;
        if (string.IsNullOrWhiteSpace(boundary.BoundaryId))
        {
            diagnostics.Add(CreateError(
                "AE-GROUND-HEAT-TRANSFER-BOUNDARY-ID-MISSING",
                "Ground boundary id is required."));
        }

        if (!string.IsNullOrWhiteSpace(boundary.AdjacentZoneId))
        {
            diagnostics.Add(CreateError(
                "AE-GROUND-HEAT-TRANSFER-ADJACENT-ZONE-FORBIDDEN",
                $"Ground boundary '{boundary.BoundaryId}' cannot define adjacent zone '{boundary.AdjacentZoneId}'."));
        }

        if (!(boundary.AreaSquareMeters > 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-GROUND-HEAT-TRANSFER-AREA-NONPOSITIVE",
                $"Ground boundary '{boundary.BoundaryId}' area must be greater than zero."));
        }

        if (boundary.ThermalTransmittanceUValueWPerSquareMeterKelvin.HasValue &&
            !(boundary.ThermalTransmittanceUValueWPerSquareMeterKelvin.Value > 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-GROUND-HEAT-TRANSFER-UVALUE-NONPOSITIVE",
                $"Ground boundary '{boundary.BoundaryId}' U-value must be greater than zero when provided."));
        }

        if (!(boundary.SoilThermalConductivityWPerMeterKelvin > 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-GROUND-HEAT-TRANSFER-SOIL-CONDUCTIVITY-NONPOSITIVE",
                $"Ground boundary '{boundary.BoundaryId}' soil conductivity must be greater than zero."));
        }

        if (!(boundary.GroundTemperatureAmplitudeCelsius >= 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-GROUND-HEAT-TRANSFER-AMPLITUDE-NEGATIVE",
                $"Ground boundary '{boundary.BoundaryId}' amplitude cannot be negative."));
        }

        if (boundary.GroundTemperaturePhaseShiftDays is { } phaseShift &&
            (phaseShift < 0.0 || phaseShift > 365.0))
        {
            diagnostics.Add(CreateError(
                "AE-GROUND-HEAT-TRANSFER-PHASE-SHIFT-OUT-OF-RANGE",
                $"Ground boundary '{boundary.BoundaryId}' phase shift must be within [0,365] days."));
        }

        if (request.ZoneIndoorTemperatureProfileCelsius.Count == 0 ||
            request.GroundTemperatureProfileCelsius.Count == 0)
        {
            diagnostics.Add(CreateError(
                "AE-GROUND-HEAT-TRANSFER-PROFILE-MISSING",
                "Ground and zone temperature profiles must both be non-empty."));
        }

        if (!(request.TimeStepHours > 0.0) || !double.IsFinite(request.TimeStepHours))
        {
            warnings.Add("TimeStepHours defaulted to 1h because input was non-positive or non-finite.");
            diagnostics.Add(CreateWarning(
                "AE-GROUND-HEAT-TRANSFER-TIMESTEP-DEFAULTED",
                "Ground heat-transfer timestep should be positive; downstream annual sums assume 1h when invalid."));
        }
    }

    private static IReadOnlyList<StandardCalculationDiagnostic> SortDiagnostics(
        IEnumerable<StandardCalculationDiagnostic> diagnostics) =>
        diagnostics
            .OrderByDescending(item => item.Severity)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.Message, StringComparer.Ordinal)
            .ToArray();

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        GroundCalculationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            StandardCalculationStage.HeatTransfer,
            "GroundBoundaryHeatTransferCalculator");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        GroundCalculationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.HeatTransfer,
            "GroundBoundaryHeatTransferCalculator");

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        GroundCalculationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Error,
            code,
            message,
            StandardCalculationStage.HeatTransfer,
            "GroundBoundaryHeatTransferCalculator");
}
