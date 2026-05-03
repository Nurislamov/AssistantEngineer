using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Modules.Calculations.Application.Services.Performance;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class BuildingEnergyAnalysisFacadeIso52016SimulationTests
{
    [Fact]
    public async Task SimulateIso52016Async_MapsApiCommandToApplicationRequest()
    {
        var simulationService = new Iso52016SimulationApplicationServiceSpy();

        var facade = new BuildingEnergyAnalysisFacade(
            performance: null!,
            iso52016Simulation: simulationService);

        var result = await facade.SimulateIso52016Async(
            buildingId: 42,
            request: new Iso52016BuildingEnergySimulationCommand(
                LatitudeDegrees: 41.3,
                LongitudeDegrees: 69.2,
                TimeZoneOffset: TimeSpan.FromHours(5),
                WeatherYear: 2026,
                HeatBalanceOptions: new Iso52016RoomHeatBalanceOptions(
                    InitialIndoorTemperatureC: 22)),
            cancellationToken: CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(simulationService.LastRequest);

        var request = simulationService.LastRequest!;

        Assert.Equal(42, request.BuildingId);
        Assert.Equal(41.3, request.LatitudeDegrees, precision: 6);
        Assert.Equal(69.2, request.LongitudeDegrees, precision: 6);
        Assert.Equal(TimeSpan.FromHours(5), request.TimeZoneOffset);
        Assert.Equal(2026, request.WeatherYear);

        Assert.NotNull(request.HeatBalanceOptions);
        Assert.Equal(22.0, request.HeatBalanceOptions!.InitialIndoorTemperatureC, precision: 6);
    }

    private sealed class Iso52016SimulationApplicationServiceSpy : IIso52016BuildingEnergySimulationApplicationService
    {
        public Iso52016BuildingEnergySimulationApplicationRequest? LastRequest { get; private set; }

        public Task<Result<Iso52016BuildingEnergySimulationApplicationResult>> SimulateAsync(
            Iso52016BuildingEnergySimulationApplicationRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;

            return Task.FromResult(
                Result<Iso52016BuildingEnergySimulationApplicationResult>.Validation(
                    "stub"));
        }
    }
}