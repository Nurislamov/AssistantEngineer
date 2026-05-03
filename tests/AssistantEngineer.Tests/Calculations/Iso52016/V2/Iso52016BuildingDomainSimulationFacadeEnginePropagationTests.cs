using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests.Calculations.Iso52016.V2;

public class Iso52016BuildingDomainSimulationFacadeEnginePropagationTests
{
    [Fact]
    public void Simulate_PassesRequestedSimulationEngineToBuildingFacade()
    {
        var buildingFacade = new BuildingSimulationFacadeSpy();

        var facade = new Iso52016BuildingDomainSimulationFacade(
            new Iso52016BuildingRoomCollector(),
            buildingFacade);

        var building = CreateBuildingWithRoom();

        var result = facade.Simulate(
            new Iso52016BuildingDomainSimulationFacadeRequest(
                Building: building,
                AnnualClimateData: CreateAnnualClimateData(),
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5),
                SimulationEngine: Iso52016SimulationEngine.V2Matrix));

        Assert.True(result.IsSuccess, result.Error);
        Assert.NotNull(buildingFacade.LastRequest);
        Assert.Equal(Iso52016SimulationEngine.V2Matrix, buildingFacade.LastRequest!.SimulationEngine);
        Assert.Equal(Iso52016SimulationEngine.V2Matrix, result.Value.SimulationEngine);
    }

    private static Building CreateBuildingWithRoom()
    {
        var project = Project.Create("Test project").Value;
        var building = Building.Create("Building", project).Value;
        var floor = building.AddFloor("Floor 1").Value;

        var room = floor.AddRoom(
            "Room 1",
            Area.FromSquareMeters(20).Value,
            3,
            Temperature.FromCelsius(20).Value,
            peopleCount: 1,
            equipmentLoad: Power.FromWatts(100).Value,
            lightingLoad: Power.FromWatts(100).Value,
            type: RoomType.Office).Value;

        Assert.True(room.AddWall(
            Area.FromSquareMeters(10).Value,
            ThermalTransmittance.FromValue(0.4).Value,
            CardinalDirection.South,
            WallBoundaryType.External).IsSuccess);

        return building;
    }

    private static AnnualClimateData CreateAnnualClimateData()
    {
        var summer = Temperature.FromCelsius(35).Value;
        var winter = Temperature.FromCelsius(-5).Value;

        var climateZone = ClimateZone.Create(
            "Test climate zone",
            summer,
            winter).Value;

        return AnnualClimateData.Create(
            climateZone,
            year: 2026).Value;
    }

    private sealed class BuildingSimulationFacadeSpy : IIso52016BuildingSimulationFacade
    {
        public Iso52016BuildingSimulationFacadeRequest? LastRequest { get; private set; }

        public Result<Iso52016BuildingSimulationFacadeResult> Simulate(
            Iso52016BuildingSimulationFacadeRequest request)
        {
            LastRequest = request;

            return Result<Iso52016BuildingSimulationFacadeResult>.Success(
                new Iso52016BuildingSimulationFacadeResult(
                    BuildingCode: request.BuildingCode,
                    WeatherSolarContext: new Iso52016WeatherSolarContext(
                        Year: 2026,
                        TimeZoneOffset: TimeSpan.Zero,
                        LatitudeDegrees: request.LatitudeDegrees,
                        LongitudeDegrees: request.LongitudeDegrees,
                        Hours: []),
                    RoomResults: [],
                    Hours: [],
                    MonthlySummaries: [],
                    SimulationEngine: request.SimulationEngine));
        }
    }
}