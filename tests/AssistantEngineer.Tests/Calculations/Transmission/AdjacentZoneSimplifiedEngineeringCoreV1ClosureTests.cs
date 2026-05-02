using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Transmission;
using AssistantEngineer.Modules.Calculations.Application.Services.Transmission;

namespace AssistantEngineer.Tests;

public class AdjacentZoneSimplifiedEngineeringCoreV1ClosureTests
{
    private readonly TransmissionHeatTransferEngine _engine = new();

    [Fact]
    public void AdjacentConditionedZoneWithSameTemperatureIsIncludedButProducesZeroHeatFlow()
    {
        var result = Calculate(
            CreateAdjacentElement(
                boundaryType: TransmissionBoundaryType.AdjacentConditionedZone,
                areaM2: 10,
                uValueWPerM2K: 0.5,
                indoorTemperatureC: 22,
                adjacentTemperatureC: 22));

        var element = Assert.Single(result.Elements);

        Assert.False(result.HasErrors);
        Assert.True(element.IsIncludedInLoad);
        Assert.Equal(0, element.DeltaTC, precision: 6);
        Assert.Equal(0, element.HeatFlowW, precision: 6);
        Assert.Equal(0, result.TotalHeatFlowW, precision: 6);
        Assert.Equal(0, result.TotalHeatLossW, precision: 6);
        Assert.Equal(0, result.TotalHeatGainW, precision: 6);
        Assert.Equal(5, result.TotalHeatTransferCoefficientWPerK, precision: 6);
    }

    [Fact]
    public void AdjacentConditionedZoneWithLowerTemperatureProducesHeatLossByUaDeltaT()
    {
        var result = Calculate(
            CreateAdjacentElement(
                boundaryType: TransmissionBoundaryType.AdjacentConditionedZone,
                areaM2: 12,
                uValueWPerM2K: 0.6,
                indoorTemperatureC: 24,
                adjacentTemperatureC: 20));

        var element = Assert.Single(result.Elements);

        Assert.False(result.HasErrors);
        Assert.True(element.IsIncludedInLoad);
        Assert.Equal(4, element.DeltaTC, precision: 6);
        Assert.Equal(28.8, element.HeatFlowW, precision: 6);
        Assert.Equal(28.8, result.TotalHeatFlowW, precision: 6);
        Assert.Equal(28.8, result.TotalHeatLossW, precision: 6);
        Assert.Equal(0, result.TotalHeatGainW, precision: 6);
        Assert.Equal(7.2, result.TotalHeatTransferCoefficientWPerK, precision: 6);
    }

    [Fact]
    public void AdjacentConditionedZoneWithHigherTemperatureProducesHeatGainByUaDeltaT()
    {
        var result = Calculate(
            CreateAdjacentElement(
                boundaryType: TransmissionBoundaryType.AdjacentConditionedZone,
                areaM2: 8,
                uValueWPerM2K: 0.7,
                indoorTemperatureC: 24,
                adjacentTemperatureC: 30));

        var element = Assert.Single(result.Elements);

        Assert.False(result.HasErrors);
        Assert.True(element.IsIncludedInLoad);
        Assert.Equal(-6, element.DeltaTC, precision: 6);
        Assert.Equal(-33.6, element.HeatFlowW, precision: 6);
        Assert.Equal(-33.6, result.TotalHeatFlowW, precision: 6);
        Assert.Equal(0, result.TotalHeatLossW, precision: 6);
        Assert.Equal(33.6, result.TotalHeatGainW, precision: 6);
        Assert.Equal(5.6, result.TotalHeatTransferCoefficientWPerK, precision: 6);
    }

    [Fact]
    public void AdjacentUnheatedSpaceUsesAdjacentTemperatureAndCorrectionFactor()
    {
        var result = Calculate(
            CreateAdjacentElement(
                boundaryType: TransmissionBoundaryType.AdjacentUnheatedSpace,
                areaM2: 5,
                uValueWPerM2K: 1.2,
                indoorTemperatureC: 24,
                adjacentTemperatureC: 16,
                correctionFactor: 0.8));

        var element = Assert.Single(result.Elements);

        Assert.False(result.HasErrors);
        Assert.True(element.IsIncludedInLoad);
        Assert.Equal(8, element.DeltaTC, precision: 6);
        Assert.Equal(38.4, element.HeatFlowW, precision: 6);
        Assert.Equal(38.4, result.TotalHeatFlowW, precision: 6);
        Assert.Equal(38.4, result.TotalHeatLossW, precision: 6);
        Assert.Equal(0, result.TotalHeatGainW, precision: 6);
        Assert.Equal(6, result.TotalHeatTransferCoefficientWPerK, precision: 6);
    }

