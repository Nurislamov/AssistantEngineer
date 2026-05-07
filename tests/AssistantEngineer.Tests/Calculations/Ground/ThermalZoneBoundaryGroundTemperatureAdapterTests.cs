using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;

namespace AssistantEngineer.Tests.Calculations.Ground;

public sealed class ThermalZoneBoundaryGroundTemperatureAdapterTests
{
    private readonly ThermalZoneBoundaryGroundTemperatureAdapter _adapter = new(
        new GroundBoundaryTemperatureLookupBuilder());

    [Fact]
    public void BuildsWeightedRepresentativeBuildingGroundTemperature()
    {
        var result = CreateBuildingResult(
            new[]
            {
                CreateSurfaceResult("S1", Enumerable.Repeat(10.0, 8760).ToArray()),
                CreateSurfaceResult("S2", Enumerable.Repeat(20.0, 8760).ToArray())
            },
            new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["S1"] = 10.0,
                ["S2"] = 30.0
            });

        var adapted = _adapter.BuildGroundTemperatureInputs(result);

        Assert.Equal(17.5, adapted.RepresentativeBuildingGroundTemperatureCelsius!.Value, 6);
        Assert.Contains(adapted.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-BUILDING-REPRESENTATIVE-TEMPERATURE-WEIGHTED");
    }

    [Fact]
    public void FallsBackToSimpleAverageWhenHMissing()
    {
        var result = CreateBuildingResult(
            new[]
            {
                CreateSurfaceResult("S1", Enumerable.Repeat(12.0, 8760).ToArray()),
                CreateSurfaceResult("S2", Enumerable.Repeat(18.0, 8760).ToArray())
            },
            new Dictionary<string, double>(StringComparer.Ordinal));

        var adapted = _adapter.BuildGroundTemperatureInputs(result);

        Assert.Equal(15.0, adapted.RepresentativeBuildingGroundTemperatureCelsius!.Value, 6);
        Assert.Contains(adapted.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-BUILDING-REPRESENTATIVE-TEMPERATURE-SIMPLE-AVERAGE");
    }

    [Fact]
    public void MissingRepresentativeTemperaturesProducesDiagnostic()
    {
        var result = CreateBuildingResult(
            new[]
            {
                CreateSurfaceResult("S1", Array.Empty<double>()),
                CreateSurfaceResult("S2", Array.Empty<double>())
            },
            new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["S1"] = 10.0,
                ["S2"] = 30.0
            });

        var adapted = _adapter.BuildGroundTemperatureInputs(result);

        Assert.Null(adapted.RepresentativeBuildingGroundTemperatureCelsius);
        Assert.Contains(adapted.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-BUILDING-REPRESENTATIVE-TEMPERATURE-MISSING");
    }

    private static BuildingGroundBoundaryCalculationResult CreateBuildingResult(
        IReadOnlyList<GroundSurfaceBoundaryCalculationResult> surfaces,
        IReadOnlyDictionary<string, double> heatTransferBySurfaceId) =>
        new(
            BuildingId: "B1",
            GroundSurfaces: surfaces,
            SurfaceHeatTransferCoefficientsWPerKelvin: heatTransferBySurfaceId,
            SurfaceHourlyGroundTemperaturesCelsius: new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal),
            SurfaceMonthlyGroundTemperaturesCelsius: new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal),
            TotalGroundHeatTransferCoefficientWPerKelvin: heatTransferBySurfaceId.Values.Sum(),
            Disclosure: new StandardCalculationDisclosureFactory().CreateGroundIso13370Disclosure(),
            Diagnostics: []);

    private static GroundSurfaceBoundaryCalculationResult CreateSurfaceResult(
        string surfaceId,
        IReadOnlyList<double> hourly)
    {
        var monthly = hourly.Count == 8760
            ? Enumerable.Repeat(hourly.Average(), 12).ToArray()
            : Array.Empty<double>();

        var groundResult = new GroundBoundaryCalculationResult(
            BoundaryId: surfaceId,
            BuildingId: "B1",
            ZoneId: "Z1",
            RoomId: "R1",
            SurfaceId: surfaceId,
            ContactKind: GroundContactKind.SlabOnGround,
            EquivalentUValueWPerSquareMeterKelvin: 0.2,
            HeatTransferCoefficientWPerKelvin: 8.0,
            CharacteristicDimensionMeters: null,
            MonthlyGroundBoundaryTemperaturesCelsius: monthly,
            HourlyGroundBoundaryTemperaturesCelsius: hourly,
            Disclosure: new StandardCalculationDisclosureFactory().CreateGroundIso13370Disclosure(),
            Diagnostics: []);

        return new GroundSurfaceBoundaryCalculationResult(
            SurfaceId: surfaceId,
            BuildingId: "B1",
            ZoneId: "Z1",
            RoomId: "R1",
            ContactKind: GroundContactKind.SlabOnGround,
            EquivalentUValueWPerSquareMeterKelvin: 0.2,
            HeatTransferCoefficientWPerKelvin: 8.0,
            MonthlyGroundBoundaryTemperaturesCelsius: monthly,
            HourlyGroundBoundaryTemperaturesCelsius: hourly,
            GroundResult: groundResult,
            Diagnostics: []);
    }
}
