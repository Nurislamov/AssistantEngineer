using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater.Iso12831;
using AssistantEngineer.Modules.Calculations.Application.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

public sealed class Iso12831DomesticHotWaterApplicationAdapter
{
    public Iso12831DomesticHotWaterInput MapToIsoInput(
        DomesticHotWaterDemandRequest request,
        DomesticHotWaterOptions options)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(options);

        return new Iso12831DomesticHotWaterInput(
            UsageCategory: options.DefaultUsageCategory,
            ReferenceMode: options.DefaultReferenceMode,
            PeopleCount: request.PeopleCount,
            EquivalentOccupants: 0.0,
            AreaM2: 0.0,
            UnitsCount: 0,
            LitersPerPersonDay: request.LitersPerPersonDay,
            LitersPerM2Day: 0.0,
            LitersPerUnitDay: 0.0,
            CustomDailyVolumeLiters: 0.0,
            HotWaterTemperatureC: request.HotWaterTemperatureC,
            ColdWaterTemperatureC: request.ColdWaterTemperatureC,
            DistributionLossFactor: request.DistributionLossFactor,
            StorageLossKWhPerDay: request.StorageLossKWhPerDay,
            CirculationLossKWhPerDay: request.CirculationLossKWhPerDay,
            IncludeHourlyProfile: request.IncludeHourlyProfile,
            Year: request.Year,
            HolidayDates: request.HolidayDates,
            DrawProfileKind: options.DefaultDrawProfileKind,
            WeekdayDrawProfile: request.WeekdayDrawProfile,
            WeekendDrawProfile: request.WeekendDrawProfile,
            CustomDrawProfile: null,
            UseTableDrivenReferenceData: false,
            TableDrivenUsageCategory: null);
    }

    public DomesticHotWaterDemandResult MapToCompatibilityResult(
        Iso12831DomesticHotWaterResult isoResult)
    {
        ArgumentNullException.ThrowIfNull(isoResult);

        var monthly = isoResult.MonthlyResults
            .Select(item => new DomesticHotWaterMonthlyDemand(
                Month: item.Month,
                VolumeLiters: item.VolumeLiters,
                EnergyKWh: item.TotalEnergyKWh))
            .ToArray();

        var hourly = isoResult.HourlyResults
            .Select(item => new DomesticHotWaterHourlyDemand(
                HourOfYear: item.HourOfYear,
                Month: item.Month,
                VolumeLiters: item.VolumeLiters,
                EnergyKWh: item.TotalEnergyKWh))
            .ToArray();

        return new DomesticHotWaterDemandResult(
            DailyVolumeLiters: isoResult.DailyVolumeLiters,
            DailyEnergyKWh: isoResult.DailyTotalEnergyKWh,
            MonthlyDemand: monthly,
            HourlyDemand: hourly,
            AnnualVolumeLiters: isoResult.AnnualVolumeLiters,
            AnnualEnergyKWh: isoResult.AnnualTotalEnergyKWh,
            Diagnostics: isoResult.Diagnostics.Select(item => item.Message).ToArray(),
            AssumptionsUsed: isoResult.AssumptionsUsed);
    }
}
