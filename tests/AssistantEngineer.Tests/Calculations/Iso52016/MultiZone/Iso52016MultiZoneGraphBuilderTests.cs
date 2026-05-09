using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

namespace AssistantEngineer.Tests.Calculations.Iso52016.MultiZone;

public sealed class Iso52016MultiZoneGraphBuilderTests
{
    private readonly Iso52016MultiZoneInputValidator _validator = new();

    [Fact]
    public void BuildGraph_CreatesTwoZoneGraph()
    {
        var builder = new Iso52016MultiZoneGraphBuilder(_validator);
        var input = CreateValidTwoZoneInput();

        var result = builder.BuildGraph(input);

        Assert.True(result.IsValid);
        Assert.Equal(2, result.Zones.Count);
        Assert.NotEmpty(result.BoundaryLinks);
        Assert.NotEmpty(result.InterZoneConductanceLinks);
        Assert.Empty(result.HourlyResults);
    }

    [Fact]
    public void BuildGraph_RejectsDuplicateZoneIds()
    {
        var builder = new Iso52016MultiZoneGraphBuilder(_validator);
        var input = CreateValidTwoZoneInput() with
        {
            Zones =
            [
                new ThermalZoneNode("ZONE-A", "Zone A", 50.0, 140.0, ["A-OUT-1"]),
                new ThermalZoneNode("ZONE-A", "Zone A duplicate", 45.0, 120.0, ["B-OUT-1"])
            ]
        };

        var result = builder.BuildGraph(input);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Code == "Iso52016.MultiZone.InputValidator.DuplicateZoneId" &&
                diagnostic.Severity == CalculationDiagnosticSeverity.Error);
    }

    [Fact]
    public void BuildGraph_RejectsBoundaryReferencingMissingZone()
    {
        var builder = new Iso52016MultiZoneGraphBuilder(_validator);
        var input = CreateValidTwoZoneInput() with
        {
            BoundaryLinks =
            [
                new ThermalZoneBoundaryLink(
                    LinkId: "LINK-MISSING-ZONE",
                    BoundaryType: MultiZoneBoundaryLinkType.ExternalBoundary,
                    SourceZoneId: "ZONE-NOT-FOUND",
                    SourceBoundaryId: "A-OUT-1",
                    AreaSquareMeters: 12.0,
                    ConductanceWPerK: 4.0)
            ]
        };

        var result = builder.BuildGraph(input);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Code == "Iso52016.MultiZone.InputValidator.BoundarySourceZoneMissing");
    }

    [Fact]
    public void BuildGraph_RejectsSelfLinkedInterZoneBoundary()
    {
        var builder = new Iso52016MultiZoneGraphBuilder(_validator);
        var input = CreateValidTwoZoneInput() with
        {
            BoundaryLinks =
            [
                new ThermalZoneBoundaryLink(
                    LinkId: "LINK-SELF",
                    BoundaryType: MultiZoneBoundaryLinkType.InterZoneBoundary,
                    SourceZoneId: "ZONE-A",
                    SourceBoundaryId: "A-INT-1",
                    AreaSquareMeters: 8.0,
                    ConductanceWPerK: 2.4,
                    TargetZoneId: "ZONE-A")
            ]
        };

        var result = builder.BuildGraph(input);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Code == "Iso52016.MultiZone.InputValidator.InterZoneBoundarySelfLink");
    }

    [Fact]
    public void BuildGraph_SupportsAdjacentUnconditionedBoundary()
    {
        var builder = new Iso52016MultiZoneGraphBuilder(_validator);
        var input = CreateValidTwoZoneInput() with
        {
            BoundaryLinks =
            [
                new ThermalZoneBoundaryLink(
                    LinkId: "LINK-ADJ-UNCOND",
                    BoundaryType: MultiZoneBoundaryLinkType.AdjacentUnconditionedZone,
                    SourceZoneId: "ZONE-A",
                    SourceBoundaryId: "A-UNCOND-1",
                    AreaSquareMeters: 10.0,
                    ConductanceWPerK: 1.5,
                    AdjacentBoundaryCondition: new AdjacentZoneBoundaryCondition(
                        ConditionId: "UNCOND-STUB",
                        TemperatureProfileCelsius: [12.0]))
            ]
        };

        var result = builder.BuildGraph(input);

        Assert.True(result.IsValid);
        Assert.Contains(
            result.BoundaryLinks,
            link => link.BoundaryType == MultiZoneBoundaryLinkType.AdjacentUnconditionedZone);
    }

    [Fact]
    public void BuildGraph_SupportsSameUseAdiabaticBoundary()
    {
        var builder = new Iso52016MultiZoneGraphBuilder(_validator);
        var input = CreateValidTwoZoneInput() with
        {
            BoundaryLinks =
            [
                new ThermalZoneBoundaryLink(
                    LinkId: "LINK-SAME-USE-ADIABATIC",
                    BoundaryType: MultiZoneBoundaryLinkType.AdjacentConditionedSameUseZone,
                    SourceZoneId: "ZONE-B",
                    SourceBoundaryId: "B-INT-1",
                    AreaSquareMeters: 9.0,
                    ConductanceWPerK: 0.0,
                    TargetZoneId: "ZONE-A",
                    AdjacentBoundaryCondition: new AdjacentZoneBoundaryCondition(
                        ConditionId: "SAME-USE-ADIABATIC",
                        TemperatureProfileCelsius: [21.0],
                        IsAdiabaticEquivalent: true,
                        Notes: "Internal engineering anchor for same-use adiabatic-style boundary."))
            ]
        };

        var result = builder.BuildGraph(input);

        Assert.True(result.IsValid);
        Assert.Contains(
            result.BoundaryLinks,
            link => link.BoundaryType == MultiZoneBoundaryLinkType.AdjacentConditionedSameUseZone &&
                link.AdjacentBoundaryCondition?.IsAdiabaticEquivalent == true);
    }

    private static MultiZoneCalculationInput CreateValidTwoZoneInput() =>
        new(
            BuildingId: "BLD-MZ-01",
            Zones:
            [
                new ThermalZoneNode(
                    ZoneId: "ZONE-A",
                    Name: "Zone A",
                    FloorAreaSquareMeters: 50.0,
                    VolumeCubicMeters: 140.0,
                    BoundaryIds: ["A-OUT-1", "A-INT-1", "A-UNCOND-1"]),
                new ThermalZoneNode(
                    ZoneId: "ZONE-B",
                    Name: "Zone B",
                    FloorAreaSquareMeters: 45.0,
                    VolumeCubicMeters: 120.0,
                    BoundaryIds: ["B-OUT-1", "B-INT-1"])
            ],
            BoundaryLinks:
            [
                new ThermalZoneBoundaryLink(
                    LinkId: "LINK-A-OUT",
                    BoundaryType: MultiZoneBoundaryLinkType.ExternalBoundary,
                    SourceZoneId: "ZONE-A",
                    SourceBoundaryId: "A-OUT-1",
                    AreaSquareMeters: 12.0,
                    ConductanceWPerK: 4.0),
                new ThermalZoneBoundaryLink(
                    LinkId: "LINK-B-OUT",
                    BoundaryType: MultiZoneBoundaryLinkType.ExternalBoundary,
                    SourceZoneId: "ZONE-B",
                    SourceBoundaryId: "B-OUT-1",
                    AreaSquareMeters: 10.0,
                    ConductanceWPerK: 3.5),
                new ThermalZoneBoundaryLink(
                    LinkId: "LINK-A-B",
                    BoundaryType: MultiZoneBoundaryLinkType.InterZoneBoundary,
                    SourceZoneId: "ZONE-A",
                    SourceBoundaryId: "A-INT-1",
                    AreaSquareMeters: 8.0,
                    ConductanceWPerK: 2.4,
                    TargetZoneId: "ZONE-B")
            ],
            InterZoneConductanceLinks:
            [
                new InterZoneConductanceLink(
                    LinkId: "COND-A-B",
                    FromZoneId: "ZONE-A",
                    ToZoneId: "ZONE-B",
                    ConductanceWPerK: 2.4,
                    AreaSquareMeters: 8.0,
                    FromBoundaryId: "A-INT-1",
                    ToBoundaryId: "B-INT-1")
            ],
            InterZoneAirflowLinks: [],
            HourlyBoundaryConditions:
            [
                new MultiZoneHourlyBoundaryCondition(
                    BoundaryId: "A-OUT-1",
                    TemperatureProfileCelsius: [5.0]),
                new MultiZoneHourlyBoundaryCondition(
                    BoundaryId: "B-OUT-1",
                    TemperatureProfileCelsius: [5.0])
            ],
            ZoneHourlyProfiles:
            [
                new MultiZoneZoneHourlyProfile(
                    ZoneId: "ZONE-A",
                    InitialTemperatureCelsius: 20.0,
                    ThermalCapacityJPerK: 1_200_000.0,
                    HeatingSetpointProfileCelsius: [21.0],
                    CoolingSetpointProfileCelsius: [26.0],
                    InternalGainsProfileW: [200.0],
                    SolarGainsProfileW: [100.0],
                    VentilationInfiltrationConductanceProfileWPerK: [15.0]),
                new MultiZoneZoneHourlyProfile(
                    ZoneId: "ZONE-B",
                    InitialTemperatureCelsius: 20.0,
                    ThermalCapacityJPerK: 1_100_000.0,
                    HeatingSetpointProfileCelsius: [21.0],
                    CoolingSetpointProfileCelsius: [26.0],
                    InternalGainsProfileW: [180.0],
                    SolarGainsProfileW: [90.0],
                    VentilationInfiltrationConductanceProfileWPerK: [12.0])
            ],
            ClaimFlags:
            [
                "validation anchor",
                "internal engineering anchor",
                "standard-based calculation",
                "not full validation"
            ]);
}
