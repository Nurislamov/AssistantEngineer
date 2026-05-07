using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;

namespace AssistantEngineer.Tests.Calculations.Ground;

public sealed class GroundBoundaryTemperatureLookupBuilderTests
{
    private readonly GroundBoundaryTemperatureLookupBuilder _builder = new();

    [Fact]
    public void BuildsLookupFromCalculatedGroundResult()
    {
        var result = CreateBuildingResult(new[]
        {
            CreateSurfaceResult(
                "S1",
                hourly: Enumerable.Repeat(11.0, 8760).ToArray(),
                monthly: Enumerable.Repeat(10.0, 12).ToArray())
        });

        var lookup = _builder.Build(result);

        Assert.True(lookup.HourlyGroundTemperaturesBySurfaceId.ContainsKey("S1"));
        Assert.True(lookup.MonthlyGroundTemperaturesBySurfaceId.ContainsKey("S1"));
        Assert.True(lookup.RepresentativeGroundTemperatureBySurfaceId.ContainsKey("S1"));
    }

    [Fact]
    public void InvalidHourlyProfileProducesDiagnostic()
    {
        var result = CreateBuildingResult(new[]
        {
            CreateSurfaceResult(
                "S1",
                hourly: Enumerable.Repeat(11.0, 8759).ToArray(),
                monthly: Enumerable.Repeat(10.0, 12).ToArray())
        });

        var lookup = _builder.Build(result);

        Assert.Contains(lookup.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-HOURLY-LOOKUP-PROFILE-INVALID");
    }

    [Fact]
    public void InvalidMonthlyProfileProducesDiagnostic()
    {
        var result = CreateBuildingResult(new[]
        {
            CreateSurfaceResult(
                "S1",
                hourly: Array.Empty<double>(),
                monthly: Enumerable.Repeat(10.0, 11).ToArray())
        });

        var lookup = _builder.Build(result);

        Assert.Contains(lookup.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-MONTHLY-LOOKUP-PROFILE-INVALID");
    }

    private static BuildingGroundBoundaryCalculationResult CreateBuildingResult(
        IReadOnlyList<GroundSurfaceBoundaryCalculationResult> surfaces) =>
        new(
            BuildingId: "B1",
            GroundSurfaces: surfaces,
            SurfaceHeatTransferCoefficientsWPerKelvin: new Dictionary<string, double>(StringComparer.Ordinal),
            SurfaceHourlyGroundTemperaturesCelsius: new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal),
            SurfaceMonthlyGroundTemperaturesCelsius: new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal),
            TotalGroundHeatTransferCoefficientWPerKelvin: 0.0,
            Disclosure: new StandardCalculationDisclosureFactory().CreateGroundIso13370Disclosure(),
            Diagnostics: []);

    private static GroundSurfaceBoundaryCalculationResult CreateSurfaceResult(
        string surfaceId,
        IReadOnlyList<double> hourly,
        IReadOnlyList<double> monthly)
    {
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
