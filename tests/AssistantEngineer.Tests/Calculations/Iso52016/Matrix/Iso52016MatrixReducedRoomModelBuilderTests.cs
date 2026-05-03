using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016MatrixReducedRoomModelBuilderTests
{
    private readonly Iso52016MatrixReducedRoomModelBuilder _builder = new();

    [Fact]
    public void Build_MapsHourlyInputProfileToSingleAirNodeMatrixRequest()
    {
        var inputProfile = CreateInputProfile();

        var result = _builder.Build(
            new Iso52016MatrixReducedRoomModelRequest(
                HourlyInputProfile: inputProfile,
                HeatBalanceOptions: new Iso52016RoomHeatBalanceOptions(
                    InitialIndoorTemperatureC: 21,
                    TimeStepSeconds: 3600)));

        Assert.True(result.IsSuccess);

        var matrixRequest = result.Value;

        Assert.Equal("room-1", matrixRequest.ZoneCode);
        Assert.Single(matrixRequest.Nodes);
        Assert.Empty(matrixRequest.InternalConductances);
        Assert.Single(matrixRequest.BoundaryConductances);
        Assert.Equal(24, matrixRequest.Hours.Count);

        var node = matrixRequest.Nodes[0];

        Assert.Equal("air", node.NodeId);
        Assert.True(node.IsAirNode);
        Assert.Equal(3_000_000.0, node.HeatCapacityJPerK, precision: 6);
        Assert.Equal(21.0, node.InitialTemperatureC, precision: 6);

        var boundary = matrixRequest.BoundaryConductances[0];

        Assert.Equal("air", boundary.NodeId);
        Assert.Equal("outdoor", boundary.BoundaryId);
        Assert.Equal(150.0, boundary.ConductanceWPerK, precision: 6);

        var firstHour = matrixRequest.Hours[0];

        Assert.Equal(10.0, firstHour.BoundaryTemperaturesC["outdoor"], precision: 6);
        Assert.Equal(700.0, firstHour.NodeHeatGainsW["air"], precision: 6);
        Assert.Equal(20.0, firstHour.HeatingSetpointC);
        Assert.Equal(26.0, firstHour.CoolingSetpointC);
        Assert.NotNull(matrixRequest.Options);
        Assert.Equal("air", matrixRequest.Options!.AirNodeId);
        Assert.Equal(20.0, matrixRequest.Options.DefaultHeatingSetpointC, precision: 6);
        Assert.Equal(26.0, matrixRequest.Options.DefaultCoolingSetpointC, precision: 6);
    }

    [Fact]
    public void Build_RejectsInvalidTimeStep()
    {
        var result = _builder.Build(
            new Iso52016MatrixReducedRoomModelRequest(
                HourlyInputProfile: CreateInputProfile(),
                HeatBalanceOptions: new Iso52016RoomHeatBalanceOptions(
                    InitialIndoorTemperatureC: 20,
                    TimeStepSeconds: 0)));

        Assert.True(result.IsFailure);
        Assert.Equal("ISO 52016 Matrix reduced room model time step must be greater than zero.", result.Error);
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
                OutdoorTemperatureC: 10,
                GroundBoundaryTemperatureC: 12,
                HeatingSetpointC: 20,
                CoolingSetpointC: 26,
                TransmissionHeatTransferCoefficientWPerK: 120,
                VentilationHeatTransferCoefficientWPerK: 30,
                TotalHeatTransferCoefficientWPerK: 150,
                ThermalCapacityJPerK: 3_000_000,
                SolarGainsW: 500,
                InternalGainsW: 200,
                TotalGainsW: 700))
            .ToArray();

        return new Iso52016RoomHourlyInputProfile(
            RoomCode: "room-1",
            TransmissionHeatTransferCoefficientWPerK: 120,
            VentilationHeatTransferCoefficientWPerK: 30,
            ThermalCapacityJPerK: 3_000_000,
            HeatingSetpointC: 20,
            CoolingSetpointC: 26,
            Hours: hours);
    }
}