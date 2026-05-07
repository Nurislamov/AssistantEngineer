using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ground;

public sealed class GroundGeometryNormalizer : IGroundGeometryNormalizer
{
    public GroundContactGeometry Normalize(
        GroundContactKind contactKind,
        GroundContactGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(geometry.Diagnostics);

        var area = geometry.AreaSquareMeters;
        var perimeter = geometry.ExposedPerimeterMeters;
        var characteristicDimension = geometry.CharacteristicDimensionMeters;

        if (!(area > 0.0))
        {
            diagnostics.Add(CreateError(
                "AE-GROUND-AREA-NONPOSITIVE",
                "Ground contact area must be greater than zero."));
        }

        if (!characteristicDimension.HasValue)
        {
            if (area > 0.0 && perimeter is > 0.0)
            {
                characteristicDimension = area / (0.5 * perimeter.Value);
                diagnostics.Add(CreateInfo(
                    "AE-GROUND-CHARACTERISTIC-DIMENSION-CALCULATED",
                    "Characteristic dimension was derived from area and exposed perimeter."));
            }
            else
            {
                diagnostics.Add(CreateWarning(
                    "AE-GROUND-CHARACTERISTIC-DIMENSION-NOT-CALCULABLE",
                    "Characteristic dimension could not be derived because area/perimeter metadata is incomplete."));
            }
        }

        if (RequiresBasementWallHeight(contactKind) && !(geometry.BasementWallHeightMeters > 0.0))
        {
            diagnostics.Add(CreateWarning(
                "AE-GROUND-BASEMENT-WALL-HEIGHT-MISSING",
                "Basement or buried-wall contact kind expects basement wall height metadata."));
        }

        if (RequiresCrawlspaceHeight(contactKind) && !(geometry.CrawlspaceHeightMeters > 0.0))
        {
            diagnostics.Add(CreateWarning(
                "AE-GROUND-CRAWLSPACE-HEIGHT-MISSING",
                "Crawlspace or suspended-floor contact kind expects crawlspace height metadata."));
        }

        if (geometry.InsulationPlacement != GroundInsulationPlacement.None)
        {
            var hasThickness = geometry.EdgeInsulationThicknessMeters is > 0.0;
            var hasConductivity = geometry.EdgeInsulationConductivityWPerMeterKelvin is > 0.0;
            if (!hasThickness || !hasConductivity)
            {
                diagnostics.Add(CreateWarning(
                    "AE-GROUND-INSULATION-INCOMPLETE",
                    "Insulation placement is specified, but insulation thickness and/or conductivity metadata is missing."));
            }
        }

        if (!characteristicDimension.HasValue && area > 0.0 && perimeter is null)
        {
            diagnostics.Add(CreateWarning(
                "AE-GROUND-PERIMETER-MISSING",
                "Exposed perimeter is required to derive characteristic dimension for perimeter-based calculations."));
        }

        if (perimeter.HasValue && !(perimeter.Value > 0.0) && NeedsPerimeter(contactKind))
        {
            diagnostics.Add(CreateWarning(
                "AE-GROUND-PERIMETER-MISSING",
                "Exposed perimeter must be greater than zero for the selected ground contact kind."));
        }

        return geometry with
        {
            CharacteristicDimensionMeters = characteristicDimension,
            Diagnostics = diagnostics
        };
    }

    private static bool RequiresBasementWallHeight(GroundContactKind contactKind) =>
        contactKind is GroundContactKind.HeatedBasement or GroundContactKind.UnheatedBasement or GroundContactKind.BuriedWall;

    private static bool RequiresCrawlspaceHeight(GroundContactKind contactKind) =>
        contactKind is GroundContactKind.Crawlspace or GroundContactKind.SuspendedFloor;

    private static bool NeedsPerimeter(GroundContactKind contactKind) =>
        contactKind is GroundContactKind.SlabOnGround
            or GroundContactKind.HeatedBasement
            or GroundContactKind.UnheatedBasement
            or GroundContactKind.BuriedWall
            or GroundContactKind.Crawlspace
            or GroundContactKind.SuspendedFloor;

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        GroundCalculationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            StandardCalculationStage.InputPreparation,
            "GroundGeometryNormalizer");

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        GroundCalculationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            StandardCalculationStage.InputPreparation,
            "GroundGeometryNormalizer");

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        GroundCalculationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Error,
            code,
            message,
            StandardCalculationStage.InputPreparation,
            "GroundGeometryNormalizer");
}
