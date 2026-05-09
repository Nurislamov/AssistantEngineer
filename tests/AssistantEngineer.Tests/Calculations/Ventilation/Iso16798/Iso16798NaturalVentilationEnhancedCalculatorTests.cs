using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation.Iso16798;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation.Iso16798;

namespace AssistantEngineer.Tests.Calculations.Ventilation.Iso16798;

public sealed class Iso16798NaturalVentilationEnhancedCalculatorTests
{
    private readonly Iso16798NaturalVentilationCalculator _calculator = new();

    [Fact]
    public void ZeroOpening_GivesZeroAirflow()
    {
        var result = _calculator.Calculate(CreateInput([
            new NaturalVentilationOpening("opening-a", 0.0, 1.0, true)
        ]));

        Assert.Equal(0.0, result.AirflowM3PerHour);
        Assert.Equal("Closed", result.SelectedBranch);
    }

    [Fact]
    public void ZeroWind_WithDeltaT_ProducesStackAirflow()
    {
        var result = _calculator.Calculate(CreateInput(
            openings:
            [
                new NaturalVentilationOpening("opening-a", 1.0, 0.7, true)
            ],
            indoorTemperatureC: 24.0,
            outdoorTemperatureC: 18.0,
            windSpeedMPerS: 0.0));

        Assert.True(result.StackAirflowM3PerS > 0.0);
        Assert.Equal(0.0, result.WindAirflowM3PerS);
        Assert.Equal(Iso16798NaturalVentilationCalculationMode.StackOnly, result.CalculationMode);
    }

    [Fact]
    public void ZeroDeltaT_WithWind_ProducesWindAirflow()
    {
        var result = _calculator.Calculate(CreateInput(
            openings:
            [
                new NaturalVentilationOpening("opening-a", 1.0, 0.8, true)
            ],
            indoorTemperatureC: 22.0,
            outdoorTemperatureC: 22.0,
            windSpeedMPerS: 4.0));

        Assert.Equal(0.0, result.StackAirflowM3PerS);
        Assert.True(result.WindAirflowM3PerS > 0.0);
        Assert.Equal(Iso16798NaturalVentilationCalculationMode.WindOnly, result.CalculationMode);
    }

    [Fact]
    public void ClosedSchedule_GivesZeroAirflow()
    {
        var result = _calculator.Calculate(CreateInput(
            openings:
            [
                new NaturalVentilationOpening("opening-a", 1.0, 1.0, true)
            ],
            schedule: new NaturalVentilationOpeningSchedule(OpeningFraction: 0.0)));

        Assert.Equal(0.0, result.AirflowM3PerHour);
        Assert.Equal("Closed", result.SelectedBranch);
        Assert.Equal("Opening schedule is closed for this step.", result.ControlReason);
    }

    [Fact]
    public void OpeningFraction_ScalesEffectiveArea()
    {
        var result = _calculator.Calculate(CreateInput(
            openings:
            [
                new NaturalVentilationOpening("opening-a", 2.0, 0.25, true)
            ]));

        Assert.Equal(0.5, result.EffectiveOpeningAreaM2, 6);
    }

    [Fact]
    public void AchClamp_WorksAndSetsClampReason()
    {
        var result = _calculator.Calculate(CreateInput(
            openings:
            [
                new NaturalVentilationOpening("opening-a", 2.5, 1.0, true)
            ],
            windSpeedMPerS: 7.0,
            maximumAch: 3.0));

        Assert.True(result.AirChangesPerHour >= result.ClampedAirChangesPerHour);
        Assert.Equal(3.0, result.ClampedAirChangesPerHour, 6);
        Assert.NotNull(result.ClampReason);
    }

    [Fact]
    public void DensityCorrection_IsDeterministic()
    {
        var input = CreateInput(
            openings:
            [
                new NaturalVentilationOpening("opening-a", 1.0, 0.7, true)
            ],
            indoorTemperatureC: 28.0,
            outdoorTemperatureC: 18.0,
            windSpeedMPerS: 3.0,
            options: new NaturalVentilationCalculationOptions(
                BranchSelectionMode: NaturalVentilationBranchSelectionMode.SumWindAndStack,
                UseDensityCorrection: true,
                UseAltitudeDensityCorrection: true,
                SingleSidedOpeningCoefficient: 1.0,
                MaximumAirChangesPerHour: 15.0),
            drivingForces: new NaturalVentilationDrivingForces(
                IndoorTemperatureC: 28.0,
                OutdoorTemperatureC: 18.0,
                WindSpeedMPerS: 3.0,
                OpeningHeightM: 2.0,
                AirDensityKgPerM3: 1.204,
                AirSpecificHeatJPerKgK: 1005.0,
                AltitudeMeters: 650.0));

        var first = _calculator.Calculate(input);
        var second = _calculator.Calculate(input);

        Assert.Equal(first.HeatTransferCoefficientWPerK, second.HeatTransferCoefficientWPerK);
        Assert.Equal(first.AirflowM3PerHour, second.AirflowM3PerHour);
        Assert.Equal(first.SelectedBranch, second.SelectedBranch);
    }

