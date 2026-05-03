using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016MatrixRoomEnergySimulationServiceTests
{
    private readonly Iso52016MatrixRoomEnergySimulationService _service =
        new(
            new Iso52016RoomSolarGainProfileBuilder(
                new Iso52016WindowSolarGainCalculator()),
            new Iso52016RoomInternalGainProfileBuilder(),
            new Iso52016RoomHourlyInputProfileBuilder(),
            new Iso52016MatrixReducedRoomModelBuilder(),
            new Iso52016MatrixHourlySolver());

    [Fact]
    public void Simulate_BuildsCompleteV2RoomEnergySimulationResult()
    {
        var request = CreateRequest();

        var result = _service.Simulate(request);

        Assert.True(result.IsSuccess);

        var simulation = result.Value;

        Assert.Equal("room-1", simulation.RoomCode);
        Assert.Equal(24, simulation.HourCount);
        Assert.Equal(24, simulation.SolarGainProfile.HourCount);
        Assert.Equal(24, simulation.InternalGainProfile.HourCount);
        Assert.Equal(24, simulation.HourlyInputProfile.HourCount);
        Assert.Equal(24, simulation.MatrixSolverProfile.HourCount);
        Assert.Single(simulation.MatrixSolverRequest.Nodes);
        Assert.Single(simulation.MatrixSolverRequest.BoundaryConductances);
        Assert.True(simulation.AnnualSolarGainsKWh > 0);
        Assert.True(simulation.AnnualInternalGainsKWh > 0);
        Assert.True(simulation.AnnualTotalGainsKWh > 0);
    }

    [Fact]
    public void Simulate_ColdWeatherProducesHeatingNeedThroughV2Path()
    {
        var request = CreateRequest(
            outdoorTemperatureC: -10,
            solarIrradianceWm2: 0,
            peopleCount: 0,
            equipmentLoadW: 0,
            lightingLoadW: 0);

        var result = _service.Simulate(request);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.AnnualHeatingEnergyKWh > 0);
        Assert.Equal(0.0, result.Value.AnnualCoolingEnergyKWh, precision: 6);
        Assert.True(result.Value.PeakHeatingLoadW > 0);
    }

    [Fact]
    public void Simulate_HotWeatherAndHighGainsProduceCoolingNeedThroughV2Path()
    {
        var request = CreateRequest(
            outdoorTemperatureC: 35,
            solarIrradianceWm2: 700,
            peopleCount: 4,
            equipmentLoadW: 1200,
            lightingLoadW: 800);

        var result = _service.Simulate(request);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.AnnualCoolingEnergyKWh > 0);
        Assert.True(result.Value.PeakCoolingLoadW > 0);
    }

    [Fact]
    public void Simulate_PropagatesScheduleValidationFailure()
    {
        var request = CreateRequest(
            occupancyFactors: ConstantProfile(23, 1.0));

        var result = _service.Simulate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Occupancy factors must contain exactly 24 values.", result.Error);
    }

    private static Iso52016RoomEnergySimulationRequest CreateRequest(
        string roomCode = "room-1",
        IReadOnlyList<Iso52016WindowSolarGainInput>? windows = null,
        IReadOnlyList<double>? occupancyFactors = null,
        double transmissionHeatTransferCoefficientWPerK = 120,
        double ventilationHeatTransferCoefficientWPerK = 30,
        double outdoorTemperatureC = 10,
        double solarIrradianceWm2 = 500,
        int peopleCount = 2,
        double equipmentLoadW = 500,
        double lightingLoadW = 300)
    {
        return new Iso52016RoomEnergySimulationRequest(
            RoomCode: roomCode,
            WeatherSolarContext: CreateWeatherSolarContext(
                outdoorTemperatureC,
                solarIrradianceWm2),
            Windows: windows ??
            [
                new(
                    WindowCode: "W1",
                    Orientation: CardinalDirection.South,
                    WindowAreaM2: 2.0,
                    SolarHeatGainCoefficient: 0.6)
            ],
            PeopleCount: peopleCount,
            SensibleHeatGainPerPersonW: 125,
            EquipmentLoadW: equipmentLoadW,
            LightingLoadW: lightingLoadW,
            OccupancyFactors: occupancyFactors ?? ConstantProfile(24, 1.0),
            EquipmentFactors: ConstantProfile(24, 1.0),
            LightingFactors: ConstantProfile(24, 1.0),
            TransmissionHeatTransferCoefficientWPerK: transmissionHeatTransferCoefficientWPerK,
            VentilationHeatTransferCoefficientWPerK: ventilationHeatTransferCoefficientWPerK,
            ThermalCapacityJPerK: 3_000_000,
            HeatingSetpointC: 20,
            CoolingSetpointC: 26,
            HeatBalanceOptions: new(
                InitialIndoorTemperatureC: 22));
    }

    private static Iso52016WeatherSolarContext CreateWeatherSolarContext(
        double outdoorTemperatureC,
        double solarIrradianceWm2)
    {
        var hours = Enumerable
            .Range(0, 24)
            .Select(hour => new Iso52016HourlyWeatherSolarRecord(
                HourOfYear: hour,
                Month: 1,
                Day: 1,
                Hour: hour,
                OutdoorTemperatureC: outdoorTemperatureC,
                GroundBoundaryTemperatureC: outdoorTemperatureC,
                SolarAltitudeDegrees: 30,
                SolarAzimuthDegrees: 180,
                DirectNormalIrradianceWm2: 600,
                DiffuseHorizontalIrradianceWm2: 100,
                GlobalHorizontalIrradianceWm2: 400,
                SurfaceIrradiance:
                [
                    new Iso52016SurfaceWeatherSolarRecord(
                        SurfaceCode: WeatherSolarSurfaceCodes.South,
                        Orientation: WeatherSolarSurface.South.Orientation,
                        IncidenceAngleDegrees: 45,
                        BeamIrradianceWm2: solarIrradianceWm2 * 0.6,
                        DiffuseSkyIrradianceWm2: solarIrradianceWm2 * 0.3,
                        GroundReflectedIrradianceWm2: solarIrradianceWm2 * 0.1,
                        TotalIrradianceWm2: solarIrradianceWm2)
                ]))
            .ToArray();

        return new Iso52016WeatherSolarContext(
            Year: 2026,
            TimeZoneOffset: TimeSpan.Zero,
            LatitudeDegrees: 0,
            LongitudeDegrees: 0,
            Hours: hours);
    }

    private static IReadOnlyList<double> ConstantProfile(
        int hourCount,
        double value) =>
        Enumerable
            .Repeat(value, hourCount)
            .ToArray();
}