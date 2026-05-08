using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class SystemEnergyCalculationSummaryBuilderTests
{
    private readonly SystemEnergyCalculationSummaryBuilder _builder = new();

    [Fact]
    public void BuildsCarrierSummaries()
    {
        var primaryResult = CreatePrimaryResult();

        var summary = _builder.Build(primaryResult);

        Assert.Contains(summary.Carriers, carrier => carrier.Carrier == SystemEnergyCarrier.Electricity);
        Assert.Contains(summary.Carriers, carrier => carrier.Carrier == SystemEnergyCarrier.NaturalGas);
    }

    [Fact]
    public void BuildsEndUseSummaries()
    {
        var primaryResult = CreatePrimaryResult();

        var summary = _builder.Build(primaryResult);

        Assert.Contains(summary.EndUses, endUse => endUse.EndUse == SystemEnergyEndUse.SpaceHeating);
        Assert.Contains(summary.EndUses, endUse => endUse.EndUse == SystemEnergyEndUse.DomesticHotWater);
    }

    [Fact]
    public void BuildsDisclosureSummaryNotForCompliance()
    {
        var primaryResult = CreatePrimaryResult();

        var summary = _builder.Build(primaryResult);

        Assert.Contains(
            new[] { SystemEnergyDisclosureStatus.NotForCompliance, SystemEnergyDisclosureStatus.StandardInspired },
            status => status == summary.DisclosureSummary.Status);

        Assert.Contains("Full ISO compliance", summary.DisclosureSummary.ForbiddenClaims);
        Assert.Contains("Full EN compliance", summary.DisclosureSummary.ForbiddenClaims);
        Assert.Contains("StandardReference equivalence", summary.DisclosureSummary.ForbiddenClaims);
        Assert.Contains("EnergyPlus comparison workflow", summary.DisclosureSummary.ForbiddenClaims);
        Assert.Contains("ASHRAE 140 / BESTEST-style validation anchor", summary.DisclosureSummary.ForbiddenClaims);
        Assert.Contains(
            summary.Diagnostics,
            diagnostic => diagnostic.Code == "AE-SYS-SUMMARY-NOT-FOR-COMPLIANCE");
    }

    [Fact]
    public void SummaryTotalsMatchPrimaryEnergyResult()
    {
        var primaryResult = CreatePrimaryResult();

        var summary = _builder.Build(primaryResult);

        Assert.Equal(primaryResult.AnnualTotalFinalEnergyKWh, summary.AnnualTotalFinalEnergyKWh, 6);
        Assert.Equal(primaryResult.AnnualTotalPrimaryEnergyKWh, summary.AnnualTotalPrimaryEnergyKWh, 6);
        if (primaryResult.AnnualTotalEmissionsKg.HasValue)
        {
            Assert.True(summary.AnnualTotalEmissionsKg.HasValue);
            Assert.Equal(primaryResult.AnnualTotalEmissionsKg.Value, summary.AnnualTotalEmissionsKg!.Value, 6);
        }
        else
        {
            Assert.Null(summary.AnnualTotalEmissionsKg);
        }
    }

    private static SystemEnergyPrimaryEnergyResult CreatePrimaryResult()
    {
        var finalEnergy = SystemEnergyPrimaryTestData.CreateFinalEnergyResult(
            new Dictionary<SystemEnergyCarrier, double>
            {
                [SystemEnergyCarrier.Electricity] = 1.0,
                [SystemEnergyCarrier.NaturalGas] = 1.0
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
            ],
            [
                SystemEnergyPrimaryTestData.CreateEmissionFactor(SystemEnergyCarrier.Electricity, 0.5),
                SystemEnergyPrimaryTestData.CreateEmissionFactor(SystemEnergyCarrier.NaturalGas, 0.25)
            ]);

        var calculator = new SystemEnergyPrimaryEnergyCalculator(
            new SystemEnergyFactorSetValidator(),
            new SystemEnergyEmissionCalculator(),
            new StandardCalculationDisclosureFactory());

        return calculator.Calculate(finalEnergy, factorSet);
    }
}
