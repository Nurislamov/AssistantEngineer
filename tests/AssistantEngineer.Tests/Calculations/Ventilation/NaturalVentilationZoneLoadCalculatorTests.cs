using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

namespace AssistantEngineer.Tests.Calculations.Ventilation;

public sealed class NaturalVentilationZoneLoadCalculatorTests
{
    private static readonly string[] RequiredForbiddenClaims =
    [
        "Full ISO compliance",
        "Full EN compliance",
        "StandardReference equivalence",
        "EnergyPlus comparison workflow",
        "ASHRAE 140 / BESTEST-style validation anchor"
    ];

    private readonly NaturalVentilationZoneLoadCalculator _calculator = new(
        new NaturalVentilationZoneIntegrationValidator(),
        new NaturalVentilationOpeningControlEvaluator(
            new NaturalVentilationControlRuleValidator(),
            new StandardCalculationDisclosureFactory()),
        new NaturalVentilationHourlyInputBuilder(),
        new NaturalVentilationAirflowCalculator(
            new NaturalVentilationOpeningGeometryNormalizer(),
            new NaturalVentilationInputValidator(),
            new NaturalVentilationPressureCalculator(),
            new StandardCalculationDisclosureFactory()),
        new StandardCalculationDisclosureFactory());

    [Fact]
    public void CalculatesHourlyRoomAndZoneVentilationLoad()
    {
        var input = CreateInput(
            topology: CreateTopology([CreateRoom("R1", volume: 100.0)]),
            openings: [CreateOpening("O1", roomId: "R1")],
            rules: [CreateRule("Rule-1", openingId: "O1", roomId: "R1")],
            environments: [CreateEnvironment(hour: 0, roomId: "R1", indoorTemperature: 22.0, outdoorTemperature: 12.0)]);

        var result = _calculator.Calculate(input);

        var zone = Assert.Single(result.HourlyZones);
        Assert.True(zone.TotalAirflowCubicMetersPerSecond > 0.0);
        Assert.True(zone.VentilationHeatTransferCoefficientWPerKelvin > 0.0);
        Assert.True(zone.SensibleVentilationLoadWatts > 0.0);
        Assert.True(zone.AirChangesPerHour > 0.0);
    }

    [Fact]
    public void ClosedOpeningProducesZeroAirflowHveAndLoad()
    {
        var input = CreateInput(
            topology: CreateTopology([CreateRoom("R1", volume: 100.0)]),
            openings: [CreateOpening("O1", roomId: "R1")],
            rules: [CreateRule("Rule-1", openingId: "O1", roomId: "R1", mode: NaturalVentilationControlMode.AlwaysClosed)],
            environments: [CreateEnvironment(hour: 0, roomId: "R1", indoorTemperature: 22.0, outdoorTemperature: 12.0)]);

        var result = _calculator.Calculate(input);

        var zone = Assert.Single(result.HourlyZones);
        Assert.Equal(0.0, zone.TotalAirflowCubicMetersPerSecond, 6);
        Assert.Equal(0.0, zone.VentilationHeatTransferCoefficientWPerKelvin, 6);
        Assert.Equal(0.0, zone.SensibleVentilationLoadWatts, 6);
    }

    [Fact]
    public void NegativeLoadWhenOutdoorWarmerThanIndoor()
    {
        var input = CreateInput(
            topology: CreateTopology([CreateRoom("R1", volume: 100.0)]),
            openings: [CreateOpening("O1", roomId: "R1")],
            rules: [CreateRule("Rule-1", openingId: "O1", roomId: "R1")],
            environments: [CreateEnvironment(hour: 0, roomId: "R1", indoorTemperature: 20.0, outdoorTemperature: 30.0)]);

        var result = _calculator.Calculate(input);

        var zone = Assert.Single(result.HourlyZones);
        Assert.True(zone.SensibleVentilationLoadWatts < 0.0);
    }

