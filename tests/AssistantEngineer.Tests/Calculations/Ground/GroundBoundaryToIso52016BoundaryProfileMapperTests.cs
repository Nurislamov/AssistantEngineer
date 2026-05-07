using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground;

namespace AssistantEngineer.Tests.Calculations.Ground;

public sealed class GroundBoundaryToIso52016BoundaryProfileMapperTests
{
    private readonly GroundBoundaryToIso52016BoundaryProfileMapper _mapper = new();

    [Fact]
    public void Maps8760GroundBoundaryTemperatureProfileBySurfaceId()
    {
        var lookup = new GroundBoundaryTemperatureLookup(
            HourlyGroundTemperaturesBySurfaceId: new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal)
            {
                ["S1"] = Enumerable.Repeat(12.5, 8760).ToArray()
            },
            MonthlyGroundTemperaturesBySurfaceId: new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal),
            RepresentativeGroundTemperatureBySurfaceId: new Dictionary<string, double>(StringComparer.Ordinal),
            Diagnostics: []);

        var mapped = _mapper.Map(lookup);

        Assert.Equal(8760, mapped.SurfaceBoundaryConditions.Count);
        Assert.Equal("S1", mapped.SurfaceBoundaryConditions[0].SurfaceId);
        Assert.Equal(0, mapped.SurfaceBoundaryConditions[0].HourOfYear);
        Assert.Equal(12.5, mapped.SurfaceBoundaryConditions[0].BoundaryTemperatureC, 6);
        Assert.Equal(8759, mapped.SurfaceBoundaryConditions[^1].HourOfYear);
    }

    [Fact]
    public void RejectsInvalidProfileLengthWithDiagnostic()
    {
        var lookup = new GroundBoundaryTemperatureLookup(
            HourlyGroundTemperaturesBySurfaceId: new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal)
            {
                ["S1"] = Enumerable.Repeat(12.5, 8759).ToArray()
            },
            MonthlyGroundTemperaturesBySurfaceId: new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal),
            RepresentativeGroundTemperatureBySurfaceId: new Dictionary<string, double>(StringComparer.Ordinal),
            Diagnostics: []);

        var mapped = _mapper.Map(lookup);

        Assert.Empty(mapped.SurfaceBoundaryConditions);
        Assert.Contains(mapped.Diagnostics, diagnostic => diagnostic.Code == "AE-GROUND-ISO52016-HOURLY-PROFILE-INVALID");
    }

    [Fact]
    public void MappingIsAdditiveProfilePreparationOnly()
    {
        var temperatures = Enumerable.Range(0, 8760).Select(hour => (double)hour / 10.0).ToArray();
        var lookup = new GroundBoundaryTemperatureLookup(
            HourlyGroundTemperaturesBySurfaceId: new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal)
            {
                ["S-ground"] = temperatures
            },
            MonthlyGroundTemperaturesBySurfaceId: new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal),
            RepresentativeGroundTemperatureBySurfaceId: new Dictionary<string, double>(StringComparer.Ordinal),
            Diagnostics: []);

        var mapped = _mapper.Map(lookup);

        Assert.Equal(8760, mapped.SurfaceBoundaryConditions.Count);
        Assert.Equal(0.0, mapped.SurfaceBoundaryConditions[0].BoundaryTemperatureC, 6);
        Assert.Equal(875.9, mapped.SurfaceBoundaryConditions[^1].BoundaryTemperatureC, 6);
    }
}
