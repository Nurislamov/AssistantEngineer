using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Models.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Ground;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016HourlyComponentBreakdownTests
{
    [Fact]
    public void HeatBalanceCalculator_ReturnsRoomHourlyComponentBreakdown()
    {
        var room = CreateRoomWithEnvelope();
        var weather = CreateWeather(hourOfYear: 10, outdoorTemperatureC: -5);
        var calculator = CreateCalculator();

        var result = calculator.CalculateZoneHourEnergyNeed(
            new Iso52016ThermalZoneGroup("Zone A", [room]),
            new Iso52016ThermalZoneState(
                FloorAreaM2: room.Area.SquareMeters,
                VolumeM3: room.CalculateVolume(),
                OutdoorBoundaryHeatTransferCoefficientWPerK: 0,
                GroundBoundaryHeatTransferCoefficientWPerK: 0,
                VentilationHeatTransferCoefficientWPerK: 0,
                ThermalCapacityJPerK: 3_000_000,
                HeatingSetpointC: 20,
                CoolingSetpointC: 26),
            weather,
            new Dictionary<int, double> { [room.Id] = 0 },
            new Dictionary<int, string> { [room.Id] = "Zone A" },
            preferences: null,
            CancellationToken.None,
            groundBoundaryTemperatureC: 12);

        var roomHour = Assert.Single(result.Rooms).Hour;

        Assert.True(roomHour.HeatingLoadW > 0);
        Assert.True(roomHour.TransmissionW > 0);
        Assert.True(roomHour.VentilationW > 0);
        Assert.True(roomHour.MechanicalVentilationW > 0);
        Assert.Equal(0, roomHour.NaturalVentilationW, precision: 6);
        Assert.True(roomHour.GroundW > 0);
        Assert.Equal(0, roomHour.InfiltrationW, precision: 6);
        Assert.Equal(roomHour.TransmissionW, result.Hour.TransmissionW, precision: 6);
        Assert.Equal(roomHour.VentilationW, result.Hour.VentilationW, precision: 6);
        Assert.Equal(roomHour.MechanicalVentilationW, result.Hour.MechanicalVentilationW, precision: 6);
        Assert.Equal(roomHour.NaturalVentilationW, result.Hour.NaturalVentilationW, precision: 6);
        Assert.Equal(roomHour.GroundW, result.Hour.GroundW, precision: 6);
    }

    [Fact]
    public void HeatBalanceCalculator_SeparatesMechanicalNaturalVentilationAndInfiltrationForColdOutdoorHour()
    {
        var room = CreateRoomWithEnvelope();
        var weather = CreateWeather(hourOfYear: 10, outdoorTemperatureC: -5);
        var calculator = CreateCalculator(new FixedVentilationHeatTransferCalculator(
            mechanicalWPerK: 5,
            infiltrationWPerK: 1), new FixedNaturalVentilationAirflowService(2));

        var result = calculator.CalculateZoneHourEnergyNeed(
            new Iso52016ThermalZoneGroup("Zone A", [room]),
            CreateZoneState(room, heatingSetpointC: 20, coolingSetpointC: 26),
            weather,
            new Dictionary<int, double> { [room.Id] = 0 },
            new Dictionary<int, string> { [room.Id] = "Zone A" },
            preferences: null,
            CancellationToken.None,
            groundBoundaryTemperatureC: 12);

        var roomHour = Assert.Single(result.Rooms).Hour;

        Assert.Equal(20, roomHour.OperativeTemperatureC, precision: 6);
        Assert.Equal(125, roomHour.MechanicalVentilationW, precision: 6);
        Assert.Equal(50, roomHour.NaturalVentilationW, precision: 6);
        Assert.Equal(175, roomHour.VentilationW, precision: 6);
        Assert.Equal(25, roomHour.InfiltrationW, precision: 6);
        Assert.Equal(-125, roomHour.MechanicalVentilationBalanceW, precision: 6);
        Assert.Equal(-50, roomHour.NaturalVentilationBalanceW, precision: 6);
        Assert.Equal(-175, roomHour.VentilationBalanceW, precision: 6);
        Assert.Equal(-25, roomHour.InfiltrationBalanceW, precision: 6);
        Assert.Equal(roomHour.MechanicalVentilationW, result.Hour.MechanicalVentilationW, precision: 6);
        Assert.Equal(roomHour.NaturalVentilationW, result.Hour.NaturalVentilationW, precision: 6);
        Assert.Equal(roomHour.InfiltrationW, result.Hour.InfiltrationW, precision: 6);
        Assert.Equal(roomHour.InfiltrationBalanceW, result.Hour.InfiltrationBalanceW, precision: 6);
    }

    [Fact]
    public void HeatBalanceCalculator_SeparatesMechanicalNaturalVentilationAndInfiltrationForHotOutdoorHour()
    {
        var room = CreateRoomWithEnvelope();
        var weather = CreateWeather(hourOfYear: 1000, outdoorTemperatureC: 34);
        var calculator = CreateCalculator(new FixedVentilationHeatTransferCalculator(
            mechanicalWPerK: 5,
            infiltrationWPerK: 1), new FixedNaturalVentilationAirflowService(2));

        var result = calculator.CalculateZoneHourEnergyNeed(
            new Iso52016ThermalZoneGroup("Zone A", [room]),
            CreateZoneState(room, heatingSetpointC: 20, coolingSetpointC: 24),
            weather,
            new Dictionary<int, double> { [room.Id] = 40 },
            new Dictionary<int, string> { [room.Id] = "Zone A" },
            preferences: null,
            CancellationToken.None,
            groundBoundaryTemperatureC: 24);

        var roomHour = Assert.Single(result.Rooms).Hour;

        Assert.Equal(24, roomHour.OperativeTemperatureC, precision: 6);
        Assert.Equal(50, roomHour.MechanicalVentilationW, precision: 6);
        Assert.Equal(20, roomHour.NaturalVentilationW, precision: 6);
        Assert.Equal(70, roomHour.VentilationW, precision: 6);
        Assert.Equal(10, roomHour.InfiltrationW, precision: 6);
        Assert.Equal(50, roomHour.MechanicalVentilationBalanceW, precision: 6);
        Assert.Equal(20, roomHour.NaturalVentilationBalanceW, precision: 6);
        Assert.Equal(70, roomHour.VentilationBalanceW, precision: 6);
        Assert.Equal(10, roomHour.InfiltrationBalanceW, precision: 6);
    }

    [Fact]
    public void HeatBalanceCalculator_DoesNotFakeNaturalVentilationWhenNaturalServiceIsAbsent()
    {
        var room = CreateRoomWithEnvelope();
        var weather = CreateWeather(hourOfYear: 10, outdoorTemperatureC: -5);
        var calculator = CreateCalculator(new FixedVentilationHeatTransferCalculator(
            mechanicalWPerK: 5,
            infiltrationWPerK: 1));

        var result = calculator.CalculateZoneHourEnergyNeed(
            new Iso52016ThermalZoneGroup("Zone A", [room]),
            CreateZoneState(room, heatingSetpointC: 20, coolingSetpointC: 26),
            weather,
            new Dictionary<int, double> { [room.Id] = 0 },
            new Dictionary<int, string> { [room.Id] = "Zone A" },
            preferences: null,
            CancellationToken.None,
            groundBoundaryTemperatureC: 12);

        var roomHour = Assert.Single(result.Rooms).Hour;

        Assert.Equal(125, roomHour.MechanicalVentilationW, precision: 6);
        Assert.Equal(0, roomHour.NaturalVentilationW, precision: 6);
        Assert.Equal(125, roomHour.VentilationW, precision: 6);
        Assert.Equal(-125, roomHour.MechanicalVentilationBalanceW, precision: 6);
        Assert.Equal(0, roomHour.NaturalVentilationBalanceW, precision: 6);
        Assert.Equal(-125, roomHour.VentilationBalanceW, precision: 6);
    }

    [Fact]
    public void HeatBalanceCalculator_DoesNotFakeMechanicalVentilationWhenMechanicalCalculatorIsAbsent()
    {
        var room = CreateRoomWithEnvelope();
        var weather = CreateWeather(hourOfYear: 10, outdoorTemperatureC: -5);
        var calculator = CreateCalculator(
            ventilationCalculator: null,
            naturalVentilationAirflowService: new FixedNaturalVentilationAirflowService(2),
            useDefaultVentilationCalculator: false);

        var result = calculator.CalculateZoneHourEnergyNeed(
            new Iso52016ThermalZoneGroup("Zone A", [room]),
            CreateZoneState(room, heatingSetpointC: 20, coolingSetpointC: 26),
            weather,
            new Dictionary<int, double> { [room.Id] = 0 },
            new Dictionary<int, string> { [room.Id] = "Zone A" },
            preferences: null,
            CancellationToken.None,
            groundBoundaryTemperatureC: 12);

        var roomHour = Assert.Single(result.Rooms).Hour;

        Assert.Equal(0, roomHour.MechanicalVentilationW, precision: 6);
        Assert.Equal(50, roomHour.NaturalVentilationW, precision: 6);
        Assert.Equal(50, roomHour.VentilationW, precision: 6);
        Assert.Equal(0, roomHour.InfiltrationW, precision: 6);
        Assert.Equal(0, roomHour.MechanicalVentilationBalanceW, precision: 6);
        Assert.Equal(-50, roomHour.NaturalVentilationBalanceW, precision: 6);
        Assert.Equal(-50, roomHour.VentilationBalanceW, precision: 6);
    }

    [Fact]
    public void ResultComposer_AggregatesZoneComponentsToBuildingHourlyResults()
    {
        var building = CreateBuilding();
        var composer = new Iso52016HourlyResultComposer();
        var zones = new[]
        {
            new Iso52016ZoneHourlyEnergyNeed(
                "Zone A",
                HourOfYear: 0,
                Month: 1,
                HeatingLoadW: 100,
                CoolingLoadW: 0,
                OperativeTemperatureC: 20,
                OutdoorTemperatureC: -5,
                InternalGainsW: 10,
                SolarGainsW: 20,
                TransmissionW: 30,
                VentilationW: 40,
                InfiltrationW: 12,
                GroundW: 50,
                TransmissionBalanceW: -30,
                VentilationBalanceW: -40,
                InfiltrationBalanceW: -12,
                GroundBalanceW: -50,
                MechanicalVentilationW: 25,
                NaturalVentilationW: 15,
                MechanicalVentilationBalanceW: -25,
                NaturalVentilationBalanceW: -15),
            new Iso52016ZoneHourlyEnergyNeed(
                "Zone B",
                HourOfYear: 0,
                Month: 1,
                HeatingLoadW: 200,
                CoolingLoadW: 0,
                OperativeTemperatureC: 21,
                OutdoorTemperatureC: -5,
                InternalGainsW: 15,
                SolarGainsW: 25,
                TransmissionW: 35,
                VentilationW: 45,
                InfiltrationW: 13,
                GroundW: 55,
                TransmissionBalanceW: -35,
                VentilationBalanceW: -45,
                InfiltrationBalanceW: -13,
                GroundBalanceW: -55,
                MechanicalVentilationW: 30,
                NaturalVentilationW: 15,
                MechanicalVentilationBalanceW: -30,
                NaturalVentilationBalanceW: -15)
        };

        var annual = composer.ComposeAnnualResult(
            building,
            weatherYear: 2026,
            zoneHourlyResults: zones,
            roomHourlyResults: []);

        var hour = Assert.Single(annual.HourlyResults);

        Assert.Equal(65, hour.TransmissionW, precision: 6);
        Assert.Equal(85, hour.VentilationW, precision: 6);
        Assert.Equal(55, hour.MechanicalVentilationW, precision: 6);
        Assert.Equal(30, hour.NaturalVentilationW, precision: 6);
        Assert.Equal(25, hour.InfiltrationW, precision: 6);
        Assert.Equal(105, hour.GroundW, precision: 6);
        Assert.Equal(45, hour.SolarGainsW, precision: 6);
        Assert.Equal(25, hour.InternalGainsW, precision: 6);
        Assert.Equal(300, hour.HeatingLoadW, precision: 6);
        Assert.Equal(-55, hour.MechanicalVentilationBalanceW, precision: 6);
        Assert.Equal(-30, hour.NaturalVentilationBalanceW, precision: 6);
        Assert.Equal(-85, hour.VentilationBalanceW, precision: 6);
        Assert.Equal(-25, hour.InfiltrationBalanceW, precision: 6);
    }

    private static Iso52016HourlyHeatBalanceCalculator CreateCalculator(
        IVentilationHeatTransferCalculator? ventilationCalculator = null,
        INaturalVentilationAirflowService? naturalVentilationAirflowService = null,
        bool useDefaultVentilationCalculator = true) =>
        new(
            new SolarRadiationService(),
            useDefaultVentilationCalculator
                ? ventilationCalculator ??
                  new VentilationHeatTransferCalculator(new Iso16798ReferenceData(new InternalLoadStandardProvider()))
                : null,
            null,
            new BuildingEnvelopeReferenceData(),
            new En16798ProfileCatalog(),
            new Iso13370GroundHeatTransferService(Options.Create(new Iso13370GroundHeatTransferOptions())),
            naturalVentilationAirflowService,
            new Iso52016EnergyNeedOptions
            {
                DefaultGroundBoundaryTemperatureC = 12,
                LatitudeDegrees = 41.3
            },
            new En16798ProfileOptions
            {
                UseStandardProfilesWhenMissingSchedules = false
            },
            new HourlyInternalGainProfileService(
                new HourlyRoomProfileAccessor(
                    new RoomAnnualProfileSetProvider(
                        new AnnualScheduleGenerationService(
                            new UzbekistanHolidayCalendarProvider(),
                            new AnnualProfileTemplateProvider())))));

    private static Iso52016ThermalZoneState CreateZoneState(
        Room room,
        double heatingSetpointC,
        double coolingSetpointC) =>
        new(
            FloorAreaM2: room.Area.SquareMeters,
            VolumeM3: room.CalculateVolume(),
            OutdoorBoundaryHeatTransferCoefficientWPerK: 0,
            GroundBoundaryHeatTransferCoefficientWPerK: 0,
            VentilationHeatTransferCoefficientWPerK: 0,
            ThermalCapacityJPerK: 3_000_000,
            HeatingSetpointC: heatingSetpointC,
            CoolingSetpointC: coolingSetpointC);

    private static Room CreateRoomWithEnvelope()
    {
        var building = CreateBuilding();
        var floor = building.AddFloor("Floor").Value;
        var room = floor.AddRoom(
            "Room",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(20).Value,
            peopleCount: 2,
            equipmentLoad: Power.FromWatts(500).Value,
            lightingLoad: Power.FromWatts(300).Value,
            type: RoomType.Office).Value;

        Assert.True(room.AddWall(
            Area.FromSquareMeters(12).Value,
            ThermalTransmittance.FromValue(0.4).Value,
            CardinalDirection.South,
            WallBoundaryType.External).IsSuccess);
        Assert.True(room.AddWindow(
            Area.FromSquareMeters(2).Value,
            ThermalTransmittance.FromValue(1.5).Value,
            SolarHeatGainCoefficient.FromValue(0.6).Value,
            CardinalDirection.South).IsSuccess);

        return room;
    }

    private static Building CreateBuilding()
    {
        var climateZone = ClimateZone.Create(
            "Climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-10).Value).Value;
        var project = Project.Create("Project").Value;
        return Building.Create("Building", project, climateZone).Value;
    }

    private static AnnualHourlyData CreateWeather(
        int hourOfYear,
        double outdoorTemperatureC)
    {
        var annual = AnnualClimateData.Create(
            ClimateZone.Create(
                "Climate",
                Temperature.FromCelsius(35).Value,
                Temperature.FromCelsius(-10).Value).Value,
            2026).Value;

        return AnnualHourlyData.Create(
            annual,
            hourOfYear,
            outdoorTemperatureC,
            directSolarRadiation: 0,
            diffuseSolarRadiation: 0,
            windSpeedMPerS: 2).Value;
    }

    private sealed class FixedVentilationHeatTransferCalculator : IVentilationHeatTransferCalculator
    {
        private readonly double _mechanicalWPerK;
        private readonly double _infiltrationWPerK;

        public FixedVentilationHeatTransferCalculator(
            double mechanicalWPerK,
            double infiltrationWPerK)
        {
            _mechanicalWPerK = mechanicalWPerK;
            _infiltrationWPerK = infiltrationWPerK;
        }

        public double Calculate(Room room, VentilationCalculationContext context) =>
            _mechanicalWPerK + _infiltrationWPerK;

        public double CalculateMechanical(Room room, VentilationCalculationContext context) =>
            _mechanicalWPerK;

        public double CalculateInfiltration(Room room, VentilationCalculationContext context) =>
            _infiltrationWPerK;
    }

    private sealed class FixedNaturalVentilationAirflowService : INaturalVentilationAirflowService
    {
        private readonly double _heatTransferWPerK;

        public FixedNaturalVentilationAirflowService(double heatTransferWPerK)
        {
            _heatTransferWPerK = heatTransferWPerK;
        }

        public double CalculateHeatTransferCoefficient(
            Room room,
            double indoorTemperatureC,
            double outdoorTemperatureC,
            double windSpeedMPerS,
            double demandFactor,
            int hourOfDay) =>
            _heatTransferWPerK;
    }
}
