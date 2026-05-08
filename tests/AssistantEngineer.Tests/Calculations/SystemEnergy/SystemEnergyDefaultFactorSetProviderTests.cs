using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class SystemEnergyDefaultFactorSetProviderTests
{
    private readonly SystemEnergyDefaultFactorSetProvider _provider = new();

    [Fact]
    public void ProvidesProjectDefaultFactorSet()
    {
        var factorSet = _provider.GetProjectDefaultFactorSet();

        Assert.Contains(
            factorSet.PrimaryEnergyFactors,
            factor => factor.Carrier == SystemEnergyCarrier.Electricity);
        Assert.Contains(
            factorSet.PrimaryEnergyFactors,
            factor => factor.Carrier == SystemEnergyCarrier.NaturalGas);
        Assert.Contains(
            factorSet.Diagnostics,
            diagnostic => diagnostic.Code == "AE-SYS-DEFAULT-FACTORS-NOT-COMPLIANCE-DATA");
    }

    [Fact]
    public void DefaultFactorsAreNonNegative()
    {
        var factorSet = _provider.GetProjectDefaultFactorSet();

        Assert.All(
            factorSet.PrimaryEnergyFactors,
            factor =>
            {
                Assert.True(factor.RenewableFactor >= 0.0);
                Assert.True(factor.NonRenewableFactor >= 0.0);
                Assert.True(factor.TotalFactor >= 0.0);
            });
    }
}
