using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016InternalGainReferenceDataProviderTests
{
    private readonly Iso52016InternalGainReferenceDataProvider _provider = new();

    [Fact]
    public void GetAll_ReturnsReferenceDataForCommonBuildingUses()
    {
        var items = _provider.GetAll();

        Assert.Contains(items, item => item.UseType == "Residential");
        Assert.Contains(items, item => item.UseType == "Office");
        Assert.Contains(items, item => item.UseType == "HotelGuestRoom");
        Assert.Contains(items, item => item.UseType == "Restaurant");
    }

    [Fact]
    public void CalculatePeakSensibleGain_SplitsConvectiveAndRadiativeFractions()
    {
        var result = _provider.CalculatePeakSensibleGain(
            useType: "Office",
            floorAreaM2: 100.0,
            occupancyFactor: 1.0,
            lightingFactor: 0.5,
            equipmentFactor: 0.25);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(1500.0, result.Value.TotalSensibleGainW, precision: 6);
        Assert.Equal(result.Value.TotalSensibleGainW, result.Value.ConvectiveGainW + result.Value.RadiativeGainW, precision: 6);
    }

    [Fact]
    public void CalculatePeakSensibleGain_RejectsUnknownUseType()
    {
        var result = _provider.CalculatePeakSensibleGain(
            useType: "Unknown",
            floorAreaM2: 100.0);

        Assert.True(result.IsFailure);
        Assert.Equal("Internal gain reference data for use type 'Unknown' was not found.", result.Error);
    }
}
