using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Tests.Calculations.StandardsFoundation;

public sealed class EngineeringUnitConversionTests
{
    [Fact]
    public void KilowattsToWatts_ConvertsDeterministically()
    {
        Assert.Equal(2500.0, EngineeringUnitConverter.KilowattsToWatts(2.5), 6);
    }

    [Fact]
    public void WattsToKilowatts_ConvertsDeterministically()
    {
        Assert.Equal(2.5, EngineeringUnitConverter.WattsToKilowatts(2500.0), 6);
    }

    [Fact]
    public void KilowattHoursToWattHours_ConvertsDeterministically()
    {
        Assert.Equal(1250.0, EngineeringUnitConverter.KilowattHoursToWattHours(1.25), 6);
    }

    [Fact]
    public void MegaJoulesToKilowattHours_ConvertsDeterministically()
    {
        Assert.Equal(1.0, EngineeringUnitConverter.MegaJoulesToKilowattHours(3.6), 6);
    }

    [Fact]
    public void CubicMetersPerHourToKilogramsPerSecond_ConvertsWithExplicitDensity()
    {
        var result = EngineeringUnitConverter.CubicMetersPerHourToKilogramsPerSecond(
            cubicMetersPerHour: 360.0,
            airDensityKgPerM3: 1.2);

        Assert.Equal(0.12, result, 6);
    }

    [Fact]
    public void AirChangesPerHourToCubicMetersPerHour_ConvertsWithExplicitVolume()
    {
        var result = EngineeringUnitConverter.AirChangesPerHourToCubicMetersPerHour(
            airChangesPerHour: 2.0,
            volumeCubicMeters: 120.0);

        Assert.Equal(240.0, result, 6);
    }

    [Fact]
    public void NegativeVolumeAndFlowInputs_AreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EngineeringUnitConverter.AirChangesPerHourToCubicMetersPerHour(1.0, -1.0));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EngineeringUnitConverter.CubicMetersPerHourToKilogramsPerSecond(-0.5, 1.2));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EngineeringUnitConverter.KilogramsPerSecondToCubicMetersPerHour(-0.1, 1.2));
    }
}
