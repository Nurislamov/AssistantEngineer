using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation.Iso16798;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests;

public class VentilationAndInfiltrationLoadEngineTests
{
    private readonly VentilationAndInfiltrationLoadEngine _engine = new();

    [Fact]
    public void MechanicalVentilationHeatingLoadReturnsExpectedValue()
    {
        var result = Calculate(CreateInput(
            indoorTemperatureC: 20,
            outdoorTemperatureC: -5,
            mechanicalAirflowM3PerHour: 100));

        Assert.False(result.HasErrors);
        Assert.Equal(100, result.MechanicalVentilation.AirflowM3PerHour);
        Assert.Equal(0.027778, result.MechanicalVentilation.AirflowM3PerSecond);
        Assert.Equal(837.5, result.MechanicalVentilation.RawHeatingLoadW);
        Assert.Equal(837.5, result.MechanicalVentilation.EffectiveHeatingLoadW);
        Assert.Equal(0, result.TotalCoolingLoadW);
    }

    [Fact]
    public void MechanicalVentilationCoolingLoadReturnsExpectedValue()
    {
        var result = Calculate(CreateInput(
            indoorTemperatureC: 24,
            outdoorTemperatureC: 34,
            mechanicalAirflowM3PerHour: 100));

        Assert.False(result.HasErrors);
        Assert.Equal(-10, result.DeltaTC);
        Assert.Equal(335, result.MechanicalVentilation.RawCoolingLoadW);
        Assert.Equal(335, result.TotalCoolingLoadW);
        Assert.Equal(0, result.TotalHeatingLoadW);
    }

    [Fact]
    public void HeatRecoveryReducesVentilationLoad()
    {
        var result = Calculate(CreateInput(
            indoorTemperatureC: 20,
            outdoorTemperatureC: -5,
            mechanicalAirflowM3PerHour: 100,
            heatRecoveryEfficiency: 0.6));

        Assert.Equal(837.5, result.MechanicalVentilation.RawHeatingLoadW);
        Assert.Equal(335, result.MechanicalVentilation.EffectiveHeatingLoadW);
        Assert.Equal(335, result.TotalHeatingLoadW);
    }

    [Fact]
    public void InfiltrationByAchReturnsExpectedValue()
    {
        var result = Calculate(CreateInput(
            indoorTemperatureC: 20,
            outdoorTemperatureC: -5,
            infiltrationAirChangesPerHour: 0.5));

        Assert.False(result.HasErrors);
        Assert.Equal(0.5, result.Infiltration.InfiltrationAirChangesPerHour);
        Assert.Equal(50, result.Infiltration.InfiltrationAirflowM3PerHour);
        Assert.Equal(0.013889, result.Infiltration.InfiltrationAirflowM3PerSecond);
        Assert.Equal(418.75, result.Infiltration.HeatingLoadW);
    }

    [Fact]
    public void ZeroAirflowReturnsZeroLoad()
    {
        var result = Calculate(CreateInput(
            indoorTemperatureC: 20,
            outdoorTemperatureC: -5,
            mechanicalAirflowM3PerHour: 0));

        Assert.False(result.HasErrors);
        Assert.Equal(0, result.MechanicalVentilation.RawHeatingLoadW);
        Assert.Equal(0, result.TotalHeatingLoadW);
        Assert.Equal(0, result.TotalCoolingLoadW);
    }

