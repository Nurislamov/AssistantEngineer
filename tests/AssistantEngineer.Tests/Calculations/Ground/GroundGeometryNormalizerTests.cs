using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground;

namespace AssistantEngineer.Tests.Calculations.Ground;

public sealed class GroundGeometryNormalizerTests
{
    private readonly GroundGeometryNormalizer _normalizer = new();

    [Fact]
    public void CalculatesCharacteristicDimensionFromAreaAndPerimeter()
    {
        var geometry = CreateGeometry(
            area: 100.0,
            perimeter: 40.0,
            characteristicDimension: null);

        var result = _normalizer.Normalize(GroundContactKind.SlabOnGround, geometry);

        Assert.NotNull(result.CharacteristicDimensionMeters);
        Assert.Equal(5.0, result.CharacteristicDimensionMeters.Value, 6);
    }

    [Fact]
    public void PreservesExplicitCharacteristicDimension()
    {
        var geometry = CreateGeometry(
            area: 100.0,
            perimeter: 40.0,
            characteristicDimension: 6.5);

        var result = _normalizer.Normalize(GroundContactKind.SlabOnGround, geometry);

        Assert.NotNull(result.CharacteristicDimensionMeters);
        Assert.Equal(6.5, result.CharacteristicDimensionMeters.Value, 6);
    }

    [Fact]
    public void ReportsMissingPerimeterWhenCharacteristicDimensionCannotBeCalculated()
    {
        var geometry = CreateGeometry(
            area: 100.0,
            perimeter: null,
            characteristicDimension: null);

        var result = _normalizer.Normalize(GroundContactKind.SlabOnGround, geometry);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-PERIMETER-MISSING");
    }

    [Fact]
    public void ReportsIncompleteInsulationMetadata()
    {
        var geometry = CreateGeometry(
            area: 100.0,
            perimeter: 40.0,
            characteristicDimension: null) with
        {
            InsulationPlacement = GroundInsulationPlacement.HorizontalPerimeter,
            EdgeInsulationThicknessMeters = 0.05,
            EdgeInsulationConductivityWPerMeterKelvin = null
        };

        var result = _normalizer.Normalize(GroundContactKind.SlabOnGround, geometry);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-INSULATION-INCOMPLETE");
    }

    private static GroundContactGeometry CreateGeometry(
        double area,
        double? perimeter,
        double? characteristicDimension) =>
        new(
            AreaSquareMeters: area,
            ExposedPerimeterMeters: perimeter,
            CharacteristicDimensionMeters: characteristicDimension,
            DepthBelowGroundMeters: 0.0,
            BasementWallHeightMeters: null,
            CrawlspaceHeightMeters: null,
            FloorUValueWPerSquareMeterKelvin: 0.25,
            WallUValueWPerSquareMeterKelvin: 0.3,
            EdgeInsulationThicknessMeters: null,
            EdgeInsulationConductivityWPerMeterKelvin: null,
            InsulationPlacement: GroundInsulationPlacement.None,
            Diagnostics: []);
}
