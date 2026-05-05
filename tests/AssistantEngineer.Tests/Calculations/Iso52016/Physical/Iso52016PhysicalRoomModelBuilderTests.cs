using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Physical;

public class Iso52016PhysicalRoomModelBuilderTests
{
    private readonly Iso52016PhysicalRoomModelBuilder _builder = new();

    [Fact]
    public void Build_CreatesDeterministicThreeNodePhysicalMatrixRequest()
    {
        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(),
                HeatBalanceOptions: new Iso52016RoomHeatBalanceOptions(
                    InitialIndoorTemperatureC: 21,
                    TimeStepSeconds: 3600)));

        Assert.True(result.IsSuccess);

        var request = result.Value;

        Assert.Equal("room-physical-1", request.ZoneCode);
        Assert.Equal(3, request.Nodes.Count);
        Assert.Equal(2, request.InternalConductances.Count);
        Assert.Equal(4, request.BoundaryConductances.Count);
        Assert.Equal(24, request.Hours.Count);

        var air = request.Nodes.Single(node => node.NodeId == "air");
        var surface = request.Nodes.Single(node => node.NodeId == "internal-surface");
        var mass = request.Nodes.Single(node => node.NodeId == "thermal-mass");

        Assert.True(air.IsAirNode);
        Assert.False(surface.IsAirNode);
        Assert.False(mass.IsAirNode);

        Assert.Equal(100_000.0, air.HeatCapacityJPerK, precision: 6);
        Assert.Equal(400_000.0, surface.HeatCapacityJPerK, precision: 6);
        Assert.Equal(4_500_000.0, mass.HeatCapacityJPerK, precision: 6);

        Assert.Equal(21.0, air.InitialTemperatureC, precision: 6);
        Assert.Equal(21.0, surface.InitialTemperatureC, precision: 6);
        Assert.Equal(21.0, mass.InitialTemperatureC, precision: 6);

        Assert.Contains(
            request.InternalConductances,
            link => link.FromNodeId == "air" &&
                link.ToNodeId == "internal-surface" &&
                Math.Abs(link.ConductanceWPerK - 250.0) < 1e-9);

        Assert.Contains(
            request.InternalConductances,
            link => link.FromNodeId == "internal-surface" &&
                link.ToNodeId == "thermal-mass" &&
                Math.Abs(link.ConductanceWPerK - 300.0) < 1e-9);

        Assert.Contains(
            request.BoundaryConductances,
            boundary => boundary.NodeId == "internal-surface" &&
                boundary.BoundaryId == "outdoor" &&
                Math.Abs(boundary.ConductanceWPerK - 70.0) < 1e-9);

        Assert.Contains(
            request.BoundaryConductances,
            boundary => boundary.NodeId == "internal-surface" &&
                boundary.BoundaryId == "ground" &&
                Math.Abs(boundary.ConductanceWPerK - 20.0) < 1e-9);

        Assert.Contains(
            request.BoundaryConductances,
            boundary => boundary.NodeId == "internal-surface" &&
                boundary.BoundaryId == "adjacent-zone" &&
                Math.Abs(boundary.ConductanceWPerK - 10.0) < 1e-9);

        Assert.Contains(
            request.BoundaryConductances,
            boundary => boundary.NodeId == "air" &&
                boundary.BoundaryId == "ventilation-air" &&
                Math.Abs(boundary.ConductanceWPerK - 25.0) < 1e-9);

        var firstHour = request.Hours[0];

        Assert.Equal(7.0, firstHour.BoundaryTemperaturesC["outdoor"], precision: 6);
        Assert.Equal(12.0, firstHour.BoundaryTemperaturesC["ground"], precision: 6);
        Assert.Equal(20.0, firstHour.BoundaryTemperaturesC["adjacent-zone"], precision: 6);
        Assert.Equal(7.0, firstHour.BoundaryTemperaturesC["ventilation-air"], precision: 6);

        Assert.Equal(100.0, firstHour.NodeHeatGainsW["air"], precision: 6);
        Assert.Equal(350.0, firstHour.NodeHeatGainsW["internal-surface"], precision: 6);
        Assert.Equal(150.0, firstHour.NodeHeatGainsW["thermal-mass"], precision: 6);

        Assert.NotNull(request.Options);
        Assert.Equal("air", request.Options!.AirNodeId);
        Assert.Equal(20.0, request.Options.DefaultHeatingSetpointC, precision: 6);
        Assert.Equal(26.0, request.Options.DefaultCoolingSetpointC, precision: 6);
    }

    [Fact]
    public void Build_RejectsBoundaryFractionsThatDoNotSumToOne()
    {
        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(),
                ModelOptions: new Iso52016PhysicalNodeModelOptions(
                    OutdoorTransmissionConductanceFraction: 0.50,
                    GroundTransmissionConductanceFraction: 0.20,
                    AdjacentTransmissionConductanceFraction: 0.10)));

        Assert.True(result.IsFailure);
        Assert.Equal(
            "ISO 52016 physical room model boundary conductance fractions must sum to 1.0.",
            result.Error);
    }

    [Fact]
    public void Build_RejectsDuplicateNodeIds()
    {
        var result = _builder.Build(
            new Iso52016PhysicalRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(),
                ModelOptions: new Iso52016PhysicalNodeModelOptions(
                    AirNodeId: "same",
                    InternalSurfaceNodeId: "same")));

        Assert.True(result.IsFailure);
        Assert.Equal(
            "ISO 52016 physical room model node ids must be unique.",
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
            RoomCode: "room-physical-1",
            TransmissionHeatTransferCoefficientWPerK: 100,
            VentilationHeatTransferCoefficientWPerK: 25,
            ThermalCapacityJPerK: 5_000_000,
            HeatingSetpointC: 20,
            CoolingSetpointC: 26,
            Hours: hours);
    }
}