    [Fact]
    public void ScheduledAirflowModeUsesPrescribedAirflowForHve()
    {
        var baseInput = CreateInput(
            topology: CreateTopology([CreateRoom("R1", volume: 100.0)]),
            openings: [CreateOpening("O1", roomId: "R1")],
            rules: [CreateRule("Rule-1", openingId: "O1", roomId: "R1")],
            environments:
            [
                CreateEnvironment(hour: 0, roomId: "R1", cp: 1000.0) with
                {
                    PrescribedAirflowCubicMetersPerSecond = 0.2
                }
            ]);

        var input = baseInput with { FlowConfiguration = NaturalVentilationFlowConfiguration.ScheduledAirflow };

        var result = _calculator.Calculate(input);

        var zone = Assert.Single(result.HourlyZones);
        Assert.Equal(0.2, zone.TotalAirflowCubicMetersPerSecond, 6);
        Assert.Equal(0.2 * 1.2 * 1000.0, zone.VentilationHeatTransferCoefficientWPerKelvin, 6);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-ZONE-PRESCRIBED-AIRFLOW-USED");
    }

    [Fact]
    public void ScheduledAirflowModeWithNoOperableOpeningsPreservesZeroFallbackDiagnostic()
    {
        var baseInput = CreateInput(
            topology: CreateTopology([CreateRoom("R1", volume: 100.0)]),
            openings: [CreateOpening("O1", roomId: "R1") with { IsOperable = false }],
            rules: [CreateRule("Rule-1", openingId: "O1", roomId: "R1")],
            environments:
            [
                CreateEnvironment(hour: 0, roomId: "R1") with
                {
                    PrescribedAirflowCubicMetersPerSecond = 0.2
                }
            ]);

        var input = baseInput with { FlowConfiguration = NaturalVentilationFlowConfiguration.ScheduledAirflow };

        var result = _calculator.Calculate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-ZONE-PRESCRIBED-AIRFLOW-NO-OPENINGS");
        var zone = Assert.Single(result.HourlyZones);
        Assert.Equal(0.0, zone.TotalAirflowCubicMetersPerSecond, 6);
    }

    [Fact]
    public void CalculatesHveFromMassFlowAndCp()
    {
        var input = CreateInput(
            topology: CreateTopology([CreateRoom("R1", volume: 100.0)]),
            openings: [CreateOpening("O1", roomId: "R1")],
            rules: [CreateRule("Rule-1", openingId: "O1", roomId: "R1")],
            environments: [CreateEnvironment(hour: 0, roomId: "R1", cp: 1000.0)]);

        var result = _calculator.Calculate(input);

        var opening = Assert.Single(result.HourlyZones.SelectMany(zone => zone.Rooms).SelectMany(room => room.Openings));
        Assert.NotNull(opening.VentilationHeatTransferCoefficientWPerKelvin);
        Assert.Equal(
            opening.AirflowKilogramsPerSecond * 1000.0,
            opening.VentilationHeatTransferCoefficientWPerKelvin!.Value,
            6);
    }

    [Fact]
    public void DefaultsCpWithDiagnostic()
    {
        var input = CreateInput(
            topology: CreateTopology([CreateRoom("R1", volume: 100.0)]),
            openings: [CreateOpening("O1", roomId: "R1")],
            rules: [CreateRule("Rule-1", openingId: "O1", roomId: "R1")],
            environments: [CreateEnvironment(hour: 0, roomId: "R1", cp: null)],
            defaultAirSpecificHeatJPerKgKelvin: null);

        var result = _calculator.Calculate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-ZONE-AIR-CP-DEFAULTED");
    }

