using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground;

namespace AssistantEngineer.Tests.Calculations.Ground;

public sealed class GroundBoundaryHeatTransferCalculatorTests
{
    private readonly GroundBoundaryHeatTransferCalculator _calculator = new();

    [Fact]
    public void GenericGroundContact_UsesAreaTimesU()
    {
        var request = CreateRequest(
            boundaryType: GroundBoundaryType.GenericGroundContact,
            mode: GroundBoundaryCalculationMode.GenericConductance,
            area: 50.0,
            uValue: 0.3,
            ground: [12.0],
            zone: [20.0]);

        var result = _calculator.Calculate(request);

        Assert.Equal(15.0, result.EquivalentGroundHeatTransferCoefficientWPerKelvin, 6);
        Assert.Equal(-120.0, result.HeatFlowProfileWatts[0], 6);
    }

    [Fact]
    public void SlabSimplifiedLane_FallbackEmitsDiagnosticWhenShapeDataMissing()
    {
        var request = CreateRequest(
            boundaryType: GroundBoundaryType.SlabOnGround,
            mode: GroundBoundaryCalculationMode.SimplifiedSlabOnGround,
            area: 50.0,
            uValue: 0.3,
            ground: [10.0],
            zone: [20.0],
            exposedPerimeter: null,
            characteristicDimension: null);

        var result = _calculator.Calculate(request);

        Assert.True(result.EquivalentGroundHeatTransferCoefficientWPerKelvin > 0.0);
        Assert.Contains(result.Diagnostics, item => item.Code == "AE-GROUND-HEAT-TRANSFER-SLAB-FALLBACK-GENERIC");
    }

    [Fact]
    public void BasementSimplifiedLane_UsesDeterministicDepthFactor()
    {
        var request = CreateRequest(
            boundaryType: GroundBoundaryType.HeatedBasementFloor,
            mode: GroundBoundaryCalculationMode.SimplifiedBasement,
            area: 50.0,
            uValue: 0.3,
            ground: [10.0],
            zone: [20.0],
            floorDepth: 2.0,
            wallHeight: 2.5);

        var result = _calculator.Calculate(request);

        Assert.True(result.EquivalentGroundHeatTransferCoefficientWPerKelvin > 15.0);
        Assert.Contains(result.Diagnostics, item => item.Code == "AE-GROUND-HEAT-TRANSFER-BASEMENT-SIMPLIFIED");
    }

    [Fact]
    public void InvalidInputs_AreRejected()
    {
        var request = CreateRequest(
            boundaryType: GroundBoundaryType.GenericGroundContact,
            mode: GroundBoundaryCalculationMode.GenericConductance,
            area: -1.0,
            uValue: -0.1,
            ground: [12.0, 11.0],
            zone: [20.0]);

        var result = _calculator.Calculate(request);

        Assert.Contains(result.Diagnostics, item => item.Code == "AE-GROUND-HEAT-TRANSFER-AREA-NONPOSITIVE");
        Assert.Contains(result.Diagnostics, item => item.Code == "AE-GROUND-HEAT-TRANSFER-UVALUE-NONPOSITIVE");
        Assert.Contains(result.Diagnostics, item => item.Code == "AE-GROUND-HEAT-TRANSFER-PROFILE-LENGTH-MISMATCH");
    }

    [Fact]
    public void GroundTemperatureDrivesHeatFlow_NotOutdoorAirProfile()
    {
        var coldGround = _calculator.Calculate(CreateRequest(
            boundaryType: GroundBoundaryType.GenericGroundContact,
            mode: GroundBoundaryCalculationMode.GenericConductance,
            area: 20.0,
            uValue: 0.5,
            ground: [2.0],
            zone: [20.0]));
        var warmGround = _calculator.Calculate(CreateRequest(
            boundaryType: GroundBoundaryType.GenericGroundContact,
            mode: GroundBoundaryCalculationMode.GenericConductance,
            area: 20.0,
            uValue: 0.5,
            ground: [14.0],
            zone: [20.0]));

        Assert.True(warmGround.HeatFlowProfileWatts[0] > coldGround.HeatFlowProfileWatts[0]);
    }

    private static GroundHeatTransferRequest CreateRequest(
        GroundBoundaryType boundaryType,
        GroundBoundaryCalculationMode mode,
        double area,
        double uValue,
        IReadOnlyList<double> ground,
        IReadOnlyList<double> zone,
        double? exposedPerimeter = 30.0,
        double? characteristicDimension = 5.0,
        double? floorDepth = 1.0,
        double? wallHeight = 2.0)
    {
        return new GroundHeatTransferRequest(
            Boundary: new GroundBoundaryDefinition(
                BoundaryId: "G-1",
                ZoneId: "ZONE-A",
                BoundaryType: boundaryType,
                AreaSquareMeters: area,
                ExposedPerimeterMeters: exposedPerimeter,
                ThermalTransmittanceUValueWPerSquareMeterKelvin: uValue,
                FloorDepthBelowGradeMeters: floorDepth,
                WallHeightBelowGradeMeters: wallHeight,
                CharacteristicDimensionMeters: characteristicDimension,
                SoilThermalConductivityWPerMeterKelvin: 2.0,
                GroundAnnualMeanTemperatureCelsius: 10.0,
                GroundTemperatureAmplitudeCelsius: 3.0,
                GroundTemperaturePhaseShiftDays: 45.0,
                ColdestMonthIndex: 1,
                EdgeInsulationThicknessMeters: null,
                EdgeInsulationConductivityWPerMeterKelvin: null,
                CalculationMode: mode,
                ThermalBoundaryId: "B-1"),
            ZoneIndoorTemperatureProfileCelsius: zone,
            GroundTemperatureProfileCelsius: ground,
            TimeStepHours: 1.0);
    }
}