    [Fact]
    public void OccupancyControl_DisablesAndEnablesVentilation()
    {
        var disabled = _calculator.Calculate(CreateInput(
            openings:
            [
                new NaturalVentilationOpening("opening-a", 1.0, 1.0, true)
            ],
            occupancyControl: new NaturalVentilationOccupancyControl(
                Enabled: true,
                OccupancyFraction: 0.0,
                MinimumOccupancyFractionToEnable: 0.1,
                DisableWhenUnoccupied: true)));

        var enabled = _calculator.Calculate(CreateInput(
            openings:
            [
                new NaturalVentilationOpening("opening-a", 1.0, 1.0, true)
            ],
            occupancyControl: new NaturalVentilationOccupancyControl(
                Enabled: true,
                OccupancyFraction: 0.5,
                MinimumOccupancyFractionToEnable: 0.1,
                DisableWhenUnoccupied: true)));

        Assert.Equal(0.0, disabled.AirflowM3PerHour);
        Assert.True(enabled.AirflowM3PerHour > 0.0);
    }

    [Fact]
    public void HeatTransferCoefficient_MatchesAirflowRelation()
    {
        const double roomVolume = 90.0;
        const double density = 1.2;
        const double cp = 1005.0;

        var result = _calculator.Calculate(CreateInput(
            openings:
            [
                new NaturalVentilationOpening("opening-a", 1.8, 0.9, true)
            ],
            roomVolumeM3: roomVolume,
            airDensityKgPerM3: density,
            airSpecificHeatJPerKgK: cp,
            options: new NaturalVentilationCalculationOptions(
                BranchSelectionMode: NaturalVentilationBranchSelectionMode.SumWindAndStack,
                UseDensityCorrection: false,
                UseAltitudeDensityCorrection: false,
                SingleSidedOpeningCoefficient: 1.0,
                MaximumAirChangesPerHour: 10.0)));

        var flowM3PerS = result.AirChangeRatePerHour * roomVolume / 3600.0;
        var expected = density * cp * flowM3PerS;
        Assert.Equal(expected, result.HeatTransferCoefficientWPerK, 6);
    }

    [Fact]
    public void MaxWindStackBranch_UsesMaximumComponent()
    {
        var result = _calculator.Calculate(CreateInput(
            openings:
            [
                new NaturalVentilationOpening("opening-a", 1.2, 0.9, true)
            ],
            indoorTemperatureC: 25.0,
            outdoorTemperatureC: 16.0,
            windSpeedMPerS: 5.0,
            options: new NaturalVentilationCalculationOptions(
                BranchSelectionMode: NaturalVentilationBranchSelectionMode.MaxOfWindAndStack,
                UseDensityCorrection: false,
                UseAltitudeDensityCorrection: false,
                SingleSidedOpeningCoefficient: 1.0,
                MaximumAirChangesPerHour: 30.0)));

        var expected = Math.Max(result.StackAirflowM3PerS, result.WindAirflowM3PerS) * 3600.0;
        Assert.InRange(result.AirflowM3PerHour, expected - 0.01, expected + 0.01);
        Assert.StartsWith("MaxWindStack:", result.SelectedBranch, StringComparison.Ordinal);
    }

    private static Iso16798NaturalVentilationInput CreateInput(
        IReadOnlyList<NaturalVentilationOpening> openings,
        double roomVolumeM3 = 100.0,
        double indoorTemperatureC = 24.0,
        double outdoorTemperatureC = 20.0,
        double windSpeedMPerS = 3.0,
        double airDensityKgPerM3 = 1.2,
        double airSpecificHeatJPerKgK = 1005.0,
        double maximumAch = 20.0,
        NaturalVentilationOpeningSchedule? schedule = null,
        NaturalVentilationDrivingForces? drivingForces = null,
        NaturalVentilationOccupancyControl? occupancyControl = null,
        NaturalVentilationCalculationOptions? options = null)
    {
        var legacyOpenings = openings
            .Select(opening => new Iso16798NaturalVentilationOpeningInput(
                opening.OpeningId,
                opening.OpeningAreaM2,
                opening.OpeningFraction,
                opening.IsOpen))
            .ToArray();

        return new Iso16798NaturalVentilationInput(
            RoomVolumeM3: roomVolumeM3,
            IndoorTemperatureC: indoorTemperatureC,
            OutdoorTemperatureC: outdoorTemperatureC,
            WindSpeedMPerS: windSpeedMPerS,
            AirDensityKgPerM3: airDensityKgPerM3,
            AirSpecificHeatJPerKgK: airSpecificHeatJPerKgK,
            DischargeCoefficient: 0.6,
            MaximumAirChangesPerHour: maximumAch,
            OpeningHeightM: 2.0,
            UsefulHeightDifferenceM: 2.0,
            WindPressureCoefficient: 0.7,
            WindExposureFactor: 1.2,
            StackCoefficient: 1.0,
            WindCoefficient: 1.0,
            Openings: legacyOpenings,
            NaturalVentilationOpenings: openings,
            OpeningSchedule: schedule,
            DrivingForces: drivingForces,
            OccupancyControl: occupancyControl,
            CalculationOptions: options);
    }
}
