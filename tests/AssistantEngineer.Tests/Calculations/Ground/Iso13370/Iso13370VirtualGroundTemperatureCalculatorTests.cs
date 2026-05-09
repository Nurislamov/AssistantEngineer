using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground.Iso13370;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground.Iso13370;

namespace AssistantEngineer.Tests.Calculations.Ground.Iso13370;

public sealed class Iso13370VirtualGroundTemperatureCalculatorTests
{
    private readonly Iso13370VirtualGroundTemperatureCalculator _calculator = new();

    [Fact]
    public void CalculatesCharacteristicDimension_FromAreaAndPerimeter()
    {
        var input = CreateInput(areaM2: 48.0, perimeterM: 24.0);

        var result = _calculator.Calculate(input);

        Assert.Equal(4.0, result.CharacteristicFloorDimensionM, 6);
    }

    [Fact]
    public void RejectsInvalidAreaOrPerimeter()
    {
        var invalidArea = CreateInput(areaM2: 0.0, perimeterM: 24.0);
        var invalidPerimeter = CreateInput(areaM2: 48.0, perimeterM: 0.0);

        var areaException = Assert.Throws<InvalidOperationException>(() => _calculator.Calculate(invalidArea));
        var perimeterException = Assert.Throws<InvalidOperationException>(() => _calculator.Calculate(invalidPerimeter));

        Assert.Contains("FloorAreaM2 > 0", areaException.Message, StringComparison.Ordinal);
        Assert.Contains("ExposedPerimeterM > 0", perimeterException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ProducesStableMonthlyProfiles_WithLength12()
    {
        var input = CreateInput();

        var result = _calculator.Calculate(input);

        Assert.Equal(12, result.MonthlyVirtualGroundTemperatureC.Count);
        Assert.Equal(12, result.MonthlyEquivalentGroundHeatTransferCoefficientWPerK.Count);
        Assert.Equal(12, result.MonthlyBoundaryConditions.Count);
        Assert.Equal(8760, result.HourlyVirtualGroundTemperatureC.Count);
    }

    [Fact]
    public void HigherInsulation_ReducesHeatTransfer()
    {
        var lowResistance = CreateInput(slabResistanceM2KPerW: 0.5);
        var highResistance = CreateInput(slabResistanceM2KPerW: 3.5);

        var lowResult = _calculator.Calculate(lowResistance);
        var highResult = _calculator.Calculate(highResistance);

        Assert.True(highResult.AnnualEquivalentGroundHeatTransferCoefficientWPerK <
                    lowResult.AnnualEquivalentGroundHeatTransferCoefficientWPerK);
    }

    [Fact]
    public void HigherGroundConductivity_IncreasesCoupling()
    {
        var lowConductivity = CreateInput(groundConductivityWPerMK: 1.0);
        var highConductivity = CreateInput(groundConductivityWPerMK: 3.5);

        var lowResult = _calculator.Calculate(lowConductivity);
        var highResult = _calculator.Calculate(highConductivity);

        Assert.True(highResult.AnnualEquivalentGroundHeatTransferCoefficientWPerK >
                    lowResult.AnnualEquivalentGroundHeatTransferCoefficientWPerK);
    }

    [Fact]
    public void SeasonalProfile_IsDeterministicAcrossRuns()
    {
        var input = CreateInput(
            monthlyOutdoorProfileC: [2.0, 4.0, 7.0, 12.0, 16.0, 20.0, 23.0, 22.0, 18.0, 12.0, 7.0, 3.0],
            seasonalAmplitudeC: 10.0,
            seasonalPhaseShiftMonths: 1.5);

        var first = _calculator.Calculate(input);
        var second = _calculator.Calculate(input);

        Assert.Equal(first.MonthlyVirtualGroundTemperatureC, second.MonthlyVirtualGroundTemperatureC);
        Assert.Equal(first.MonthlyEquivalentGroundHeatTransferCoefficientWPerK, second.MonthlyEquivalentGroundHeatTransferCoefficientWPerK);
        Assert.Equal(first.HourlyVirtualGroundTemperatureC, second.HourlyVirtualGroundTemperatureC);
    }

    [Fact]
    public void VirtualGroundOutput_MapsToIso52016BoundaryProfile()
    {
        var input = CreateInput();
        var virtualResult = _calculator.Calculate(input);

        var lookup = new GroundBoundaryTemperatureLookup(
            HourlyGroundTemperaturesBySurfaceId: new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal)
            {
                ["slab-ground-surface"] = virtualResult.HourlyVirtualGroundTemperatureC
            },
            MonthlyGroundTemperaturesBySurfaceId: new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal)
            {
                ["slab-ground-surface"] = virtualResult.MonthlyVirtualGroundTemperatureC
            },
            RepresentativeGroundTemperatureBySurfaceId: new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["slab-ground-surface"] = virtualResult.AnnualMeanVirtualGroundTemperatureC
            },
            Diagnostics: []);

        var mapper = new GroundBoundaryToIso52016BoundaryProfileMapper();
        var mapped = mapper.Map(lookup);

        Assert.Equal(8760, mapped.SurfaceBoundaryConditions.Count);
        Assert.Equal("slab-ground-surface", mapped.SurfaceBoundaryConditions[0].SurfaceId);
        Assert.Equal(0, mapped.SurfaceBoundaryConditions[0].HourOfYear);
    }

    private static Iso13370VirtualGroundInput CreateInput(
        double areaM2 = 48.0,
        double perimeterM = 24.0,
        double slabResistanceM2KPerW = 2.0,
        double groundConductivityWPerMK = 2.0,
        double equivalentGroundThicknessM = 0.0,
        IReadOnlyList<double>? monthlyOutdoorProfileC = null,
        double seasonalAmplitudeC = 8.0,
        double seasonalPhaseShiftMonths = 1.0)
    {
        return new Iso13370VirtualGroundInput(
            Geometry: new SlabOnGroundGeometry(
                FloorAreaM2: areaM2,
                ExposedPerimeterM: perimeterM,
                SlabThermalResistanceM2KPerW: slabResistanceM2KPerW),
            GroundThermalProperties: new GroundThermalProperties(
                GroundConductivityWPerMK: groundConductivityWPerMK,
                EquivalentGroundThicknessM: equivalentGroundThicknessM),
            AnnualAverageOutdoorTemperatureC: 10.0,
            MonthlyOutdoorTemperatureProfileC: monthlyOutdoorProfileC,
            SeasonalAmplitudeC: seasonalAmplitudeC,
            SeasonalPhaseShiftMonths: seasonalPhaseShiftMonths,
            IndoorSetpointTemperatureC: 20.0,
            ThermalBridge: new GroundThermalBridgeInput(
                Enabled: false,
                LinearThermalTransmittanceWPerMK: 0.0,
                BridgeLengthM: perimeterM),
            Options: new Iso13370GroundCalculationOptions(
                EnableSeasonalComponent: true,
                EnablePerimeterThermalBridge: true,
                SeasonalAttenuationFactor: 0.55,
                MonthlyHeatTransferVariationFactor: 0.05,
                MinimumEquivalentGroundThicknessM: 0.3,
                MaximumEquivalentGroundThicknessM: 8.0));
    }
}