    [Fact]
    public void DefaultsAirDensityWithDiagnostic()
    {
        var input = CreateInput(
            topology: CreateTopology([CreateRoom("R1", volume: 100.0)]),
            openings: [CreateOpening("O1", roomId: "R1")],
            rules: [CreateRule("Rule-1", openingId: "O1", roomId: "R1")],
            environments: [CreateEnvironment(hour: 0, roomId: "R1") with { AirDensityKgPerCubicMeter = null }],
            defaultAirDensityKgPerCubicMeter: null);

        var result = _calculator.Calculate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-ZONE-AIR-DENSITY-DEFAULTED");
    }

    [Fact]
    public void OpeningDiagnosticsOrderIsPreservedForCalculatedLoad()
    {
        var input = CreateInput(
            topology: CreateTopology([CreateRoom("R1", volume: 100.0)]),
            openings: [CreateOpening("O1", roomId: "R1")],
            rules: [CreateRule("Rule-1", openingId: "O1", roomId: "R1")],
            environments: [CreateEnvironment(hour: 0, roomId: "R1")]);

        var result = _calculator.Calculate(input);

        var opening = Assert.Single(result.HourlyZones.SelectMany(zone => zone.Rooms).SelectMany(room => room.Openings));
        var openingCodes = opening.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray();
        var hveIndex = Array.IndexOf(openingCodes, "AE-VENT-ZONE-HVE-CALCULATED");
        var loadIndex = Array.IndexOf(openingCodes, "AE-VENT-ZONE-SENSIBLE-LOAD-CALCULATED");

        Assert.True(hveIndex >= 0);
        Assert.True(loadIndex >= 0);
        Assert.True(hveIndex < loadIndex);
    }

    [Fact]
    public void CalculatesAchFromRoomVolume()
    {
        var input = CreateInput(
            topology: CreateTopology([CreateRoom("R1", volume: 100.0)]),
            openings: [CreateOpening("O1", roomId: "R1")],
            rules: [CreateRule("Rule-1", openingId: "O1", roomId: "R1")],
            environments: [CreateEnvironment(hour: 0, roomId: "R1")]);

        var result = _calculator.Calculate(input);

        var room = Assert.Single(result.HourlyZones.SelectMany(zone => zone.Rooms));
        Assert.NotNull(room.AirChangesPerHour);
        Assert.Equal(room.TotalAirflowCubicMetersPerHour / 100.0, room.AirChangesPerHour!.Value, 6);
    }