    [Fact]
    public void AdjacentBoundaryCanBeAggregatedWithMixedHeatLossAndHeatGainDirections()
    {
        var result = _engine.Calculate(new TransmissionHeatTransferRequest(
        [
            CreateAdjacentElement(
                elementId: 1,
                boundaryType: TransmissionBoundaryType.AdjacentConditionedZone,
                areaM2: 10,
                uValueWPerM2K: 0.5,
                indoorTemperatureC: 24,
                adjacentTemperatureC: 18),

            CreateAdjacentElement(
                elementId: 2,
                boundaryType: TransmissionBoundaryType.AdjacentUnheatedSpace,
                areaM2: 8,
                uValueWPerM2K: 0.7,
                indoorTemperatureC: 24,
                adjacentTemperatureC: 30)
        ]));

        Assert.True(result.IsSuccess, result.Error);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(2, result.Value.Elements.Count);
        Assert.Equal(30, result.Value.TotalHeatLossW, precision: 6);
        Assert.Equal(33.6, result.Value.TotalHeatGainW, precision: 6);
        Assert.Equal(-3.6, result.Value.TotalHeatFlowW, precision: 6);
        Assert.Equal(10.6, result.Value.TotalHeatTransferCoefficientWPerK, precision: 6);
    }

    [Fact]
    public void AdjacentBoundaryMissingTemperatureProducesErrorAndExcludesElement()
    {
        var result = Calculate(
            new TransmissionElementInput(
                ElementId: 1,
                ElementType: TransmissionElementType.Wall,
                RoomId: 101,
                AreaM2: 10,
                UValueWPerM2K: 0.5,
                IndoorTemperatureC: 22,
                BoundaryType: TransmissionBoundaryType.AdjacentConditionedZone,
                AdjacentTemperatureC: null,
                DiagnosticsContext: "Adjacent missing temperature"));

        var element = Assert.Single(result.Elements);

        Assert.True(result.HasErrors);
        Assert.False(element.IsIncludedInLoad);
        Assert.Equal(0, element.HeatFlowW, precision: 6);
        Assert.Equal(0, result.TotalHeatFlowW, precision: 6);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Error &&
            diagnostic.Code == "Transmission.MissingBoundaryTemperature");
    }

    [Fact]
    public void AdjacentBoundaryUsesBoundaryTemperatureFallbackWhenAdjacentTemperatureIsMissing()
    {
        var result = Calculate(
            new TransmissionElementInput(
                ElementId: 1,
                ElementType: TransmissionElementType.Wall,
                RoomId: 101,
                AreaM2: 10,
                UValueWPerM2K: 0.5,
                IndoorTemperatureC: 22,
                BoundaryType: TransmissionBoundaryType.AdjacentConditionedZone,
                BoundaryTemperatureC: 19,
                AdjacentTemperatureC: null,
                DiagnosticsContext: "Adjacent fallback temperature"));

        var element = Assert.Single(result.Elements);

        Assert.False(result.HasErrors);
        Assert.True(element.IsIncludedInLoad);
        Assert.Equal(3, element.DeltaTC, precision: 6);
        Assert.Equal(15, element.HeatFlowW, precision: 6);
        Assert.Equal(15, result.TotalHeatFlowW, precision: 6);
    }

    private TransmissionHeatTransferResult Calculate(
        TransmissionElementInput element)
    {
        var result = _engine.Calculate(new TransmissionHeatTransferRequest([element]));
        Assert.True(result.IsSuccess, result.Error);
        return result.Value;
    }

    private static TransmissionElementInput CreateAdjacentElement(
        TransmissionBoundaryType boundaryType,
        double areaM2,
        double uValueWPerM2K,
        double indoorTemperatureC,
        double adjacentTemperatureC,
        double? correctionFactor = null,
        int elementId = 1) =>
        new(
            ElementId: elementId,
            ElementType: TransmissionElementType.Wall,
            RoomId: 101,
            AreaM2: areaM2,
            UValueWPerM2K: uValueWPerM2K,
            IndoorTemperatureC: indoorTemperatureC,
            BoundaryType: boundaryType,
            AdjacentTemperatureC: adjacentTemperatureC,
            CorrectionFactor: correctionFactor,
            DiagnosticsContext: "Adjacent simplified boundary");
}