using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;

namespace AssistantEngineer.Tests.Calculations.Iso52016.MultiZone;

public sealed class Iso52016MultiZoneGroundBoundaryTests
{
    [Fact]
    public void GroundBoundaryUsesGroundTemperatureLane()
    {
        var service = CreateService();
        var coldGround = CreateInput(groundTemperature: 0.0);
        var warmGround = CreateInput(groundTemperature: 10.0);

        var coldResult = service.Simulate(coldGround);
        var warmResult = service.Simulate(warmGround);

        Assert.True(coldResult.IsValid);
        Assert.True(warmResult.IsValid);

        var coldHeating = coldResult.HourlyResults[0].HeatingLoadsByZoneW["ZONE-A"];
        var warmHeating = warmResult.HourlyResults[0].HeatingLoadsByZoneW["ZONE-A"];

        Assert.True(warmHeating < coldHeating);
    }

    [Fact]
    public void GroundBoundaryDiffersFromExteriorBoundaryWithSameConductance()
    {
        var service = CreateService();
        var withGround = CreateInput(groundTemperature: 10.0);
        var withExterior = new MultiZoneCalculationInput(
            BuildingId: "BLD-GROUND",
            Zones:
            [
                new ThermalZoneNode("ZONE-A", "Zone A", 20.0, 50.0, ["A-OUT"])
            ],
            BoundaryLinks:
            [
                new ThermalZoneBoundaryLink(
                    LinkId: "A-OUT-LINK",
                    BoundaryType: MultiZoneBoundaryLinkType.ExternalBoundary,
                    SourceZoneId: "ZONE-A",
                    SourceBoundaryId: "A-OUT",
                    AreaSquareMeters: 20.0,
                    ConductanceWPerK: 30.0)
            ],
            InterZoneConductanceLinks: [],
            InterZoneAirflowLinks: [],
            HourlyBoundaryConditions:
            [
                new MultiZoneHourlyBoundaryCondition("A-OUT", [0.0])
            ],
            ZoneHourlyProfiles: withGround.ZoneHourlyProfiles,
            ClaimFlags: withGround.ClaimFlags);

        var groundResult = service.Simulate(withGround);
        var exteriorResult = service.Simulate(withExterior);

        Assert.True(groundResult.IsValid);
        Assert.True(exteriorResult.IsValid);
        Assert.NotEqual(
            groundResult.HourlyResults[0].HeatingLoadsByZoneW["ZONE-A"],
            exteriorResult.HourlyResults[0].HeatingLoadsByZoneW["ZONE-A"]);
    }

    [Fact]
    public void GroundBoundaryDoesNotCreateExteriorBoundaryPath()
    {
        var service = CreateService();
        var input = CreateInput(groundTemperature: 10.0);

        var result = service.Simulate(input);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(
            result.Diagnostics,
            item => item.Code == "Iso52016.MultiZone.HourlySolver.ExternalBoundaryTemperatureMissing");
    }

    private static Iso52016MultiZoneEnergySimulationService CreateService()
    {
        var validator = new Iso52016MultiZoneInputValidator();
        var graphBuilder = new Iso52016MultiZoneGraphBuilder(validator);
        var solver = new Iso52016MultiZoneHourlySolver();
        return new Iso52016MultiZoneEnergySimulationService(validator, graphBuilder, solver);
    }

    private static MultiZoneCalculationInput CreateInput(double groundTemperature) =>
        new(
            BuildingId: "BLD-GROUND",
            Zones:
            [
                new ThermalZoneNode("ZONE-A", "Zone A", 20.0, 50.0, ["A-GRD"])
            ],
            BoundaryLinks:
            [
                new ThermalZoneBoundaryLink(
                    LinkId: "A-GRD-LINK",
                    BoundaryType: MultiZoneBoundaryLinkType.GroundBoundary,
                    SourceZoneId: "ZONE-A",
                    SourceBoundaryId: "A-GRD",
                    AreaSquareMeters: 20.0,
                    ConductanceWPerK: 30.0)
            ],
            InterZoneConductanceLinks: [],
            InterZoneAirflowLinks: [],
            HourlyBoundaryConditions:
            [
                new MultiZoneHourlyBoundaryCondition("A-GRD", [groundTemperature])
            ],
            ZoneHourlyProfiles:
            [
                new MultiZoneZoneHourlyProfile("ZONE-A", 20.0, 1_000_000.0, [21.0], [26.0], [0.0], [0.0], [0.0])
            ],
            ClaimFlags:
            [
                "validation anchor",
                "internal engineering anchor",
                "standard-based calculation",
                "not full validation"
            ]);
}