    [Fact]
    public void InvalidHeatRecoveryEfficiencyProducesDiagnostics()
    {
        var result = Calculate(CreateInput(
            indoorTemperatureC: 20,
            outdoorTemperatureC: -5,
            mechanicalAirflowM3PerHour: 100,
            heatRecoveryEfficiency: 1.25));

        Assert.True(result.HasErrors);
        Assert.Equal(0, result.TotalHeatingLoadW);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "Ventilation.InvalidHeatRecoveryEfficiency");
    }

    [Fact]
    public void NegativeAirflowProducesDiagnostics()
    {
        var result = Calculate(CreateInput(
            indoorTemperatureC: 20,
            outdoorTemperatureC: -5,
            mechanicalAirflowM3PerHour: -1));

        Assert.True(result.HasErrors);
        Assert.Equal(0, result.TotalHeatingLoadW);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "Ventilation.MechanicalAirflowM3PerHour");
    }

    [Fact]
    public void AchRequiresValidRoomVolume()
    {
        var result = Calculate(CreateInput(
            indoorTemperatureC: 20,
            outdoorTemperatureC: -5,
            volumeM3: 0,
            infiltrationAirChangesPerHour: 0.5));

        Assert.True(result.HasErrors);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "Ventilation.InvalidVolume");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "Ventilation.InfiltrationAirChangesPerHour");
    }

    [Fact]
    public void UnitConversionsAreCorrect()
    {
        Assert.Equal(0.0277778, AirflowNormalizer.M3PerHourToM3PerSecond(100).Value, 7);
        Assert.Equal(0.01, AirflowNormalizer.LitersPerSecondToM3PerSecond(10).Value, 7);
        Assert.Equal(50, AirflowNormalizer.AirChangesPerHourToM3PerHour(100, 0.5).Value, 7);
        Assert.Equal(20, AirflowNormalizer.AirflowPerPersonToLitersPerSecond(10, 2).Value, 7);
        Assert.Equal(12, AirflowNormalizer.AirflowPerAreaToLitersPerSecond(0.3, 40).Value, 7);
    }

    [Fact]
    public void DiagnosticsIncludeConstantsWhenDefaultsAreUsed()
    {
        var result = Calculate(new VentilationAndInfiltrationLoadInput(
            RoomId: 101,
            AreaM2: 40,
            VolumeM3: 100,
            OccupancyPeople: 0,
            IndoorTemperatureC: 20,
            OutdoorTemperatureC: -5,
            MechanicalAirflowM3PerHour: 100));

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "Ventilation.AirConstantsUsed");
        Assert.Equal(AirPhysicalConstants.AirDensityKgPerM3, result.AirDensityKgPerM3);
        Assert.Equal(AirPhysicalConstants.AirSpecificHeatJPerKgK, result.AirSpecificHeatJPerKgK);
    }

    [Fact]
    public void MissingInfiltrationAndNaturalInputsProduceExpectedDiagnosticsInStableOrder()
    {
        var result = Calculate(CreateInput(
            indoorTemperatureC: 20,
            outdoorTemperatureC: -5,
            mechanicalAirflowM3PerHour: 100));

        var codes = result.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray();
        var infiltrationIndex = Array.IndexOf(codes, "Ventilation.NoInfiltrationAirflow");
        var naturalIndex = Array.IndexOf(codes, "Ventilation.NoNaturalVentilationAirflow");

        Assert.True(infiltrationIndex >= 0);
        Assert.True(naturalIndex >= 0);
        Assert.True(infiltrationIndex < naturalIndex);
    }

    [Fact]
    public void MechanicalAirflowFromPerPersonAndPerAreaInputsIsSummed()
    {
        var result = Calculate(new VentilationAndInfiltrationLoadInput(
            RoomId: 101,
            AreaM2: 40,
            VolumeM3: 100,
            OccupancyPeople: 2,
            IndoorTemperatureC: 20,
            OutdoorTemperatureC: -5,
            AirflowPerPersonLps: 10.0,
            AirflowPerAreaLpsM2: 0.25,
            AirDensityKgPerM3: AirPhysicalConstants.AirDensityKgPerM3,
            AirSpecificHeatJPerKgK: AirPhysicalConstants.AirSpecificHeatJPerKgK,
            DiagnosticsContext: "Ventilation test"));

        Assert.Equal(108.0, result.MechanicalVentilation.AirflowM3PerHour, 6);
    }

    [Fact]
    public void EnhancedNaturalVentilationResult_IsUsedWhenProvided()
    {
        var enhanced = new Iso16798NaturalVentilationResult(
            CalculationMode: Iso16798NaturalVentilationCalculationMode.MaxWindOrStack,
            EffectiveOpeningAreaM2: 0.9,
            StackAirflowM3PerS: 0.2,
            WindAirflowM3PerS: 0.5,
            TotalAirflowM3PerS: 0.5,
            TotalAirflowM3PerH: 1800.0,
            AirChangesPerHour: 18.0,
            ClampedAirChangesPerHour: 10.0,
            HeatTransferCoefficientWPerK: 335.0,
            Diagnostics: [],
            AirflowM3PerHour: 1000.0,
            AirChangeRatePerHour: 10.0,
            WindComponentM3PerHour: 1800.0,
            StackComponentM3PerHour: 720.0,
            SelectedBranch: "MaxWindStack:Wind",
            ClampReason: "Air-change rate was clamped to maximum ACH 10.000000.",
            ControlReason: null);

        var result = Calculate(new VentilationAndInfiltrationLoadInput(
            RoomId: 101,
            AreaM2: 40,
            VolumeM3: 100,
            OccupancyPeople: 0,
            IndoorTemperatureC: 25,
            OutdoorTemperatureC: 15,
            NaturalVentilationEnhancedResult: enhanced));

        Assert.Equal(1000.0, result.NaturalVentilation.AirflowM3PerHour);
        Assert.Equal(10.0, result.NaturalVentilation.AirChangeRatePerHour);
        Assert.Equal(335.0, result.NaturalVentilation.HeatTransferCoefficientWPerK);
        Assert.Equal("MaxWindStack:Wind", result.NaturalVentilation.SelectedBranch);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "Ventilation.NaturalVentilationEnhancedResultUsed");
    }

    [Fact]
    public async Task RoomHeatingCalculationIncludesVentilationAndInfiltrationComponents()
    {
        var floor = DomainInvariantTests.CreateFloor();
        var room = floor.AddRoom(
            "Ventilation room",
            Area.FromSquareMeters(20).Value,
            heightM: 5,
            Temperature.FromCelsius(20).Value,
            Temperature.FromCelsius(-5).Value).Value;
        var ventilation = VentilationParameters.Create(
            airChangesPerHour: 1,
            heatRecoveryEfficiency: 0,
            infiltrationAirChangesPerHour: 0.5,
            windExposureFactor: 0,
            stackCoefficient: 0,
            windCoefficient: 0).Value;
        Assert.True(room.SetVentilationParameters(ventilation).IsSuccess);
        var calculator = new En12831HeatingLoadCalculator(
            Options.Create(new En12831HeatingLoadOptions()));

        var result = await calculator.CalculateAsync(room);

        Assert.Equal(837.5, result.MechanicalVentilationHeatLossW);
        Assert.Equal(418.75, result.InfiltrationHeatLossW);
        Assert.Equal(1256.25, result.VentilationHeatLossW);
    }

    [Fact]
    public async Task RoomCoolingCalculationIncludesVentilationAndInfiltrationComponents()
    {
        var project = DomainInvariantTests.CreateProject();
        var climateZone = ClimateZone.Create(
            "Ventilation cooling climate",
            Temperature.FromCelsius(34).Value,
            Temperature.FromCelsius(-5).Value).Value;
        var building = Building.Create("Ventilation cooling building", project, climateZone).Value;
        var floor = building.AddFloor("Level 1").Value;
        var room = floor.AddRoom(
            "Cooling ventilation room",
            Area.FromSquareMeters(20).Value,
            heightM: 5,
            Temperature.FromCelsius(24).Value,
            Temperature.FromCelsius(34).Value).Value;
        var ventilation = VentilationParameters.Create(
            airChangesPerHour: 1,
            heatRecoveryEfficiency: 0,
            infiltrationAirChangesPerHour: 0.5,
            windExposureFactor: 0,
            stackCoefficient: 0,
            windCoefficient: 0).Value;
        Assert.True(room.SetVentilationParameters(ventilation).IsSuccess);
        var calculator = new Iso52016CoolingLoadCalculator(
            Options.Create(new Iso52016CoolingLoadOptions()),
            new ConstantIso52016ReferenceDataProvider(34),
            CalculationTestFactory.CreateProfileAggregator());

        var result = await calculator.CalculateAsync(room);

        Assert.Equal(335, result.VentilationHeatGainW);
        Assert.Equal(167.5, result.InfiltrationHeatGainW);
        Assert.Equal(0, result.NaturalVentilationHeatGainW);
        Assert.Equal(502.5, result.CoolingLoadW);
    }

    private VentilationAndInfiltrationLoadResult Calculate(
        VentilationAndInfiltrationLoadInput input)
    {
        var result = _engine.Calculate(input);
        Assert.True(result.IsSuccess, result.Error);
        return result.Value;
    }

    private static VentilationAndInfiltrationLoadInput CreateInput(
        double indoorTemperatureC,
        double outdoorTemperatureC,
        double volumeM3 = 100,
        double? mechanicalAirflowM3PerHour = null,
        double? infiltrationAirChangesPerHour = null,
        double heatRecoveryEfficiency = 0) =>
        new(
            RoomId: 101,
            AreaM2: 40,
            VolumeM3: volumeM3,
            OccupancyPeople: 0,
            IndoorTemperatureC: indoorTemperatureC,
            OutdoorTemperatureC: outdoorTemperatureC,
            MechanicalAirflowM3PerHour: mechanicalAirflowM3PerHour,
            InfiltrationAirChangesPerHour: infiltrationAirChangesPerHour,
            HeatRecoveryEfficiency: heatRecoveryEfficiency,
            AirDensityKgPerM3: AirPhysicalConstants.AirDensityKgPerM3,
            AirSpecificHeatJPerKgK: AirPhysicalConstants.AirSpecificHeatJPerKgK,
            DiagnosticsContext: "Ventilation test");

    private sealed class ConstantIso52016ReferenceDataProvider : ISo52016ReferenceDataProvider
    {
        private readonly double _outdoorTemperatureC;

        public ConstantIso52016ReferenceDataProvider(double outdoorTemperatureC)
        {
            _outdoorTemperatureC = outdoorTemperatureC;
        }

        public Task<IReadOnlyList<double>?> GetOutdoorTemperatureProfileAsync(
            ClimateZone climateZone,
            int month,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<double>?>(
                Enumerable.Repeat(_outdoorTemperatureC, 24).ToArray());

        public Task<IReadOnlyDictionary<CardinalDirection, IReadOnlyList<double>>> GetSolarRadiationAsync(
            ClimateZone climateZone,
            int month,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<CardinalDirection, IReadOnlyList<double>>>(
                new Dictionary<CardinalDirection, IReadOnlyList<double>>());

        public Task<bool> HasClimateDataAsync(
            ClimateZone climateZone,
            int month,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public double GetDefaultSolarRadiation(CardinalDirection orientation) => 0;

        public double GetPeopleHeatGain(RoomType roomType) => 0;
    }
}
