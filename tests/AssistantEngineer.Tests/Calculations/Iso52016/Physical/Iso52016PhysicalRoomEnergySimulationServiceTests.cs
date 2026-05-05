using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Physical;

public class Iso52016PhysicalRoomEnergySimulationServiceTests
{
    private readonly Iso52016PhysicalRoomEnergySimulationService _service = new(
        new Iso52016PhysicalRoomModelBuilder(),
        new Iso52016MatrixHourlySolver());

    [Fact]
    public void Simulate_BuildsPhysicalMatrixRequestAndRunsExistingMatrixSolver()
    {
        var request = new Iso52016PhysicalRoomModelRequest(
            HourlyInputProfile: CreateInputProfile(
                roomCode: "physical-service-room",
                ventilationHeatTransferCoefficientWPerK: 0),
            HeatBalanceOptions: new Iso52016RoomHeatBalanceOptions(
                InitialIndoorTemperatureC: 20,
                TimeStepSeconds: 3600));

        var result = _service.Simulate(request);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal("physical-service-room", result.Value.RoomCode);
        Assert.Same(request, result.Value.PhysicalModelRequest);
        Assert.Equal("physical-service-room", result.Value.MatrixSolverRequest.ZoneCode);
        Assert.Equal(3, result.Value.MatrixSolverRequest.Nodes.Count);
        Assert.Equal(3, result.Value.HourCount);
        Assert.Equal(result.Value.MatrixSolverProfile.HourCount, result.Value.HourCount);
        Assert.Equal(result.Value.MatrixSolverProfile.AnnualHeatingEnergyKWh, result.Value.AnnualHeatingEnergyKWh, precision: 9);
        Assert.Equal(result.Value.MatrixSolverProfile.AnnualCoolingEnergyKWh, result.Value.AnnualCoolingEnergyKWh, precision: 9);
    }

    [Fact]
    public void Simulate_PreservesOperationProfileVentilationOverridesInGeneratedMatrixRequest()
    {
        var request = new Iso52016PhysicalRoomModelRequest(
            HourlyInputProfile: CreateInputProfile(
                roomCode: "physical-operation-service-room",
                ventilationHeatTransferCoefficientWPerK: 0),
            OperationConditions: new[]
            {
                new Iso52016PhysicalHourlyOperationCondition(
                    HourOfYear: 0,
                    VentilationHeatTransferCoefficientWPerK: 15,
                    VentilationBoundaryTemperatureC: 19,
                    InternalGainsConvectiveFraction: 0.75,
                    SolarGainsToAirFraction: 0.10),
                new Iso52016PhysicalHourlyOperationCondition(
                    HourOfYear: 1,
                    VentilationHeatTransferCoefficientWPerK: 0)
            });

        var result = _service.Simulate(request);

        Assert.True(result.IsSuccess, result.Error);

        var matrixRequest = result.Value.MatrixSolverRequest;
        var ventilationLink = Assert.Single(
            matrixRequest.BoundaryConductances,
            link => link.BoundaryId == "ventilation-air");

        Assert.Equal("air", ventilationLink.NodeId);
        Assert.Equal(15.0, ventilationLink.ConductanceWPerK, precision: 6);
        Assert.Equal(19.0, matrixRequest.Hours[0].BoundaryTemperaturesC["ventilation-air"], precision: 6);
        Assert.Equal(15.0, Assert.Single(matrixRequest.Hours[0].BoundaryConductanceOverrides!).ConductanceWPerK, precision: 6);
        Assert.Equal(0.0, Assert.Single(matrixRequest.Hours[1].BoundaryConductanceOverrides!).ConductanceWPerK, precision: 6);
        Assert.Equal(3, result.Value.MatrixSolverProfile.HourCount);
    }

    [Fact]
    public void Simulate_PropagatesPhysicalBuilderValidationFailure()
    {
        var result = _service.Simulate(
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
    public void Simulate_RejectsNullRequest()
    {
        Iso52016PhysicalRoomModelRequest request = null!;

        var result = _service.Simulate(request);

        Assert.True(result.IsFailure);
        Assert.Equal(
            "ISO 52016 physical room energy simulation request is required.",
            result.Error);
    }

    private static Iso52016RoomHourlyInputProfile CreateInputProfile(
        string roomCode = "physical-service-room",
        double ventilationHeatTransferCoefficientWPerK = 5)
    {
        var hours = Enumerable
            .Range(0, 3)
            .Select(hour => new Iso52016RoomHourlyInputRecord(
                HourOfYear: hour,
                Month: 1,
                Day: 1,
                Hour: hour,
                OutdoorTemperatureC: 20,
                GroundBoundaryTemperatureC: 20,
                HeatingSetpointC: 0,
                CoolingSetpointC: 50,
                TransmissionHeatTransferCoefficientWPerK: 100,
                VentilationHeatTransferCoefficientWPerK: ventilationHeatTransferCoefficientWPerK,
                TotalHeatTransferCoefficientWPerK: 100 + ventilationHeatTransferCoefficientWPerK,
                ThermalCapacityJPerK: 3_000_000,
                SolarGainsW: 0,
                InternalGainsW: 0,
                TotalGainsW: 0))
            .ToArray();

        return new Iso52016RoomHourlyInputProfile(
            RoomCode: roomCode,
            TransmissionHeatTransferCoefficientWPerK: 100,
            VentilationHeatTransferCoefficientWPerK: ventilationHeatTransferCoefficientWPerK,
            ThermalCapacityJPerK: 3_000_000,
            HeatingSetpointC: 0,
            CoolingSetpointC: 50,
            Hours: hours);
    }
}