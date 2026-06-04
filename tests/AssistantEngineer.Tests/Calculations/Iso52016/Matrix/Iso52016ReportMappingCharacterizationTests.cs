using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.WeatherSolar;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public sealed class Iso52016ReportMappingCharacterizationTests
{
    private readonly Iso52016MatrixRoomEnergySimulationService _service =
        new(
            new Iso52016RoomSolarGainProfileBuilder(
                new Iso52016WindowSolarGainCalculator()),
            new Iso52016RoomInternalGainProfileBuilder(),
            new Iso52016RoomHourlyInputProfileBuilder(),
            new Iso52016MatrixReducedRoomModelBuilder(),
            new Iso52016MatrixHourlySolver());

    private readonly Iso52016MatrixRoomEnergySimulationResultMapper _mapper = new();

    [Fact]
    public void Map_SelectedFieldsRemainStableAndDeterministic()
    {
        var request = CreateRequest();
        var result = _service.Simulate(request);
        Assert.True(result.IsSuccess, result.Error);

        var mappedA = _mapper.Map(result.Value);
        var mappedB = _mapper.Map(result.Value);

        Assert.True(mappedA.IsSuccess, mappedA.Error);
        Assert.True(mappedB.IsSuccess, mappedB.Error);

        Assert.Equal(mappedA.Value.RoomCode, mappedB.Value.RoomCode);
        Assert.Equal(mappedA.Value.HourCount, mappedB.Value.HourCount);
        Assert.InRange(Math.Abs(mappedA.Value.AnnualHeatingEnergyKWh - mappedB.Value.AnnualHeatingEnergyKWh), 0.0, 1e-9);
        Assert.InRange(Math.Abs(mappedA.Value.AnnualCoolingEnergyKWh - mappedB.Value.AnnualCoolingEnergyKWh), 0.0, 1e-9);

        Assert.InRange(
            Math.Abs(mappedA.Value.HeatBalanceProfile.Hours[0].IndoorTemperatureAfterHvacC
                     - result.Value.MatrixSolverProfile.Hours[0].AirTemperatureAfterHvacC),
            0.0,
            1e-6);

        Assert.All(mappedA.Value.HeatBalanceProfile.Hours, hour =>
        {
            Assert.False(double.IsNaN(hour.IndoorTemperatureAfterHvacC));
            Assert.False(double.IsInfinity(hour.IndoorTemperatureAfterHvacC));
            Assert.False(double.IsNaN(hour.HeatingLoadW));
            Assert.False(double.IsNaN(hour.CoolingLoadW));
        });
    }

    private static Iso52016RoomEnergySimulationRequest CreateRequest()
    {
        return new Iso52016RoomEnergySimulationRequest(
            RoomCode: "room-characterization",
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

    private static IReadOnlyList<double> ConstantProfile(int hourCount, double value) =>
        Enumerable.Repeat(value, hourCount).ToArray();
}
