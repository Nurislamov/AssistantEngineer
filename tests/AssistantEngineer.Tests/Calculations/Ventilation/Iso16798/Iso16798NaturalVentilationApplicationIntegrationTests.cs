using AssistantEngineer.Modules.Buildings.Domain.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Models.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation.Iso16798;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Calculations.Ventilation.Iso16798;

public sealed class Iso16798NaturalVentilationApplicationIntegrationTests
{
    [Fact]
    public void DefaultOption_UsesCompatibilityBehavior()
    {
        var opening = new NaturalVentilationOpeningState(
            IsOpen: true,
            OpeningFactor: 0.8,
            EffectiveOpeningAreaM2: 1.2,
            Reason: "Test opening");

        var room = CreateRoomWithVentilationParameters(
            stackCoefficient: 0.45,
            windCoefficient: 0.35,
            windExposureFactor: 1.4);

        var options = new NaturalVentilationOptions
        {
            Enabled = true,
            UseIso16798InspiredCalculator = false,
            OpeningDischargeCoefficient = 0.6,
            MaximumAirChangesPerHour = 12.0
        };

        var service = CreateService(options, opening);
        var result = service.CalculateHeatTransferCoefficient(
            room,
            indoorTemperatureC: 25.0,
            outdoorTemperatureC: 20.0,
            windSpeedMPerS: 4.0,
            demandFactor: 1.0,
            hourOfDay: 13);

        var expected = CalculateCompatibilityExpected(
            roomVolumeM3: room.CalculateVolume(),
            effectiveOpeningAreaM2: opening.EffectiveOpeningAreaM2,
            indoorTemperatureC: 25.0,
            outdoorTemperatureC: 20.0,
            windSpeedMPerS: 4.0,
            dischargeCoefficient: 0.6,
            stackCoefficient: 0.45,
            windCoefficient: 0.35,
            windExposureFactor: 1.4,
            maximumAirChangesPerHour: 12.0);

        Assert.Equal(expected, result, precision: 6);
    }

    [Fact]
    public void OptInOption_UsesIso16798InspiredCalculatorPath()
    {
        var opening = new NaturalVentilationOpeningState(
            IsOpen: true,
            OpeningFactor: 0.7,
            EffectiveOpeningAreaM2: 1.0,
            Reason: "Test opening");

        var room = CreateRoomWithVentilationParameters(
            stackCoefficient: 0.8,
            windCoefficient: 1.1,
            windExposureFactor: 1.3);

        var options = new NaturalVentilationOptions
        {
            Enabled = true,
            UseIso16798InspiredCalculator = true,
            OpeningDischargeCoefficient = 0.62,
            MaximumAirChangesPerHour = 10.0
        };

        var calculator = new Iso16798NaturalVentilationCalculator();
        var adapter = new Iso16798NaturalVentilationApplicationAdapter();
        var service = CreateService(options, opening, calculator, adapter);

        var serviceResult = service.CalculateHeatTransferCoefficient(
            room,
            indoorTemperatureC: 26.0,
            outdoorTemperatureC: 19.0,
            windSpeedMPerS: 5.0,
            demandFactor: 1.0,
            hourOfDay: 14);

        var input = adapter.BuildInput(
            room,
            opening,
            options,
            indoorTemperatureC: 26.0,
            outdoorTemperatureC: 19.0,
            windSpeedMPerS: 5.0);
        var expected = calculator.Calculate(input).HeatTransferCoefficientWPerK;

        Assert.Equal(expected, serviceResult, precision: 6);
    }

    [Fact]
    public void DisabledNaturalVentilation_ReturnsZero()
    {
        var room = CreateRoomWithVentilationParameters(0.3, 0.4, 1.1);
        var options = new NaturalVentilationOptions
        {
            Enabled = false
        };

        var service = CreateService(options, new NaturalVentilationOpeningState(true, 1.0, 1.0, "Open"));
        var result = service.CalculateHeatTransferCoefficient(room, 24, 20, 3, 1, 12);

        Assert.Equal(0.0, result);
    }

