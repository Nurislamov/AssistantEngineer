using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

namespace AssistantEngineer.Tests.Calculations.Ventilation;

public sealed class NaturalVentilationOpeningGeometryNormalizerTests
{
    private readonly NaturalVentilationOpeningGeometryNormalizer _normalizer = new();

    [Fact]
    public void CalculatesAreaFromWidthAndHeightWhenAreaMissing()
    {
        var opening = CreateOpening(
            area: 0.0,
            width: 1.0,
            height: 1.5,
            openingFraction: 1.0,
            dischargeCoefficient: 0.6);

        var normalized = _normalizer.Normalize(opening);

        Assert.Equal(1.5, normalized.OpeningAreaSquareMeters, 6);
    }

    [Fact]
    public void PreservesValidExplicitArea()
    {
        var opening = CreateOpening(
            area: 2.0,
            width: 1.0,
            height: 1.5,
            openingFraction: 1.0,
            dischargeCoefficient: 0.6);

        var normalized = _normalizer.Normalize(opening);

        Assert.Equal(2.0, normalized.OpeningAreaSquareMeters, 6);
    }

    [Fact]
    public void DefaultsOpeningFractionWithDiagnostic()
    {
        var opening = CreateOpening(
            area: 1.0,
            width: 1.0,
            height: 1.0,
            openingFraction: null,
            dischargeCoefficient: 0.6);

        var normalized = _normalizer.Normalize(opening);

        Assert.Equal(1.0, normalized.OpeningFraction!.Value, 6);
        Assert.Contains(normalized.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-OPENING-FRACTION-DEFAULTED");
    }

    [Fact]
    public void DefaultsDischargeCoefficientWithDiagnostic()
    {
        var opening = CreateOpening(
            area: 1.0,
            width: 1.0,
            height: 1.0,
            openingFraction: 1.0,
            dischargeCoefficient: null);

        var normalized = _normalizer.Normalize(opening);

        Assert.Equal(0.60, normalized.DischargeCoefficient!.Value, 6);
        Assert.Contains(normalized.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-DISCHARGE-COEFFICIENT-DEFAULTED");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void ReportsInvalidOpeningFraction(double fraction)
    {
        var opening = CreateOpening(
            area: 1.0,
            width: 1.0,
            height: 1.0,
            openingFraction: fraction,
            dischargeCoefficient: 0.6);

        var normalized = _normalizer.Normalize(opening);

        Assert.Equal(fraction, normalized.OpeningFraction);
        Assert.Contains(normalized.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-OPENING-FRACTION-INVALID");
    }

    private static NaturalVentilationOpeningGeometry CreateOpening(
        double area,
        double? width,
        double? height,
        double? openingFraction,
        double? dischargeCoefficient) =>
        new(
            OpeningId: "O1",
            RoomId: "R1",
            ZoneId: "Z1",
            SurfaceId: "S1",
            OpeningType: NaturalVentilationOpeningType.Window,
            OpeningAreaSquareMeters: area,
            OpeningHeightMeters: height,
            OpeningWidthMeters: width,
            OpeningCenterHeightMeters: 1.5,
            BottomHeightMeters: 1.0,
            TopHeightMeters: 2.0,
            OpeningFraction: openingFraction,
            DischargeCoefficient: dischargeCoefficient,
            WindPressureCoefficient: 0.5,
            OppositeWindPressureCoefficient: 0.0,
            OrientationAzimuthDegrees: 180.0,
            Source: "UnitTest",
            Diagnostics: []);
}
