using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

namespace AssistantEngineer.Tests.Calculations.Ventilation;

public sealed class NaturalVentilationZoneIntegrationValidatorTests
{
    private readonly NaturalVentilationZoneIntegrationValidator _validator = new();

    [Fact]
    public void AcceptsValidZoneIntegrationInput()
    {
        var input = CreateInput();

        var result = _validator.Validate(input);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RejectsMissingCalculationId()
    {
        var input = CreateInput() with { CalculationId = "" };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-ZONE-CALCULATION-ID-MISSING");
    }

    [Fact]
    public void RejectsMissingOpenings()
    {
        var input = CreateInput() with { Openings = [] };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-ZONE-OPENINGS-MISSING");
    }

    [Fact]
    public void RejectsMissingHourlyEnvironments()
    {
        var input = CreateInput() with { HourlyEnvironments = [] };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-ZONE-HOURLY-ENVIRONMENTS-MISSING");
    }

    [Fact]
    public void ReportsOpeningRoomMissing()
    {
        var input = CreateInput() with
        {
            Openings =
            [
                CreateOpening(roomId: "R-MISSING")
            ]
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-ZONE-OPENING-ROOM-MISSING");
    }

    [Fact]
    public void ReportsInvalidWindSpeed()
    {
        var input = CreateInput() with
        {
            HourlyEnvironments =
            [
                CreateEnvironment(hour: 0, windSpeed: -1.0)
            ]
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-ZONE-WIND-SPEED-INVALID");
    }

    private static NaturalVentilationZoneIntegrationInput CreateInput() =>
        new(
            CalculationId: "VENT-ZONE-1",
            Topology: CreateTopology(),
            Openings:
            [
                CreateOpening()
            ],
            ControlRules:
            [
                CreateRule()
            ],
            HourlyEnvironments:
            [
                CreateEnvironment(hour: 0)
            ],
            FlowConfiguration: NaturalVentilationFlowConfiguration.WindOnly,
            DefaultAirDensityKgPerCubicMeter: 1.2,
            DefaultAirSpecificHeatJPerKgKelvin: 1005.0,
            DisclosureOverride: null,
            Source: "UnitTest");

    private static BuildingThermalTopology CreateTopology() =>
        new(
            BuildingId: "B1",
            Zones: [new ThermalTopologyZone("Z1", "Zone 1", ["R1"], [])],
            Rooms: [new ThermalTopologyRoom("R1", "Z1", 100.0, 40.0, [], [])],
            Surfaces: [],
            Disclosure: new StandardCalculationDisclosureFactory().CreateThermalZonesDisclosure(),
            Diagnostics: []);

    private static NaturalVentilationOpeningGeometry CreateOpening(string roomId = "R1") =>
        new(
            OpeningId: "O1",
            RoomId: roomId,
            ZoneId: "Z1",
            SurfaceId: "S1",
            OpeningType: NaturalVentilationOpeningType.Window,
            OpeningAreaSquareMeters: 1.0,
            OpeningHeightMeters: 1.0,
            OpeningWidthMeters: 1.0,
            OpeningCenterHeightMeters: 1.5,
            BottomHeightMeters: 1.0,
            TopHeightMeters: 2.0,
            OpeningFraction: 1.0,
            DischargeCoefficient: 0.6,
            WindPressureCoefficient: 0.5,
            OppositeWindPressureCoefficient: 0.0,
            OrientationAzimuthDegrees: 180.0,
            Source: "UnitTest",
            Diagnostics: []);

    private static NaturalVentilationOpeningControlRule CreateRule() =>
        new(
            RuleId: "R1",
            OpeningId: "O1",
            RoomId: "R1",
            ZoneId: "Z1",
            ControlMode: NaturalVentilationControlMode.AlwaysOpen,
            NightVentilationMode: NaturalVentilationNightVentilationMode.Disabled,
            FixedOpeningFraction: null,
            MinimumOpeningFraction: null,
            MaximumOpeningFraction: null,
            IndoorTemperatureOpenAboveCelsius: null,
            IndoorTemperatureCloseBelowCelsius: null,
            OutdoorTemperatureMinimumCelsius: null,
            OutdoorTemperatureMaximumCelsius: null,
            IndoorOutdoorTemperatureDifferenceMinimumKelvin: null,
            RequiresOccupancy: null,
            ScheduleId: null,
            OccupancyProfileId: null,
            Source: "UnitTest",
            Diagnostics: []);

    private static NaturalVentilationHourlyZoneEnvironment CreateEnvironment(
        int hour,
        double windSpeed = 2.0) =>
        new(
            HourIndex: hour,
            RoomId: "R1",
            ZoneId: "Z1",
            IndoorTemperatureCelsius: 22.0,
            OutdoorTemperatureCelsius: 12.0,
            WindSpeedMetersPerSecond: windSpeed,
            OccupancyFraction: 1.0,
            ScheduleFraction: 1.0,
            IsNightHour: false,
            AirDensityKgPerCubicMeter: 1.2,
            AirSpecificHeatJPerKgKelvin: 1005.0,
            Source: "UnitTest",
            Diagnostics: []);
}
