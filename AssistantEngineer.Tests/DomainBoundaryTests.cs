using AssistantEngineer.Application.Services.Calculations;
using AssistantEngineer.Domain.Models.Schedules;
using AssistantEngineer.Domain.Models.Ventilation;
using System.Reflection;

namespace AssistantEngineer.Tests;

public class DomainBoundaryTests
{
    [Fact]
    public void HourlyScheduleAcceptsExactlyTwentyFourBoundaryFactors()
    {
        var factors = Enumerable.Range(0, 24)
            .Select(index => index % 2 == 0 ? 0.0 : 1.0)
            .ToArray();

        var result = HourlySchedule.Create("Boundary schedule", factors);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(24, result.Value.Factors.Count);
    }

    [Fact]
    public void HourlyScheduleRejectsWrongLengthAndOutOfRangeFactors()
    {
        var wrongLength = HourlySchedule.Create("Short schedule", Enumerable.Repeat(0.5, 23).ToArray());
        var outOfRange = HourlySchedule.Create("Invalid schedule", Enumerable.Repeat(1.1, 24).ToArray());

        Assert.True(wrongLength.IsFailure);
        Assert.True(outOfRange.IsFailure);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ValueObjectsRejectNonFiniteNumbers(double value)
    {
        Assert.True(AssistantEngineer.Domain.ValueObjects.Area.FromSquareMeters(value).IsFailure);
        Assert.True(AssistantEngineer.Domain.ValueObjects.Power.FromWatts(value).IsFailure);
        Assert.True(AssistantEngineer.Domain.ValueObjects.Temperature.FromCelsius(value).IsFailure);
        Assert.True(AssistantEngineer.Domain.ValueObjects.ThermalTransmittance.FromValue(value).IsFailure);
        Assert.True(AssistantEngineer.Domain.ValueObjects.SolarHeatGainCoefficient.FromValue(value).IsFailure);
    }

    [Fact]
    public void GenericResultRejectsNullSuccessValue()
    {
        Assert.Throws<ArgumentNullException>(() => AssistantEngineer.Domain.Primitives.Result<string>.Success(null!));
    }

    [Fact]
    public async Task FullHeatRecoveryEliminatesVentilationHeatingLoss()
    {
        var calculator = CalculationTestFactory.CreateHeatingLoadCalculator();
        var floor = DomainInvariantTests.CreateFloor();
        var room = floor.AddRoom(
            "Office",
            AssistantEngineer.Domain.ValueObjects.Area.FromSquareMeters(20).Value,
            3,
            AssistantEngineer.Domain.ValueObjects.Temperature.FromCelsius(22).Value,
            AssistantEngineer.Domain.ValueObjects.Temperature.FromCelsius(-15).Value).Value;
        var ventilation = VentilationParameters.Create(airChangesPerHour: 1, heatRecoveryEfficiency: 1).Value;
        Assert.True(room.SetVentilationParameters(ventilation).IsSuccess);

        var result = await calculator.CalculateAsync(room);

        Assert.Equal(0, result.VentilationHeatLossW);
    }

    [Fact]
    public void RoomRejectsMalformedScheduleAssignment()
    {
        var room = DomainInvariantTests.CreateRoom();
        var malformedSchedule = CreateMalformedSchedule([0.5, 0.5, 0.5]);

        var result = room.SetOccupancySchedule(malformedSchedule);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void RoomRejectsMalformedVentilationParametersAssignment()
    {
        var room = DomainInvariantTests.CreateRoom();
        var malformedVentilation = CreateMalformedVentilationParameters(airChangesPerHour: -1, heatRecoveryEfficiency: 0.5);

        var result = room.SetVentilationParameters(malformedVentilation);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UnsupportedCoolingCalculationMethodThrows()
    {
        var calculator = CalculationTestFactory.CreateRoomCoolingLoadCalculator();
        var room = DomainInvariantTests.CreateRoom();
        var unsupportedMethod = (AssistantEngineer.Domain.Models.CoolingLoadCalculationMethod)999;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            calculator.CalculateAsync(room, unsupportedMethod));
    }

    private static HourlySchedule CreateMalformedSchedule(IReadOnlyList<double> factors)
    {
        var schedule = (HourlySchedule)Activator.CreateInstance(
            typeof(HourlySchedule),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: [],
            culture: null)!;
        SetBackingField(schedule, nameof(HourlySchedule.Name), "Malformed schedule");
        SetBackingField(schedule, nameof(HourlySchedule.Factors), factors);
        return schedule;
    }

    private static VentilationParameters CreateMalformedVentilationParameters(
        double airChangesPerHour,
        double heatRecoveryEfficiency)
    {
        var ventilation = (VentilationParameters)Activator.CreateInstance(
            typeof(VentilationParameters),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: [],
            culture: null)!;
        SetBackingField(ventilation, nameof(VentilationParameters.AirChangesPerHour), airChangesPerHour);
        SetBackingField(ventilation, nameof(VentilationParameters.HeatRecoveryEfficiency), heatRecoveryEfficiency);
        return ventilation;
    }

    private static void SetBackingField<T>(object target, string propertyName, T value)
    {
        var field = target.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(target, value);
    }
}