    [Fact]
    public void ClosedOpening_ReturnsZero()
    {
        var room = CreateRoomWithVentilationParameters(0.3, 0.4, 1.1);
        var options = new NaturalVentilationOptions { Enabled = true };

        var service = CreateService(options, new NaturalVentilationOpeningState(false, 0.0, 0.0, "Closed"));
        var result = service.CalculateHeatTransferCoefficient(room, 24, 20, 3, 1, 12);

        Assert.Equal(0.0, result);
    }

    [Fact]
    public void MissingVentilationParameters_ReturnsZero()
    {
        var room = DomainInvariantTests.CreateRoom();
        var options = new NaturalVentilationOptions { Enabled = true };

        var service = CreateService(options, new NaturalVentilationOpeningState(true, 1.0, 1.0, "Open"));
        var result = service.CalculateHeatTransferCoefficient(room, 24, 20, 3, 1, 12);

        Assert.Equal(0.0, result);
    }

    private static NaturalVentilationAirflowService CreateService(
        NaturalVentilationOptions options,
        NaturalVentilationOpeningState openingState,
        Iso16798NaturalVentilationCalculator? calculator = null,
        Iso16798NaturalVentilationApplicationAdapter? adapter = null) =>
        new(
            Options.Create(options),
            new StubOpeningControlService(openingState),
            calculator ?? new Iso16798NaturalVentilationCalculator(),
            adapter ?? new Iso16798NaturalVentilationApplicationAdapter());

    private static Modules.Buildings.Domain.Entities.Room CreateRoomWithVentilationParameters(
        double stackCoefficient,
        double windCoefficient,
        double windExposureFactor)
    {
        var room = DomainInvariantTests.CreateRoom(areaM2: 20.0);
        var parameters = VentilationParameters.Create(
            airChangesPerHour: 0.5,
            heatRecoveryEfficiency: 0.0,
            infiltrationAirChangesPerHour: 0.2,
            windExposureFactor: windExposureFactor,
            stackCoefficient: stackCoefficient,
            windCoefficient: windCoefficient).Value;

        Assert.True(room.SetVentilationParameters(parameters).IsSuccess);
        return room;
    }

    private static double CalculateCompatibilityExpected(
        double roomVolumeM3,
        double effectiveOpeningAreaM2,
        double indoorTemperatureC,
        double outdoorTemperatureC,
        double windSpeedMPerS,
        double dischargeCoefficient,
        double stackCoefficient,
        double windCoefficient,
        double windExposureFactor,
        double maximumAirChangesPerHour)
    {
        var deltaT = Math.Abs(indoorTemperatureC - outdoorTemperatureC);
        var stackFlowM3PerS = effectiveOpeningAreaM2 * dischargeCoefficient * stackCoefficient * Math.Sqrt(Math.Max(deltaT, 0.0));
        var windFlowM3PerS = effectiveOpeningAreaM2 * dischargeCoefficient * windCoefficient * Math.Max(windSpeedMPerS, 0.0) * Math.Max(1.0, windExposureFactor);
        var totalFlowM3PerH = (stackFlowM3PerS + windFlowM3PerS) * 3600.0;
        var ach = Math.Clamp(totalFlowM3PerH / roomVolumeM3, 0.0, maximumAirChangesPerHour);
        return AirPhysicalConstants.AirHeatCapacityWhPerM3K * ach * roomVolumeM3;
    }

    private sealed class StubOpeningControlService : INaturalVentilationOpeningControlService
    {
        private readonly NaturalVentilationOpeningState _state;

        public StubOpeningControlService(NaturalVentilationOpeningState state)
        {
            _state = state;
        }

        public NaturalVentilationOpeningState Resolve(
            Modules.Buildings.Domain.Entities.Room room,
            double indoorTemperatureC,
            double outdoorTemperatureC,
            double windSpeedMPerS,
            double demandFactor,
            int hourOfDay) => _state;
    }
}
