using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class SystemEnergyFinalEnergyAggregatorTests
{
    private readonly SystemEnergyFinalEnergyAggregator _aggregator = new(new StandardCalculationDisclosureFactory());

    [Fact]
    public void AggregatesFinalEnergyByCarrier()
    {
        var handoff = SystemEnergyTestData.CreateGenerationHandoff(heatingHourlyLoad: 1.5);
        var gasGenerator = CreateGeneratorResult("G1", SystemEnergyCarrier.NaturalGas, suppliedPerHour: 1.0, finalPerHour: 1.0);
        var electricGenerator = CreateGeneratorResult("G2", SystemEnergyCarrier.Electricity, suppliedPerHour: 0.5, finalPerHour: 0.5);

        var result = _aggregator.Aggregate("CALC-A1", handoff, [gasGenerator, electricGenerator], null);

        Assert.Equal(8760.0, result.AnnualFinalEnergyByCarrierKWh[SystemEnergyCarrier.NaturalGas], 6);
        Assert.Equal(4380.0, result.AnnualFinalEnergyByCarrierKWh[SystemEnergyCarrier.Electricity], 6);
    }

    [Fact]
    public void AggregatesEndUseSuppliedAndUnmetLoad()
    {
        var handoff = SystemEnergyTestData.CreateGenerationHandoff(heatingHourlyLoad: 10.0);
        var generator = CreateGeneratorResult("G1", SystemEnergyCarrier.NaturalGas, suppliedPerHour: 8.0, finalPerHour: 8.0);

        var result = _aggregator.Aggregate("CALC-A2", handoff, [generator], null);
        var endUse = Assert.Single(result.EndUses);

        Assert.Equal(2.0, endUse.HourlyUnmetSystemLoadKWh8760[0], 6);
        Assert.Equal(17520.0, endUse.AnnualUnmetSystemLoadKWh, 6);
    }

    [Fact]
    public void IncludesAuxiliaryElectricity()
    {
        var handoff = SystemEnergyTestData.CreateGenerationHandoff(heatingHourlyLoad: 1.0);
        var generator = CreateGeneratorResult("G1", SystemEnergyCarrier.NaturalGas, suppliedPerHour: 1.0, finalPerHour: 1.0, auxiliaryPerHour: 0.1);

        var result = _aggregator.Aggregate("CALC-A3", handoff, [generator], null);

        Assert.Equal(876.0, result.AnnualTotalAuxiliaryElectricityKWh, 6);
        Assert.True(result.AnnualFinalEnergyByCarrierKWh[SystemEnergyCarrier.Electricity] >= 876.0);
    }

    [Fact]
    public void StatusCalculatedWhenNoUnmetLoad()
    {
        var handoff = SystemEnergyTestData.CreateGenerationHandoff(heatingHourlyLoad: 1.0);
        var generator = CreateGeneratorResult("G1", SystemEnergyCarrier.NaturalGas, suppliedPerHour: 1.0, finalPerHour: 1.0);

        var result = _aggregator.Aggregate("CALC-A4", handoff, [generator], null);

        Assert.Equal(SystemEnergyFinalEnergyStatus.Calculated, result.Status);
    }

    [Fact]
    public void StatusPartiallyCalculatedWhenUnmetLoadExists()
    {
        var handoff = SystemEnergyTestData.CreateGenerationHandoff(heatingHourlyLoad: 1.0);
        var generator = CreateGeneratorResult("G1", SystemEnergyCarrier.NaturalGas, suppliedPerHour: 0.6, finalPerHour: 0.6);

        var result = _aggregator.Aggregate("CALC-A5", handoff, [generator], null);

        Assert.Equal(SystemEnergyFinalEnergyStatus.PartiallyCalculated, result.Status);
    }

    private static SystemEnergyGeneratorResult CreateGeneratorResult(
        string generatorId,
        SystemEnergyCarrier carrier,
        double suppliedPerHour,
        double finalPerHour,
        double auxiliaryPerHour = 0.0)
    {
        var supplied = SystemEnergyTestData.HourlyConstant(suppliedPerHour);
        var final = SystemEnergyTestData.HourlyConstant(finalPerHour);
        var aux = SystemEnergyTestData.HourlyConstant(auxiliaryPerHour);

        return new SystemEnergyGeneratorResult(
            GeneratorId: generatorId,
            Name: generatorId,
            GeneratorKind: SystemEnergyGeneratorKind.Boiler,
            CalculationMode: SystemEnergyGeneratorCalculationMode.FixedEfficiency,
            FinalEnergyCarrier: carrier,
            ServedEndUses: [SystemEnergyEndUse.SpaceHeating],
            HourlyDispatch: [],
            HourlySuppliedSystemLoadByEndUseKWh8760: new Dictionary<SystemEnergyEndUse, IReadOnlyList<double>>
            {
                [SystemEnergyEndUse.SpaceHeating] = supplied
            },
            HourlyFinalEnergyByEndUseKWh8760: new Dictionary<SystemEnergyEndUse, IReadOnlyList<double>>
            {
                [SystemEnergyEndUse.SpaceHeating] = final
            },
            HourlyTotalFinalEnergyKWh8760: final,
            HourlyTotalAuxiliaryElectricityKWh8760: aux,
            AnnualSuppliedSystemLoadKWh: supplied.Sum(),
            AnnualFinalEnergyKWh: final.Sum(),
            AnnualAuxiliaryElectricityKWh: aux.Sum(),
            MonthlyFinalEnergyKWh: AggregateMonthly(final),
            MonthlyAuxiliaryElectricityKWh: AggregateMonthly(aux),
            Status: SystemEnergyFinalEnergyStatus.Calculated,
            Diagnostics: []);
    }

    private static IReadOnlyList<double> AggregateMonthly(IReadOnlyList<double> hourly)
    {
        var daysPerMonth = new[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        var monthly = new double[12];
        var offset = 0;
        for (var month = 0; month < 12; month++)
        {
            var hours = daysPerMonth[month] * 24;
            monthly[month] = hourly.Skip(offset).Take(hours).Sum();
            offset += hours;
        }

        return monthly;
    }
}
