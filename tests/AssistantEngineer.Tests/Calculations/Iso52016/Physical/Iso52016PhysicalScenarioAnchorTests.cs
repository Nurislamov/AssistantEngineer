using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Physical;

public class Iso52016PhysicalScenarioAnchorTests
{
    private readonly Iso52016PhysicalRoomModelBuilder _builder = new();
    private readonly Iso52016MatrixHourlySolver _solver = new();

    [Fact]
    public void BuildAndSolve_AggregatedOperationScenario_PreservesHourlyGainsAndProducesFiniteResults()
    {
        var hourlyInputProfile = CreateHourlyInputProfile(
            roomCode: "scenario-aggregated-operation",
            hourCount: 24,
            ventilationHeatTransferCoefficientWPerK: 0.0,
            solarGainsW: 320.0,
            internalGainsW: 180.0);

        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: hourlyInputProfile,
                HeatBalanceOptions: new Iso52016RoomHeatBalanceOptions(
                    InitialIndoorTemperatureC: 21.0,
                    TimeStepSeconds: 3600.0),
                OperationConditions: new[]
                {
                    new Iso52016PhysicalHourlyOperationCondition(
                        HourOfYear: 0,
                        VentilationHeatTransferCoefficientWPerK: 35.0,
                        VentilationBoundaryTemperatureC: 5.0,
                        InternalGainsConvectiveFraction: 0.65,
                        SolarGainsToAirFraction: 0.05),
                    new Iso52016PhysicalHourlyOperationCondition(
                        HourOfYear: 12,
                        VentilationHeatTransferCoefficientWPerK: 10.0,
                        VentilationBoundaryTemperatureC: 12.0,
                        InternalGainsConvectiveFraction: 0.45,
                        SolarGainsToAirFraction: 0.15)
                }));

        Assert.True(result.IsSuccess, result.Error);

        var matrixRequest = result.Value;
        Assert.Equal("scenario-aggregated-operation", matrixRequest.ZoneCode);
        Assert.Equal(3, matrixRequest.Nodes.Count);
        Assert.Equal(24, matrixRequest.Hours.Count);
        Assert.Contains(matrixRequest.BoundaryConductances, boundary =>
            boundary.NodeId == "air" &&
            boundary.BoundaryId == "ventilation-air" &&
            Math.Abs(boundary.ConductanceWPerK - 35.0) < 1e-9);

        Assert.Equal(5.0, matrixRequest.Hours[0].BoundaryTemperaturesC["ventilation-air"], precision: 6);
        Assert.Equal(35.0, Assert.Single(matrixRequest.Hours[0].BoundaryConductanceOverrides!).ConductanceWPerK, precision: 6);
        Assert.Equal(10.0, Assert.Single(matrixRequest.Hours[12].BoundaryConductanceOverrides!).ConductanceWPerK, precision: 6);

        AssertNodeGainConservation(hourlyInputProfile, matrixRequest);

        var solveResult = _solver.Solve(matrixRequest);
        Assert.True(solveResult.IsSuccess, solveResult.Error);
        Assert.Equal(24, solveResult.Value.Hours.Count);
        Assert.All(solveResult.Value.Hours, hour =>
        {
            Assert.True(double.IsFinite(hour.AirTemperatureAfterHvacC));
            Assert.True(double.IsFinite(hour.HeatingLoadW));
            Assert.True(double.IsFinite(hour.CoolingLoadW));
            Assert.True(hour.HeatingLoadW >= 0.0);
            Assert.True(hour.CoolingLoadW >= 0.0);
        });
    }

    [Fact]
    public void BuildAndSolve_SurfaceBoundaryScenario_PreservesSurfaceBoundaryOverridesAndGainDistribution()
    {
        var hourlyInputProfile = CreateHourlyInputProfile(
            roomCode: "scenario-surface-boundary",
            hourCount: 24,
            ventilationHeatTransferCoefficientWPerK: 18.0,
            solarGainsW: 500.0,
            internalGainsW: 220.0);

        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: hourlyInputProfile,
                HeatBalanceOptions: new Iso52016RoomHeatBalanceOptions(
                    InitialIndoorTemperatureC: 20.0,
                    TimeStepSeconds: 3600.0),
                Surfaces: new[]
                {
                    CreateSurface(
                        surfaceId: "wall-east",
                        boundaryType: Iso52016PhysicalSurfaceBoundaryType.Outdoor,
                        areaM2: 12.0,
                        thicknessM: 0.20,
                        conductivityWPerMK: 1.20,
                        densityKgPerM3: 950.0,
                        specificHeatCapacityJPerKgK: 1000.0),
                    CreateSurface(
                        surfaceId: "slab",
                        boundaryType: Iso52016PhysicalSurfaceBoundaryType.Ground,
                        areaM2: 18.0,
                        thicknessM: 0.15,
                        conductivityWPerMK: 0.90,
                        densityKgPerM3: 1800.0,
                        specificHeatCapacityJPerKgK: 900.0)
                },
                SurfaceBoundaryConditions: new[]
                {
                    new Iso52016PhysicalSurfaceHourlyBoundaryCondition(
                        SurfaceId: "wall-east",
                        HourOfYear: 0,
                        BoundaryTemperatureC: 34.0),
                    new Iso52016PhysicalSurfaceHourlyBoundaryCondition(
                        SurfaceId: "slab",
                        HourOfYear: 0,
                        BoundaryTemperatureC: 16.0)
                },
                OperationConditions: new[]
                {
                    new Iso52016PhysicalHourlyOperationCondition(
                        HourOfYear: 0,
                        VentilationHeatTransferCoefficientWPerK: 25.0,
                        VentilationBoundaryTemperatureC: 14.0,
                        InternalGainsConvectiveFraction: 0.60,
                        SolarGainsToAirFraction: 0.10)
                }));

        Assert.True(result.IsSuccess, result.Error);

        var matrixRequest = result.Value;
        Assert.Equal(5, matrixRequest.Nodes.Count);
        Assert.Contains(matrixRequest.Nodes, node => node.NodeId == "surface:wall-east");
        Assert.Contains(matrixRequest.Nodes, node => node.NodeId == "mass:wall-east");
        Assert.Contains(matrixRequest.Nodes, node => node.NodeId == "surface:slab");
        Assert.Contains(matrixRequest.Nodes, node => node.NodeId == "mass:slab");

        Assert.Contains(matrixRequest.BoundaryConductances, boundary =>
            boundary.NodeId == "surface:wall-east" &&
            boundary.BoundaryId == "outdoor:wall-east");
        Assert.Contains(matrixRequest.BoundaryConductances, boundary =>
            boundary.NodeId == "surface:slab" &&
            boundary.BoundaryId == "ground:slab");

        Assert.Equal(34.0, matrixRequest.Hours[0].BoundaryTemperaturesC["outdoor:wall-east"], precision: 6);
        Assert.Equal(16.0, matrixRequest.Hours[0].BoundaryTemperaturesC["ground:slab"], precision: 6);
        Assert.Equal(7.0, matrixRequest.Hours[1].BoundaryTemperaturesC["outdoor:wall-east"], precision: 6);
        Assert.Equal(12.0, matrixRequest.Hours[1].BoundaryTemperaturesC["ground:slab"], precision: 6);
        Assert.Equal(14.0, matrixRequest.Hours[0].BoundaryTemperaturesC["ventilation-air"], precision: 6);

        AssertNodeGainConservation(hourlyInputProfile, matrixRequest);

        var solveResult = _solver.Solve(matrixRequest);
        Assert.True(solveResult.IsSuccess, solveResult.Error);
        Assert.Equal(24, solveResult.Value.HourCount);
        Assert.All(solveResult.Value.Hours, hour => Assert.True(double.IsFinite(hour.AirTemperatureAfterHvacC)));
    }

    [Fact]
    public void Build_SurfaceScenarioWithAdjacentBoundaries_MapsAdjacentDrivingTemperatures()
    {
        var hourlyInputProfile = CreateHourlyInputProfile(
            roomCode: "scenario-adjacent-boundaries",
            hourCount: 3,
            ventilationHeatTransferCoefficientWPerK: 0.0,
            solarGainsW: 100.0,
            internalGainsW: 120.0);

        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: hourlyInputProfile,
                Surfaces: new[]
                {
                    CreateSurface(
                        surfaceId: "party-wall",
                        boundaryType: Iso52016PhysicalSurfaceBoundaryType.AdjacentConditioned,
                        areaM2: 9.0,
                        adjacentBoundaryTemperatureC: 22.0),
                    CreateSurface(
                        surfaceId: "garage-wall",
                        boundaryType: Iso52016PhysicalSurfaceBoundaryType.AdjacentUnconditioned,
                        areaM2: 7.0,
                        adjacentBoundaryTemperatureC: 15.0)
                }));

        Assert.True(result.IsSuccess, result.Error);

        var matrixRequest = result.Value;
        var firstHour = matrixRequest.Hours[0];

        Assert.Contains(matrixRequest.BoundaryConductances, boundary =>
            boundary.NodeId == "surface:party-wall" &&
            boundary.BoundaryId == "adjacent-conditioned-zone");
        Assert.Contains(matrixRequest.BoundaryConductances, boundary =>
            boundary.NodeId == "surface:garage-wall" &&
            boundary.BoundaryId == "adjacent-unconditioned-zone");
        Assert.Equal(22.0, firstHour.BoundaryTemperaturesC["adjacent-conditioned-zone"], precision: 6);
        Assert.Equal(15.0, firstHour.BoundaryTemperaturesC["adjacent-unconditioned-zone"], precision: 6);
        AssertNodeGainConservation(hourlyInputProfile, matrixRequest);
    }

    private static void AssertNodeGainConservation(
        Iso52016RoomHourlyInputProfile hourlyInputProfile,
        Iso52016MatrixHourlySolverRequest matrixRequest)
    {
        var sourceGainsByHour = hourlyInputProfile.Hours.ToDictionary(
            hour => hour.HourOfYear,
            hour => hour.TotalGainsW);

        foreach (var hour in matrixRequest.Hours)
        {
            var distributedGainsW = hour.NodeHeatGainsW.Values.Sum();
            Assert.True(
                sourceGainsByHour.TryGetValue(hour.HourOfYear, out var sourceGainsW),
                $"Source gains were not found for hour {hour.HourOfYear}.");
            Assert.Equal(sourceGainsW, distributedGainsW, precision: 6);
        }
    }

    private static Iso52016RoomHourlyInputProfile CreateHourlyInputProfile(
        string roomCode,
        int hourCount,
        double ventilationHeatTransferCoefficientWPerK,
        double solarGainsW,
        double internalGainsW)
    {
        var hours = Enumerable
            .Range(0, hourCount)
            .Select(hour => new Iso52016RoomHourlyInputRecord(
                HourOfYear: hour,
                Month: 1,
                Day: 1,
                Hour: hour % 24,
                OutdoorTemperatureC: 7.0,
                GroundBoundaryTemperatureC: 12.0,
                HeatingSetpointC: 20.0,
                CoolingSetpointC: 26.0,
                TransmissionHeatTransferCoefficientWPerK: 130.0,
                VentilationHeatTransferCoefficientWPerK: ventilationHeatTransferCoefficientWPerK,
                TotalHeatTransferCoefficientWPerK: 130.0 + ventilationHeatTransferCoefficientWPerK,
                ThermalCapacityJPerK: 4_000_000.0,
                SolarGainsW: solarGainsW,
                InternalGainsW: internalGainsW,
                TotalGainsW: solarGainsW + internalGainsW))
            .ToArray();

        return new Iso52016RoomHourlyInputProfile(
            RoomCode: roomCode,
            TransmissionHeatTransferCoefficientWPerK: 130.0,
            VentilationHeatTransferCoefficientWPerK: ventilationHeatTransferCoefficientWPerK,
            ThermalCapacityJPerK: 4_000_000.0,
            HeatingSetpointC: 20.0,
            CoolingSetpointC: 26.0,
            Hours: hours);
    }

    private static Iso52016PhysicalSurface CreateSurface(
        string surfaceId,
        Iso52016PhysicalSurfaceBoundaryType boundaryType,
        double areaM2,
        double thicknessM = 0.20,
        double conductivityWPerMK = 1.0,
        double densityKgPerM3 = 1000.0,
        double specificHeatCapacityJPerKgK = 1000.0,
        double? adjacentBoundaryTemperatureC = null) =>
        new(
            SurfaceId: surfaceId,
            BoundaryType: boundaryType,
            AreaM2: areaM2,
            ConstructionLayers: new[]
            {
                new Iso52016PhysicalConstructionLayer(
                    LayerId: $"{surfaceId}-layer",
                    ThicknessM: thicknessM,
                    ConductivityWPerMK: conductivityWPerMK,
                    DensityKgPerM3: densityKgPerM3,
                    SpecificHeatCapacityJPerKgK: specificHeatCapacityJPerKgK)
            },
            AdjacentBoundaryTemperatureC: adjacentBoundaryTemperatureC);
}