using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.V2;

namespace AssistantEngineer.Tests.Calculations.Iso52016.V2;

public class Iso52016V2RoomEnergySimulationResultMapperTests
{
    private readonly Iso52016V2RoomEnergySimulationService _v2Service =
        new(
            new Iso52016RoomSolarGainProfileBuilder(
                new Iso52016WindowSolarGainCalculator()),
            new Iso52016RoomInternalGainProfileBuilder(),
            new Iso52016RoomHourlyInputProfileBuilder(),
            new Iso52016V2ReducedRoomModelBuilder(),
            new Iso52016V2HourlySolver());

    private readonly Iso52016V2RoomEnergySimulationResultMapper _mapper = new();

    [Fact]
    public void Map_ConvertsV2ResultToLegacyRoomEnergySimulationContract()
    {
        var v2Result = _v2Service.Simulate(
            CreateRequest());

        Assert.True(v2Result.IsSuccess);

        var mapped = _mapper.Map(
            v2Result.Value);

        Assert.True(mapped.IsSuccess);

        Assert.Equal(v2Result.Value.RoomCode, mapped.Value.RoomCode);
        Assert.Equal(v2Result.Value.HourCount, mapped.Value.HourCount);
        Assert.Equal(v2Result.Value.AnnualHeatingEnergyKWh, mapped.Value.AnnualHeatingEnergyKWh, precision: 6);
        Assert.Equal(v2Result.Value.AnnualCoolingEnergyKWh, mapped.Value.AnnualCoolingEnergyKWh, precision: 6);
        Assert.Equal(v2Result.Value.AnnualSolarGainsKWh, mapped.Value.AnnualSolarGainsKWh, precision: 6);
        Assert.Equal(v2Result.Value.AnnualInternalGainsKWh, mapped.Value.AnnualInternalGainsKWh, precision: 6);
        Assert.Equal(v2Result.Value.MatrixSolverProfile.Hours[0].AirTemperatureAfterHvacC, mapped.Value.HeatBalanceProfile.Hours[0].IndoorTemperatureAfterHvacC, precision: 6);
    }

    private static Iso52016RoomEnergySimulationRequest CreateRequest()
    {
        return new Iso52016RoomEnergySimulationRequest(
            RoomCode: "room-1",
            WeatherSolarContext: CreateWeatherSolarContext(),
            Windows:
            [
                new(
                    WindowCode: "W1",
                    Orientation: CardinalDirection.South,
                    WindowAreaM2: 2.0,
                    SolarHeatGainCoefficient: 0.6)
            ],
            PeopleCount: 2,
            SensibleHeatGainPerPersonW: 125,
            EquipmentLoadW: 500,
            LightingLoadW: 300,
            OccupancyFactors: ConstantProfile(24, 1.0),
            EquipmentFactors: ConstantProfile(24, 1.0),
            LightingFactors: ConstantProfile(24, 1.0),
            TransmissionHeatTransferCoefficientWPerK: 120,
            VentilationHeatTransferCoefficientWPerK: 30,
            ThermalCapacityJPerK: 3_000_000,
            HeatingSetpointC: 20,
            CoolingSetpointC: 26,
            HeatBalanceOptions: new(
                InitialIndoorTemperatureC: 22));
    }

    private static Iso52016WeatherSolarContext CreateWeatherSolarContext()
    {
        var hours = Enumerable
            .Range(0, 24)
            .Select(hour => new Iso52016HourlyWeatherSolarRecord(
                HourOfYear: hour,
                Month: 1,
                Day: 1,
                Hour: hour,
                OutdoorTemperatureC: 10,
                GroundBoundaryTemperatureC: 10,
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
                        BeamIrradianceWm2: 300,
                        DiffuseSkyIrradianceWm2: 150,
                        GroundReflectedIrradianceWm2: 50,
                        TotalIrradianceWm2: 500)
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