    [Fact]
    public void MissingVolumeProducesAchDiagnostic()
    {
        var input = CreateInput(
            topology: CreateTopology([CreateRoom("R1", volume: null)]),
            openings: [CreateOpening("O1", roomId: "R1")],
            rules: [CreateRule("Rule-1", openingId: "O1", roomId: "R1")],
            environments: [CreateEnvironment(hour: 0, roomId: "R1")]);

        var result = _calculator.Calculate(input);

        var room = Assert.Single(result.HourlyZones.SelectMany(zone => zone.Rooms));
        Assert.Null(room.AirChangesPerHour);
        Assert.Contains(room.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-ZONE-VOLUME-MISSING");
    }

    [Fact]
    public void AggregatesMultipleRoomsIntoZone()
    {
        var input = CreateInput(
            topology: CreateTopology([CreateRoom("R1", volume: 100.0), CreateRoom("R2", volume: 100.0)]),
            openings:
            [
                CreateOpening("O1", roomId: "R1"),
                CreateOpening("O2", roomId: "R2")
            ],
            rules:
            [
                CreateRule("Rule-1", openingId: "O1", roomId: "R1"),
                CreateRule("Rule-2", openingId: "O2", roomId: "R2")
            ],
            environments:
            [
                CreateEnvironment(hour: 0, roomId: "R1"),
                CreateEnvironment(hour: 0, roomId: "R2")
            ]);

        var result = _calculator.Calculate(input);

        var zone = Assert.Single(result.HourlyZones);
        var rooms = zone.Rooms;
        Assert.Equal(2, rooms.Count);

        Assert.Equal(rooms.Sum(room => room.TotalAirflowCubicMetersPerSecond), zone.TotalAirflowCubicMetersPerSecond, 6);
        Assert.Equal(rooms.Sum(room => room.VentilationHeatTransferCoefficientWPerKelvin), zone.VentilationHeatTransferCoefficientWPerKelvin, 6);
        Assert.Equal(rooms.Sum(room => room.SensibleVentilationLoadWatts), zone.SensibleVentilationLoadWatts, 6);
    }

    [Fact]
    public void BuildsZoneProfilesFor24Hours()
    {
        var environments = Enumerable.Range(0, 24)
            .Select(hour => CreateEnvironment(hour, roomId: "R1"))
            .ToArray();

        var input = CreateInput(
            topology: CreateTopology([CreateRoom("R1", volume: 100.0)]),
            openings: [CreateOpening("O1", roomId: "R1")],
            rules: [CreateRule("Rule-1", openingId: "O1", roomId: "R1")],
            environments: environments);

        var result = _calculator.Calculate(input);

        Assert.Equal(24, result.ZoneAirflowCubicMetersPerHourProfiles["Z1"].Count);
        Assert.Equal(24, result.ZoneVentilationHeatTransferCoefficientProfilesWPerKelvin["Z1"].Count);
        Assert.Equal(24, result.ZoneSensibleVentilationLoadProfilesWatts["Z1"].Count);
        Assert.Equal(24, result.ZoneAirChangesPerHourProfiles["Z1"].Count);
    }

    [Fact]
    public void BuildsZoneProfilesFor8760Hours()
    {
        var environments = Enumerable.Range(0, 8760)
            .Select(hour => CreateEnvironment(hour, roomId: "R1"))
            .ToArray();

        var input = CreateInput(
            topology: CreateTopology([CreateRoom("R1", volume: 100.0)]),
            openings: [CreateOpening("O1", roomId: "R1")],
            rules: [CreateRule("Rule-1", openingId: "O1", roomId: "R1")],
            environments: environments);

        var result = _calculator.Calculate(input);

        Assert.Equal(8760, result.ZoneAirflowCubicMetersPerHourProfiles["Z1"].Count);
        Assert.Equal(8760, result.ZoneVentilationHeatTransferCoefficientProfilesWPerKelvin["Z1"].Count);
        Assert.Equal(8760, result.ZoneSensibleVentilationLoadProfilesWatts["Z1"].Count);
        Assert.Equal(8760, result.ZoneAirChangesPerHourProfiles["Z1"].Count);
    }

    [Fact]
    public void NonstandardProfileLengthProducesDiagnostic()
    {
        var environments = Enumerable.Range(0, 5)
            .Select(hour => CreateEnvironment(hour, roomId: "R1"))
            .ToArray();

        var input = CreateInput(
            topology: CreateTopology([CreateRoom("R1", volume: 100.0)]),
            openings: [CreateOpening("O1", roomId: "R1")],
            rules: [CreateRule("Rule-1", openingId: "O1", roomId: "R1")],
            environments: environments);

        var result = _calculator.Calculate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-ZONE-PROFILE-LENGTH-NONSTANDARD");
    }

    [Fact]
    public void DisclosureKeepsForbiddenClaims()
    {
        var disclosureOverride = new StandardCalculationDisclosure(
            Family: StandardCalculationFamily.EN16798,
            Stage: StandardCalculationStage.Ventilation,
            Mode: StandardCalculationMode.StandardInspired,
            CalculationPath: "UnitTest/VentZoneOverride",
            IsFallback: false,
            UsesExternalValidation: false,
            ClaimBoundary: new StandardClaimBoundary(
                AllowedClaims:
                [
                    "safe claim",
                    "Full EN compliance",
                    "prefix Full ISO compliance suffix"
                ],
                ForbiddenClaims: [],
                Limitations: ["Unit test"],
                Assumptions: ["Unit test"]),
            Diagnostics: []);

        var input = CreateInput(
            topology: CreateTopology([CreateRoom("R1", volume: 100.0)]),
            openings: [CreateOpening("O1", roomId: "R1")],
            rules: [CreateRule("Rule-1", openingId: "O1", roomId: "R1")],
            environments: [CreateEnvironment(hour: 0, roomId: "R1")],
            disclosureOverride: disclosureOverride);

        var result = _calculator.Calculate(input);

        foreach (var forbiddenClaim in RequiredForbiddenClaims)
        {
            Assert.Contains(forbiddenClaim, result.Disclosure.ClaimBoundary.ForbiddenClaims, StringComparer.Ordinal);
            Assert.DoesNotContain(
                result.Disclosure.ClaimBoundary.AllowedClaims,
                claim => claim.Contains(forbiddenClaim, StringComparison.Ordinal));
        }
    }

    private static NaturalVentilationZoneIntegrationInput CreateInput(
        BuildingThermalTopology topology,
        IReadOnlyList<NaturalVentilationOpeningGeometry> openings,
        IReadOnlyList<NaturalVentilationOpeningControlRule> rules,
        IReadOnlyList<NaturalVentilationHourlyZoneEnvironment> environments,
        double? defaultAirDensityKgPerCubicMeter = 1.2,
        double? defaultAirSpecificHeatJPerKgKelvin = 1005.0,
        StandardCalculationDisclosure? disclosureOverride = null) =>
        new(
            CalculationId: "VENT-ZONE-LOAD-1",
            Topology: topology,
            Openings: openings,
            ControlRules: rules,
            HourlyEnvironments: environments,
            FlowConfiguration: NaturalVentilationFlowConfiguration.WindOnly,
            DefaultAirDensityKgPerCubicMeter: defaultAirDensityKgPerCubicMeter,
            DefaultAirSpecificHeatJPerKgKelvin: defaultAirSpecificHeatJPerKgKelvin,
            DisclosureOverride: disclosureOverride,
            Source: "UnitTest",
            StrictBoundaryValidation: false);

    private static BuildingThermalTopology CreateTopology(
        IReadOnlyList<ThermalTopologyRoom> rooms)
    {
        var roomIds = rooms.Select(room => room.RoomId).ToArray();

        return new BuildingThermalTopology(
            BuildingId: "B1",
            Zones: [new ThermalTopologyZone("Z1", "Zone 1", roomIds, [])],
            Rooms: rooms,
            Surfaces: [],
            Disclosure: new StandardCalculationDisclosureFactory().CreateThermalZonesDisclosure(),
            Diagnostics: []);
    }

    private static ThermalTopologyRoom CreateRoom(
        string roomId,
        double? volume) =>
        new(
            RoomId: roomId,
            ZoneId: "Z1",
            VolumeCubicMeters: volume,
            FloorAreaSquareMeters: 40.0,
            Surfaces: [],
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
        string roomId,
        NaturalVentilationControlMode mode = NaturalVentilationControlMode.AlwaysOpen) =>
        new(
            RuleId: ruleId,
            OpeningId: openingId,
            RoomId: roomId,
            ZoneId: "Z1",
            ControlMode: mode,
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
        string roomId,
        double indoorTemperature = 22.0,
        double outdoorTemperature = 12.0,
        double windSpeed = 3.0,
        double? cp = 1005.0) =>
        new(
            HourIndex: hour,
            RoomId: roomId,
            ZoneId: "Z1",
            IndoorTemperatureCelsius: indoorTemperature,
            OutdoorTemperatureCelsius: outdoorTemperature,
            WindSpeedMetersPerSecond: windSpeed,
            OccupancyFraction: 1.0,
            ScheduleFraction: 1.0,
            IsNightHour: false,
            AirDensityKgPerCubicMeter: 1.2,
            AirSpecificHeatJPerKgKelvin: cp,
            Source: "UnitTest",
            Diagnostics: []);
}
