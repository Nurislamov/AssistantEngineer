using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

internal static class SystemEnergyPrimaryTestData
{
    private static readonly StandardCalculationDisclosureFactory DisclosureFactory = new();

    public static SystemEnergyFinalEnergyResult CreateFinalEnergyResult(
        IReadOnlyDictionary<SystemEnergyCarrier, double> hourlyFinalEnergyByCarrier,
        IReadOnlyDictionary<SystemEnergyEndUse, IReadOnlyDictionary<SystemEnergyCarrier, double>>? hourlyFinalByEndUse = null,
        string calculationId = "PRIMARY-CALC-1")
    {
        var endUseCarrierData = hourlyFinalByEndUse
            ?? new Dictionary<SystemEnergyEndUse, IReadOnlyDictionary<SystemEnergyCarrier, double>>
            {
                [SystemEnergyEndUse.SpaceHeating] = hourlyFinalEnergyByCarrier
            };

        var endUses = endUseCarrierData.Select(endUseEntry =>
        {
            var finalByCarrier = endUseEntry.Value.ToDictionary(
                carrier => carrier.Key,
                carrier => (IReadOnlyList<double>)SystemEnergyTestData.HourlyConstant(carrier.Value),
                EqualityComparer<SystemEnergyCarrier>.Default);

            var totalHourly = new double[8760];
            foreach (var profile in finalByCarrier.Values)
            {
                for (var hour = 0; hour < 8760; hour++)
                {
                    totalHourly[hour] += profile[hour];
                }
            }

            var annualByCarrier = finalByCarrier.ToDictionary(
                carrier => carrier.Key,
                carrier => carrier.Value.Sum(),
                EqualityComparer<SystemEnergyCarrier>.Default);

            var monthlyByCarrier = finalByCarrier.ToDictionary(
                carrier => carrier.Key,
                carrier => AggregateMonthly(carrier.Value),
                EqualityComparer<SystemEnergyCarrier>.Default);

            return new SystemEnergyEndUseFinalEnergyResult(
                EndUse: endUseEntry.Key,
                HourlySystemLoadBeforeGenerationKWh8760: totalHourly,
                HourlySuppliedSystemLoadKWh8760: totalHourly,
                HourlyUnmetSystemLoadKWh8760: SystemEnergyTestData.HourlyConstant(0.0),
                HourlyFinalEnergyByCarrierKWh8760: finalByCarrier,
                HourlyAuxiliaryElectricityKWh8760: SystemEnergyTestData.HourlyConstant(0.0),
                AnnualSystemLoadBeforeGenerationKWh: totalHourly.Sum(),
                AnnualSuppliedSystemLoadKWh: totalHourly.Sum(),
                AnnualUnmetSystemLoadKWh: 0.0,
                AnnualFinalEnergyByCarrierKWh: annualByCarrier,
                AnnualAuxiliaryElectricityKWh: 0.0,
                MonthlySuppliedSystemLoadKWh: AggregateMonthly(totalHourly),
                MonthlyUnmetSystemLoadKWh: AggregateMonthly(SystemEnergyTestData.HourlyConstant(0.0)),
                MonthlyFinalEnergyByCarrierKWh: monthlyByCarrier,
                Diagnostics: []);
        }).ToArray();

        var hourlyByCarrier = hourlyFinalEnergyByCarrier.ToDictionary(
            carrier => carrier.Key,
            carrier => (IReadOnlyList<double>)SystemEnergyTestData.HourlyConstant(carrier.Value),
            EqualityComparer<SystemEnergyCarrier>.Default);

        var annualByCarrier = hourlyByCarrier.ToDictionary(
            carrier => carrier.Key,
            carrier => carrier.Value.Sum(),
            EqualityComparer<SystemEnergyCarrier>.Default);

        var monthlyByCarrier = hourlyByCarrier.ToDictionary(
            carrier => carrier.Key,
            carrier => AggregateMonthly(carrier.Value),
            EqualityComparer<SystemEnergyCarrier>.Default);

        var totalHourlyFinal = new double[8760];
        foreach (var profile in hourlyByCarrier.Values)
        {
            for (var hour = 0; hour < 8760; hour++)
            {
                totalHourlyFinal[hour] += profile[hour];
            }
        }

        return new SystemEnergyFinalEnergyResult(
            CalculationId: calculationId,
            Generators: [],
            EndUses: endUses,
            HourlyFinalEnergyByCarrierKWh8760: hourlyByCarrier,
            AnnualFinalEnergyByCarrierKWh: annualByCarrier,
            MonthlyFinalEnergyByCarrierKWh: monthlyByCarrier,
            HourlyTotalFinalEnergyKWh8760: totalHourlyFinal,
            HourlyTotalAuxiliaryElectricityKWh8760: SystemEnergyTestData.HourlyConstant(0.0),
            AnnualTotalFinalEnergyKWh: totalHourlyFinal.Sum(),
            AnnualTotalAuxiliaryElectricityKWh: 0.0,
            AnnualTotalUnmetSystemLoadKWh: 0.0,
            Status: SystemEnergyFinalEnergyStatus.Calculated,
            Disclosure: DisclosureFactory.CreateSystemEnergyEn15316Disclosure(),
            Diagnostics: []);
    }

    public static SystemEnergyFactorSet CreateFactorSet(
        IReadOnlyList<SystemEnergyPrimaryEnergyFactor> primaryFactors,
        IReadOnlyList<SystemEnergyEmissionFactor>? emissionFactors = null,
        string factorSetId = "FS-1") =>
        new(
            FactorSetId: factorSetId,
            PrimaryEnergyFactors: primaryFactors,
            EmissionFactors: emissionFactors ?? [],
            Region: null,
            Year: null,
            Source: "test",
            Diagnostics: []);

    public static SystemEnergyPrimaryEnergyFactor CreatePrimaryFactor(
        SystemEnergyCarrier carrier,
        double renewableFactor,
        double nonRenewableFactor,
        double totalFactor,
        SystemEnergyFactorSourceKind sourceKind = SystemEnergyFactorSourceKind.UserProvided) =>
        new(
            Carrier: carrier,
            RenewableFactor: renewableFactor,
            NonRenewableFactor: nonRenewableFactor,
            TotalFactor: totalFactor,
            SourceKind: sourceKind,
            Source: "test",
            Region: null,
            Year: null,
            Diagnostics: []);

    public static SystemEnergyEmissionFactor CreateEmissionFactor(
        SystemEnergyCarrier carrier,
        double kgPerKWh,
        SystemEnergyEmissionFactorKind factorKind = SystemEnergyEmissionFactorKind.CarbonDioxide,
        SystemEnergyFactorSourceKind sourceKind = SystemEnergyFactorSourceKind.UserProvided) =>
        new(
            Carrier: carrier,
            FactorKind: factorKind,
            KgPerKWh: kgPerKWh,
            SourceKind: sourceKind,
            Source: "test",
            Region: null,
            Year: null,
            Diagnostics: []);

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
