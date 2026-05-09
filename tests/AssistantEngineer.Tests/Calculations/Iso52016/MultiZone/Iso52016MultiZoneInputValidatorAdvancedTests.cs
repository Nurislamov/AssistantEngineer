using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

namespace AssistantEngineer.Tests.Calculations.Iso52016.MultiZone;

public sealed class Iso52016MultiZoneInputValidatorAdvancedTests
{
    private readonly Iso52016MultiZoneInputValidator _validator = new();

    [Fact]
    public void RejectsExteriorBoundaryWithAdjacentZoneId()
    {
        var input = CreateValidInput() with
        {
            BoundaryLinks =
            [
                new ThermalZoneBoundaryLink(
                    LinkId: "LINK-EXT",
                    BoundaryType: MultiZoneBoundaryLinkType.ExternalBoundary,
                    SourceZoneId: "ZONE-A",
                    SourceBoundaryId: "A-OUT",
                    AreaSquareMeters: 10.0,
                    ConductanceWPerK: 20.0,
                    TargetZoneId: "ZONE-B")
            ]
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, item => item.Code == "Iso52016.MultiZone.InputValidator.ExteriorBoundaryTargetZoneForbidden");
    }

    [Fact]
    public void RejectsAdjacentBoundaryWithoutTargetZone()
    {
        var input = CreateValidInput() with
        {
            BoundaryLinks =
            [
                new ThermalZoneBoundaryLink(
                    LinkId: "LINK-INT",
                    BoundaryType: MultiZoneBoundaryLinkType.InterZoneBoundary,
                    SourceZoneId: "ZONE-A",
                    SourceBoundaryId: "A-INT",
                    AreaSquareMeters: 10.0,
                    ConductanceWPerK: 20.0,
                    TargetZoneId: null)
            ]
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, item => item.Code == "Iso52016.MultiZone.InputValidator.AdjacentBoundaryTargetZoneMissing");
    }

    [Fact]
    public void RejectsGroundBoundaryWithoutGroundTemperatureProfile()
    {
        var input = CreateValidInput() with
        {
            BoundaryLinks =
            [
                new ThermalZoneBoundaryLink(
                    LinkId: "LINK-GROUND",
                    BoundaryType: MultiZoneBoundaryLinkType.GroundBoundary,
                    SourceZoneId: "ZONE-A",
                    SourceBoundaryId: "A-GRD",
                    AreaSquareMeters: 10.0,
                    ConductanceWPerK: 20.0)
            ],
            HourlyBoundaryConditions = []
        };

        var result = _validator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, item => item.Code == "Iso52016.MultiZone.InputValidator.GroundBoundaryTemperatureMissing");
    }

    [Fact]
    public void ValidationDiagnostics_AreDeterministicAndSorted()
    {
        var invalid = CreateValidInput() with
        {
            BoundaryLinks =
            [
                new ThermalZoneBoundaryLink(
                    LinkId: "LINK-1",
                    BoundaryType: MultiZoneBoundaryLinkType.ExternalBoundary,
                    SourceZoneId: "ZONE-A",
                    SourceBoundaryId: "A-OUT",
                    AreaSquareMeters: 0.0,
                    ConductanceWPerK: 0.0)
            ]
        };

        var first = _validator.Validate(invalid).Diagnostics.Select(item => item.Code).ToArray();
        var second = _validator.Validate(invalid).Diagnostics.Select(item => item.Code).ToArray();

        Assert.Equal(first, second);
    }

    private static MultiZoneCalculationInput CreateValidInput() =>
        new(
            BuildingId: "BLD-VALID",
            Zones:
            [
                new ThermalZoneNode("ZONE-A", "Zone A", 20.0, 50.0, ["A-OUT"]),
                new ThermalZoneNode("ZONE-B", "Zone B", 20.0, 50.0, ["B-OUT"])
            ],
            BoundaryLinks:
            [
                new ThermalZoneBoundaryLink("LINK-A-OUT", MultiZoneBoundaryLinkType.ExternalBoundary, "ZONE-A", "A-OUT", 10.0, 20.0),
                new ThermalZoneBoundaryLink("LINK-B-OUT", MultiZoneBoundaryLinkType.ExternalBoundary, "ZONE-B", "B-OUT", 10.0, 20.0)
            ],
            InterZoneConductanceLinks: [],
            InterZoneAirflowLinks: [],
            HourlyBoundaryConditions:
            [
                new MultiZoneHourlyBoundaryCondition("A-OUT", [0.0]),
                new MultiZoneHourlyBoundaryCondition("B-OUT", [0.0])
            ],
            ZoneHourlyProfiles:
            [
                new MultiZoneZoneHourlyProfile("ZONE-A", 20.0, 800_000.0, [21.0], [26.0], [0.0], [0.0], [0.0]),
                new MultiZoneZoneHourlyProfile("ZONE-B", 20.0, 800_000.0, [21.0], [26.0], [0.0], [0.0], [0.0])
            ],
            ClaimFlags:
            [
                "validation anchor",
                "internal engineering anchor",
                "standard-based calculation",
                "not full validation"
            ]);
}
