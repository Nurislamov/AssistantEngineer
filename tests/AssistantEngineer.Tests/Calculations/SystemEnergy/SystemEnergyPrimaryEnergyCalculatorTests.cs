using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class SystemEnergyPrimaryEnergyCalculatorTests
{
    private static readonly StandardCalculationDisclosureFactory DisclosureFactory = new();

    [Fact]
    public void CalculatesPrimaryEnergyForElectricity()
    {
        var calculator = CreateCalculator();
        var finalEnergy = SystemEnergyPrimaryTestData.CreateFinalEnergyResult(
            new Dictionary<SystemEnergyCarrier, double>
            {
                [SystemEnergyCarrier.Electricity] = 1.0
            });
        var factorSet = SystemEnergyPrimaryTestData.CreateFactorSet(
            [
                SystemEnergyPrimaryTestData.CreatePrimaryFactor(
                    SystemEnergyCarrier.Electricity,
                    renewableFactor: 0.2,
                    nonRenewableFactor: 1.8,
                    totalFactor: 2.0)
            ]);

        var result = calculator.Calculate(finalEnergy, factorSet);

        Assert.Equal(8760.0, result.AnnualTotalFinalEnergyKWh, 6);
        Assert.Equal(1752.0, result.AnnualTotalRenewablePrimaryEnergyKWh, 6);
        Assert.Equal(15768.0, result.AnnualTotalNonRenewablePrimaryEnergyKWh, 6);
        Assert.Equal(17520.0, result.AnnualTotalPrimaryEnergyKWh, 6);
    }

    [Fact]
    public void CalculatesPrimaryEnergyForMultipleCarriers()
    {
        var calculator = CreateCalculator();
        var finalEnergy = SystemEnergyPrimaryTestData.CreateFinalEnergyResult(
            new Dictionary<SystemEnergyCarrier, double>
            {
                [SystemEnergyCarrier.Electricity] = 1.0,
                [SystemEnergyCarrier.NaturalGas] = 2.0
            });
        var factorSet = SystemEnergyPrimaryTestData.CreateFactorSet(
            [
                SystemEnergyPrimaryTestData.CreatePrimaryFactor(SystemEnergyCarrier.Electricity, 0.2, 1.8, 2.0),
                SystemEnergyPrimaryTestData.CreatePrimaryFactor(SystemEnergyCarrier.NaturalGas, 0.0, 1.1, 1.1)
            ]);

        var result = calculator.Calculate(finalEnergy, factorSet);
        var carrierTotals = result.Carriers.Sum(carrier => carrier.AnnualTotalPrimaryEnergyKWh);

        Assert.Equal(carrierTotals, result.AnnualTotalPrimaryEnergyKWh, 6);
    }

    [Fact]
    public void MissingPrimaryFactorProducesDiagnostic()
    {
        var calculator = CreateCalculator();
        var finalEnergy = SystemEnergyPrimaryTestData.CreateFinalEnergyResult(
            new Dictionary<SystemEnergyCarrier, double>
            {
                [SystemEnergyCarrier.Biomass] = 1.0
            });
        var factorSet = SystemEnergyPrimaryTestData.CreateFactorSet(
            [SystemEnergyPrimaryTestData.CreatePrimaryFactor(SystemEnergyCarrier.Electricity, 0.2, 1.8, 2.0)]);

        var result = calculator.Calculate(finalEnergy, factorSet);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-PRIMARY-FACTOR-MISSING");
    }

    [Fact]
    public void AggregatesMonthlyPrimaryEnergy()
    {
        var calculator = CreateCalculator();
        var finalEnergy = SystemEnergyPrimaryTestData.CreateFinalEnergyResult(
            new Dictionary<SystemEnergyCarrier, double>
            {
                [SystemEnergyCarrier.Electricity] = 1.0
            });
        var factorSet = SystemEnergyPrimaryTestData.CreateFactorSet(
            [SystemEnergyPrimaryTestData.CreatePrimaryFactor(SystemEnergyCarrier.Electricity, 0.2, 1.8, 2.0)]);

        var result = calculator.Calculate(finalEnergy, factorSet);

        Assert.Equal(12, result.MonthlyTotalPrimaryEnergyKWh.Count);
        Assert.True(result.MonthlyTotalPrimaryEnergyKWh[0] > result.MonthlyTotalPrimaryEnergyKWh[1]);
    }

    [Fact]
    public void CalculatesEndUsePrimaryEnergy()
    {
        var calculator = CreateCalculator();
        var finalEnergy = SystemEnergyPrimaryTestData.CreateFinalEnergyResult(
            new Dictionary<SystemEnergyCarrier, double>
            {
                [SystemEnergyCarrier.NaturalGas] = 1.0,
                [SystemEnergyCarrier.Electricity] = 1.0
            },
            new Dictionary<SystemEnergyEndUse, IReadOnlyDictionary<SystemEnergyCarrier, double>>
            {
                [SystemEnergyEndUse.SpaceHeating] = new Dictionary<SystemEnergyCarrier, double>
                {
                    [SystemEnergyCarrier.NaturalGas] = 1.0
                },
                [SystemEnergyEndUse.DomesticHotWater] = new Dictionary<SystemEnergyCarrier, double>
                {
                    [SystemEnergyCarrier.Electricity] = 1.0
                }
            });
        var factorSet = SystemEnergyPrimaryTestData.CreateFactorSet(
            [
                SystemEnergyPrimaryTestData.CreatePrimaryFactor(SystemEnergyCarrier.Electricity, 0.2, 1.8, 2.0),
                SystemEnergyPrimaryTestData.CreatePrimaryFactor(SystemEnergyCarrier.NaturalGas, 0.0, 1.1, 1.1)
            ]);

        var result = calculator.Calculate(finalEnergy, factorSet);

        Assert.Contains(result.EndUses, endUse => endUse.EndUse == SystemEnergyEndUse.SpaceHeating);
        Assert.Contains(result.EndUses, endUse => endUse.EndUse == SystemEnergyEndUse.DomesticHotWater);
        Assert.All(result.EndUses, endUse => Assert.True(endUse.AnnualTotalPrimaryEnergyKWh > 0.0));
    }

    [Fact]
    public void DisclosureKeepsForbiddenClaims()
    {
        var calculator = CreateCalculator();
        var finalEnergy = SystemEnergyPrimaryTestData.CreateFinalEnergyResult(
            new Dictionary<SystemEnergyCarrier, double>
            {
                [SystemEnergyCarrier.Electricity] = 1.0
            });
        var factorSet = SystemEnergyPrimaryTestData.CreateFactorSet(
            [SystemEnergyPrimaryTestData.CreatePrimaryFactor(SystemEnergyCarrier.Electricity, 0.2, 1.8, 2.0)]);

        var result = calculator.Calculate(finalEnergy, factorSet);
        var boundary = result.Disclosure.ClaimBoundary;

        Assert.Contains("Full ISO compliance", boundary.ForbiddenClaims);
        Assert.Contains("Full EN compliance", boundary.ForbiddenClaims);
        Assert.Contains("StandardReference equivalence", boundary.ForbiddenClaims);
        Assert.Contains("EnergyPlus comparison workflow", boundary.ForbiddenClaims);
        Assert.Contains("ASHRAE 140 / BESTEST-style validation anchor", boundary.ForbiddenClaims);
        Assert.DoesNotContain(boundary.AllowedClaims, claim => claim.Contains("Full ISO compliance", StringComparison.Ordinal));
        Assert.DoesNotContain(boundary.AllowedClaims, claim => claim.Contains("Full EN compliance", StringComparison.Ordinal));
        Assert.DoesNotContain(boundary.AllowedClaims, claim => claim.Contains("StandardReference equivalence", StringComparison.Ordinal));
        Assert.DoesNotContain(boundary.AllowedClaims, claim => claim.Contains("EnergyPlus comparison workflow", StringComparison.Ordinal));
        Assert.DoesNotContain(boundary.AllowedClaims, claim => claim.Contains("ASHRAE 140 / BESTEST-style validation anchor", StringComparison.Ordinal));
    }

    private static SystemEnergyPrimaryEnergyCalculator CreateCalculator() =>
        new(
            new SystemEnergyFactorSetValidator(),
            new SystemEnergyEmissionCalculator(),
            DisclosureFactory);
}
