using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater.Iso12831;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater.Iso12831;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater.Iso12831;

public sealed class Iso12831DomesticHotWaterDrawProfileTests
{
    private readonly Iso12831DomesticHotWaterDrawProfileProvider _provider = new();

    [Fact]
    public void ResidentialProfile_IsNormalized()
    {
        var result = _provider.ResolveProfiles(
            Iso12831DomesticHotWaterDrawProfileKind.ResidentialWeekdayWeekend,
            weekdayOverride: null,
            weekendOverride: null,
            customDrawProfile: null);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(24, result.Value.WeekdayProfile.Count);
        Assert.Equal(24, result.Value.WeekendProfile.Count);
        Assert.Equal(1.0, result.Value.WeekdayProfile.Sum(), 12);
        Assert.Equal(1.0, result.Value.WeekendProfile.Sum(), 12);
    }

    [Fact]
    public void CustomProfile_Requires24Values()
    {
        var result = _provider.ResolveProfiles(
            Iso12831DomesticHotWaterDrawProfileKind.Custom,
            weekdayOverride: null,
            weekendOverride: null,
            customDrawProfile: [1.0, 2.0]);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void InvalidOverride_WithNegativeValuesIsRejected()
    {
        var invalid = Enumerable.Repeat(1.0, 24).ToArray();
        invalid[5] = -0.5;

        var result = _provider.ResolveProfiles(
            Iso12831DomesticHotWaterDrawProfileKind.Flat,
            weekdayOverride: invalid,
            weekendOverride: null,
            customDrawProfile: null);

        Assert.True(result.IsFailure);
    }
}
