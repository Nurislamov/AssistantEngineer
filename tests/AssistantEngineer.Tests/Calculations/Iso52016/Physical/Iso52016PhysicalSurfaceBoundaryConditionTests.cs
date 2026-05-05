using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Physical;

public class Iso52016PhysicalSurfaceBoundaryConditionTests
{
    private readonly Iso52016PhysicalRoomModelBuilder _builder = new();

    [Fact]
    public void Build_WithSurfaceHourlyBoundaryCondition_UsesSurfaceSpecificBoundaryIdAndHourlyTemperatureFallback()
    {
        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(),
                Surfaces: new[]
                {
                    CreateSurface("wall-east", Iso52016PhysicalSurfaceBoundaryType.Outdoor, 10),
                    CreateSurface("wall-west", Iso52016PhysicalSurfaceBoundaryType.Outdoor, 10)
                },
                SurfaceBoundaryConditions: new[]
                {
                    new Iso52016PhysicalSurfaceHourlyBoundaryCondition(
                        SurfaceId: "wall-east",
                        HourOfYear: 0,
                        BoundaryTemperatureC: 35),
                    new Iso52016PhysicalSurfaceHourlyBoundaryCondition(
                        SurfaceId: "wall-east",
                        HourOfYear: 1,
                        BoundaryTemperatureC: 31)
                }));

        Assert.True(result.IsSuccess);

        var matrixRequest = result.Value;

        Assert.Contains(matrixRequest.BoundaryConductances, boundary =>
            boundary.NodeId == "surface:wall-east" &&
            boundary.BoundaryId == "outdoor:wall-east");

        Assert.Contains(matrixRequest.BoundaryConductances, boundary =>
            boundary.NodeId == "surface:wall-west" &&
            boundary.BoundaryId == "outdoor");

        Assert.Equal(35.0, matrixRequest.Hours[0].BoundaryTemperaturesC["outdoor:wall-east"], precision: 6);
        Assert.Equal(31.0, matrixRequest.Hours[1].BoundaryTemperaturesC["outdoor:wall-east"], precision: 6);
        Assert.Equal(7.0, matrixRequest.Hours[2].BoundaryTemperaturesC["outdoor:wall-east"], precision: 6);
        Assert.Equal(7.0, matrixRequest.Hours[0].BoundaryTemperaturesC["outdoor"], precision: 6);
    }

    [Fact]
    public void Build_WithGroundAndAdjacentHourlyBoundaryConditions_UsesPerSurfaceDrivingTemperatures()
    {
        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(),
                Surfaces: new[]
                {
                    CreateSurface("slab", Iso52016PhysicalSurfaceBoundaryType.Ground, 12),
                    CreateSurface("party-wall", Iso52016PhysicalSurfaceBoundaryType.AdjacentConditioned, 8)
                },
                SurfaceBoundaryConditions: new[]
                {
                    new Iso52016PhysicalSurfaceHourlyBoundaryCondition(
                        SurfaceId: "slab",
                        HourOfYear: 0,
                        BoundaryTemperatureC: 18),
                    new Iso52016PhysicalSurfaceHourlyBoundaryCondition(
                        SurfaceId: "party-wall",
                        HourOfYear: 0,
                        BoundaryTemperatureC: 23.5)
                }));

        Assert.True(result.IsSuccess);

        var matrixRequest = result.Value;
        var firstHour = matrixRequest.Hours[0];

        Assert.Contains(matrixRequest.BoundaryConductances, boundary =>
            boundary.NodeId == "surface:slab" &&
            boundary.BoundaryId == "ground:slab");

        Assert.Contains(matrixRequest.BoundaryConductances, boundary =>
            boundary.NodeId == "surface:party-wall" &&
            boundary.BoundaryId == "adjacent-conditioned-zone:party-wall");

        Assert.Equal(18.0, firstHour.BoundaryTemperaturesC["ground:slab"], precision: 6);
        Assert.Equal(23.5, firstHour.BoundaryTemperaturesC["adjacent-conditioned-zone:party-wall"], precision: 6);
        Assert.Equal(12.0, matrixRequest.Hours[1].BoundaryTemperaturesC["ground:slab"], precision: 6);
        Assert.Equal(20.0, matrixRequest.Hours[1].BoundaryTemperaturesC["adjacent-conditioned-zone:party-wall"], precision: 6);
    }

    [Fact]
    public void Build_RejectsSurfaceBoundaryConditionWithoutExplicitSurfaces()
    {
        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(),
                SurfaceBoundaryConditions: new[]
                {
                    new Iso52016PhysicalSurfaceHourlyBoundaryCondition(
                        SurfaceId: "wall-east",
                        HourOfYear: 0,
                        BoundaryTemperatureC: 30)
                }));

        Assert.True(result.IsFailure);
        Assert.Equal(
            "ISO 52016 physical room model surface boundary conditions require explicit surfaces.",
            result.Error);
    }

    [Fact]
    public void Build_RejectsBoundaryConditionForUnknownSurface()
    {
        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(),
                Surfaces: new[]
                {
                    CreateSurface("wall-east", Iso52016PhysicalSurfaceBoundaryType.Outdoor, 10)
                },
                SurfaceBoundaryConditions: new[]
                {
                    new Iso52016PhysicalSurfaceHourlyBoundaryCondition(
                        SurfaceId: "missing-wall",
                        HourOfYear: 0,
                        BoundaryTemperatureC: 30)
                }));

        Assert.True(result.IsFailure);
        Assert.Equal(
            "ISO 52016 physical room model boundary condition references unknown surface id 'missing-wall'.",
            result.Error);
    }

    [Fact]
    public void Build_RejectsDuplicateBoundaryConditionForSameSurfaceHour()
    {
        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(),
                Surfaces: new[]
                {
                    CreateSurface("wall-east", Iso52016PhysicalSurfaceBoundaryType.Outdoor, 10)
                },
                SurfaceBoundaryConditions: new[]
                {
                    new Iso52016PhysicalSurfaceHourlyBoundaryCondition(
                        SurfaceId: "wall-east",
                        HourOfYear: 0,
                        BoundaryTemperatureC: 30),
                    new Iso52016PhysicalSurfaceHourlyBoundaryCondition(
                        SurfaceId: "wall-east",
                        HourOfYear: 0,
                        BoundaryTemperatureC: 31)
                }));

        Assert.True(result.IsFailure);
        Assert.Equal(
            "ISO 52016 physical room model duplicate boundary condition for surface 'wall-east' and hour 0.",
            result.Error);
    }

    [Fact]
    public void Build_RejectsBoundaryConditionForHourOutsideProfile()
    {
        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(),
                Surfaces: new[]
                {
                    CreateSurface("wall-east", Iso52016PhysicalSurfaceBoundaryType.Outdoor, 10)
                },
                SurfaceBoundaryConditions: new[]
                {
                    new Iso52016PhysicalSurfaceHourlyBoundaryCondition(
                        SurfaceId: "wall-east",
                        HourOfYear: 8760,
                        BoundaryTemperatureC: 30)
                }));

        Assert.True(result.IsFailure);
        Assert.Equal(
            "ISO 52016 physical room model boundary condition for surface 'wall-east' references hour 8760 that is not in the hourly profile.",
            result.Error);
    }

    private static Iso52016RoomHourlyInputProfile CreateInputProfile()
    {
        var hours = Enumerable
            .Range(0, 24)
            .Select(hour => new Iso52016RoomHourlyInputRecord(
                HourOfYear: hour,
                Month: 1,
                Day: 1,
                Hour: hour,
                OutdoorTemperatureC: 7,
                GroundBoundaryTemperatureC: 12,
                HeatingSetpointC: 20,
                CoolingSetpointC: 26,
                TransmissionHeatTransferCoefficientWPerK: 100,
                VentilationHeatTransferCoefficientWPerK: 25,
                TotalHeatTransferCoefficientWPerK: 125,
                ThermalCapacityJPerK: 5_000_000,
                SolarGainsW: 400,
                InternalGainsW: 200,
                TotalGainsW: 600))
            .ToArray();

        return new Iso52016RoomHourlyInputProfile(
            RoomCode: "room-boundary-profile-1",
            TransmissionHeatTransferCoefficientWPerK: 100,
            VentilationHeatTransferCoefficientWPerK: 25,
            ThermalCapacityJPerK: 5_000_000,
            HeatingSetpointC: 20,
            CoolingSetpointC: 26,
            Hours: hours);
    }

    private static Iso52016PhysicalSurface CreateSurface(
        string surfaceId,
        Iso52016PhysicalSurfaceBoundaryType boundaryType,
        double areaM2) =>
        new(
            SurfaceId: surfaceId,
            BoundaryType: boundaryType,
            AreaM2: areaM2,
            ConstructionLayers: new[]
            {
                new Iso52016PhysicalConstructionLayer(
                    LayerId: $"{surfaceId}-layer",
                    ThicknessM: 0.20,
                    ConductivityWPerMK: 1.0,
                    DensityKgPerM3: 1000,
                    SpecificHeatCapacityJPerKgK: 1000)
            });
}