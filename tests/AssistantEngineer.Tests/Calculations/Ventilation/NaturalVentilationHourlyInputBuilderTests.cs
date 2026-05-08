using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

namespace AssistantEngineer.Tests.Calculations.Ventilation;

public sealed class NaturalVentilationHourlyInputBuilderTests
{
    private readonly NaturalVentilationHourlyInputBuilder _builder = new();

    [Fact]
    public void BuildsHourlyAirflowInputForMatchingRoomOpening()
    {
        var input = CreateInput(
            openings: [CreateOpening("O1", "R1")],
            environments: [CreateEnvironment(0, "R1")]);
        var environment = input.HourlyEnvironments[0];
        var operations =
            new[]
            {
                CreateOperation("Rule-1", "O1", 0, 0.5, roomId: "R1")
            };

        var result = _builder.BuildHourlyAirflowInput(input, environment, operations);

        var opening = Assert.Single(result.Openings);
        Assert.NotNull(opening.OpeningFraction);
        Assert.Equal(0.5, opening.OpeningFraction!.Value, 6);
    }

    [Fact]
    public void UsesZeroFractionWhenNoOperationExists()
    {
        var input = CreateInput(
            openings: [CreateOpening("O1", "R1")],
            environments: [CreateEnvironment(0, "R1")]);
        var environment = input.HourlyEnvironments[0];

        var result = _builder.BuildHourlyAirflowInput(input, environment, []);

        var opening = Assert.Single(result.Openings);
        Assert.NotNull(opening.OpeningFraction);
        Assert.Equal(0.0, opening.OpeningFraction!.Value, 6);
        Assert.Contains(opening.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-ZONE-OPENING-NO-OPERATION");
    }

    [Fact]
    public void MultipleOperationsUseMaximumFraction()
    {
        var input = CreateInput(
            openings: [CreateOpening("O1", "R1")],
            environments: [CreateEnvironment(0, "R1")]);
        var environment = input.HourlyEnvironments[0];
        var operations =
            new[]
            {
                CreateOperation("Rule-1", "O1", 0, 0.2, roomId: "R1"),
                CreateOperation("Rule-2", "O1", 0, 0.8, roomId: "R1")
            };

        var result = _builder.BuildHourlyAirflowInput(input, environment, operations);

        var opening = Assert.Single(result.Openings);
        Assert.NotNull(opening.OpeningFraction);
        Assert.Equal(0.8, opening.OpeningFraction!.Value, 6);
        Assert.Contains(opening.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-ZONE-MULTIPLE-OPERATIONS-MAX-FRACTION-USED");
    }

    [Fact]
    public void NoMatchingOpeningsProducesDiagnostic()
    {
        var input = CreateInput(
            openings: [CreateOpening("O1", "R1")],
            environments: [CreateEnvironment(0, "R2")]);
        var environment = input.HourlyEnvironments[0];

        var result = _builder.BuildHourlyAirflowInput(input, environment, []);

        Assert.Empty(result.Openings);
        Assert.Contains(result.Environment.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-ZONE-NO-MATCHING-OPENINGS");
    }

    private static NaturalVentilationZoneIntegrationInput CreateInput(
        IReadOnlyList<NaturalVentilationOpeningGeometry> openings,
        IReadOnlyList<NaturalVentilationHourlyZoneEnvironment> environments) =>
        new(
            CalculationId: "VENT-ZONE-HOURLY-INPUT",
            Topology: CreateTopology(),
            Openings: openings,
            ControlRules: [CreateRule("Rule-1", "O1", "R1")],
            HourlyEnvironments: environments,
            FlowConfiguration: NaturalVentilationFlowConfiguration.WindOnly,
            DefaultAirDensityKgPerCubicMeter: 1.2,
            DefaultAirSpecificHeatJPerKgKelvin: 1005.0,
            DisclosureOverride: null,
            Source: "UnitTest");

    private static BuildingThermalTopology CreateTopology() =>
        new(
            BuildingId: "B1",
            Zones: [new ThermalTopologyZone("Z1", "Zone 1", ["R1", "R2"], [])],
            Rooms:
            [
                new ThermalTopologyRoom("R1", "Z1", 100.0, 40.0, [], []),
                new ThermalTopologyRoom("R2", "Z1", 80.0, 30.0, [], [])
            ],
            Surfaces: [],
            Disclosure: new StandardCalculationDisclosureFactory().CreateThermalZonesDisclosure(),
            Diagnostics: []);

    private static NaturalVentilationOpeningGeometry CreateOpening(
        string openingId,
        string roomId) =>
        new(
            OpeningId: openingId,
            RoomId: roomId,
            ZoneId: "Z1",
            SurfaceId: $"S-{openingId}",
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

    private static NaturalVentilationOpeningControlRule CreateRule(
        string ruleId,
        string openingId,
        string roomId) =>
        new(
            RuleId: ruleId,
            OpeningId: openingId,
            RoomId: roomId,
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
        string roomId) =>
        new(
            HourIndex: hour,
            RoomId: roomId,
            ZoneId: "Z1",
            IndoorTemperatureCelsius: 22.0,
            OutdoorTemperatureCelsius: 12.0,
            WindSpeedMetersPerSecond: 3.0,
            OccupancyFraction: 1.0,
            ScheduleFraction: 1.0,
            IsNightHour: false,
            AirDensityKgPerCubicMeter: 1.2,
            AirSpecificHeatJPerKgKelvin: 1005.0,
            Source: "UnitTest",
            Diagnostics: []);

    private static NaturalVentilationOpeningOperationResult CreateOperation(
        string ruleId,
        string openingId,
        int hour,
        double openingFraction,
        string? roomId = null,
        string? zoneId = "Z1") =>
        new(
            RuleId: ruleId,
            OpeningId: openingId,
            RoomId: roomId,
            ZoneId: zoneId,
            HourIndex: hour,
            ControlMode: NaturalVentilationControlMode.AlwaysOpen,
            OpeningFraction: openingFraction,
            IsOpen: openingFraction > 0.0,
            IsNightVentilationActive: false,
            ActiveReasons: ["UnitTest"],
            Diagnostics: []);
}
