using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.V2;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016RoomEnergySimulationServiceTests
{
    private readonly Iso52016RoomEnergySimulationService _service =
        new(
            new Iso52016RoomSolarGainProfileBuilder(
                new Iso52016WindowSolarGainCalculator()),
            new Iso52016RoomInternalGainProfileBuilder(),
            new Iso52016RoomHourlyInputProfileBuilder(),
            new Iso52016V2ReducedRoomModelBuilder(),
                new Iso52016V2HourlySolver(),
                new Iso52016V2RoomEnergySimulationResultMapper());

    [Fact]
    public void Simulate_BuildsCompleteRoomEnergySimulationResult()
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
        Assert.Equal(24, simulation.HeatBalanceProfile.HourCount);

        Assert.Equal(600.0, simulation.SolarGainProfile.GetHour(0).TotalSolarGainW, precision: 6);
        Assert.Equal(600.0, simulation.HourlyInputProfile.GetHour(0).SolarGainsW, precision: 6);
        Assert.Equal(600.0 * 24.0 / 1000.0, simulation.AnnualSolarGainsKWh, precision: 6);
        Assert.True(simulation.AnnualSolarGainsKWh > 0);
        Assert.True(simulation.AnnualInternalGainsKWh > 0);
        Assert.True(simulation.AnnualTotalGainsKWh > 0);
    }

    [Fact]
    public void Simulate_WithNoWindows_ReturnsZeroSolarGains()
    {
        var request = CreateRequest(
            windows: []);

        var result = _service.Simulate(request);

        Assert.True(result.IsSuccess);

        Assert.Equal(0.0, result.Value.AnnualSolarGainsKWh, precision: 6);
        Assert.True(result.Value.AnnualInternalGainsKWh > 0);
    }

    [Fact]
    public void Simulate_PropagatesInternalGainValidationFailure()
    {
        var request = CreateRequest(
            occupancyFactors: ConstantProfile(23, 1.0));

        var result = _service.Simulate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Occupancy factors must contain exactly 24 values.", result.Error);
    }

    [Fact]
    public void Simulate_PropagatesHourlyInputValidationFailure()
    {
        var request = CreateRequest(
            transmissionHeatTransferCoefficientWPerK: 0,
            ventilationHeatTransferCoefficientWPerK: 0);

        var result = _service.Simulate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("At least one heat transfer coefficient must be greater than zero.", result.Error);
    }

    [Fact]
    public void Simulate_RejectsEmptyRoomCode()
    {
        var request = CreateRequest(
            roomCode: " ");

        var result = _service.Simulate(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Room code is required.", result.Error);
    }

    [Fact]
    public void Simulate_ReturnsHeatingOrCoolingEnergyDependingOnConditions()
    {
        var heatingRequest = CreateRequest(
            outdoorTemperatureC: -5,
            solarIrradianceWm2: 0,
            equipmentLoadW: 0,
            lightingLoadW: 0,
            peopleCount: 0);

        var heatingResult = _service.Simulate(heatingRequest);

        Assert.True(heatingResult.IsSuccess);
        Assert.True(heatingResult.Value.AnnualHeatingEnergyKWh > 0);
        Assert.Equal(0.0, heatingResult.Value.AnnualCoolingEnergyKWh, precision: 6);

        var coolingRequest = CreateRequest(
            outdoorTemperatureC: 35,
            solarIrradianceWm2: 500,
            equipmentLoadW: 1000,
            lightingLoadW: 500,
            peopleCount: 2);

        var coolingResult = _service.Simulate(coolingRequest);

        Assert.True(coolingResult.IsSuccess);
        Assert.True(coolingResult.Value.AnnualCoolingEnergyKWh > 0);
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
