using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

internal sealed class Iso52016HourlyResultComposer
{
    public Iso52016AnnualEnergyNeedResult ComposeAnnualResult(
        Building building,
        int weatherYear,
        IReadOnlyCollection<Iso52016ZoneHourlyEnergyNeed> zoneHourlyResults,
        IReadOnlyCollection<Iso52016RoomHourlyEnergyNeed> roomHourlyResults)
    {
        var groupedHourlyResults = zoneHourlyResults
            .GroupBy(hour => hour.HourOfYear)
            .Select(group => new Iso52016HourlyEnergyNeed(
                HourOfYear: group.Key,
                Month: Iso52016HourlyCalculatorMath.GetMonth(group.Key),
                HeatingLoadW: Iso52016HourlyCalculatorMath.Round(group.Sum(hour => hour.HeatingLoadW)),
                CoolingLoadW: Iso52016HourlyCalculatorMath.Round(group.Sum(hour => hour.CoolingLoadW)),
                OperativeTemperatureC: Iso52016HourlyCalculatorMath.Round(group.Average(hour => hour.OperativeTemperatureC)),
                OutdoorTemperatureC: Iso52016HourlyCalculatorMath.Round(group.Average(hour => hour.OutdoorTemperatureC)),
                InternalGainsW: Iso52016HourlyCalculatorMath.Round(group.Sum(hour => hour.InternalGainsW)),
                SolarGainsW: Iso52016HourlyCalculatorMath.Round(group.Sum(hour => hour.SolarGainsW)),
                TransmissionW: Iso52016HourlyCalculatorMath.Round(group.Sum(hour => hour.TransmissionW)),
                VentilationW: Iso52016HourlyCalculatorMath.Round(group.Sum(hour => hour.VentilationW)),
                InfiltrationW: Iso52016HourlyCalculatorMath.Round(group.Sum(hour => hour.InfiltrationW)),
                GroundW: Iso52016HourlyCalculatorMath.Round(group.Sum(hour => hour.GroundW)),
                TransmissionBalanceW: Iso52016HourlyCalculatorMath.Round(group.Sum(hour => hour.TransmissionBalanceW)),
                VentilationBalanceW: Iso52016HourlyCalculatorMath.Round(group.Sum(hour => hour.VentilationBalanceW)),
                InfiltrationBalanceW: Iso52016HourlyCalculatorMath.Round(group.Sum(hour => hour.InfiltrationBalanceW)),
                GroundBalanceW: Iso52016HourlyCalculatorMath.Round(group.Sum(hour => hour.GroundBalanceW)),
                MechanicalVentilationW: Iso52016HourlyCalculatorMath.Round(group.Sum(hour => hour.MechanicalVentilationW)),
                NaturalVentilationW: Iso52016HourlyCalculatorMath.Round(group.Sum(hour => hour.NaturalVentilationW)),
                MechanicalVentilationBalanceW: Iso52016HourlyCalculatorMath.Round(group.Sum(hour => hour.MechanicalVentilationBalanceW)),
                NaturalVentilationBalanceW: Iso52016HourlyCalculatorMath.Round(group.Sum(hour => hour.NaturalVentilationBalanceW))))
            .OrderBy(hour => hour.HourOfYear)
            .ToArray();

        var monthlyResults = groupedHourlyResults
            .GroupBy(hour => hour.Month)
            .Select(group => new Iso52016MonthlyEnergyNeed(
                group.Key,
                HeatingDemandKWh: Iso52016HourlyCalculatorMath.Round(group.Sum(hour => hour.HeatingLoadW) / 1000.0),
                CoolingDemandKWh: Iso52016HourlyCalculatorMath.Round(group.Sum(hour => hour.CoolingLoadW) / 1000.0)))
            .OrderBy(month => month.Month)
            .ToArray();

        return new Iso52016AnnualEnergyNeedResult(
            building.Id,
            building.Name,
            weatherYear,
            groupedHourlyResults,
            monthlyResults,
            AnnualHeatingDemandKWh: Iso52016HourlyCalculatorMath.Round(monthlyResults.Sum(month => month.HeatingDemandKWh)),
            AnnualCoolingDemandKWh: Iso52016HourlyCalculatorMath.Round(monthlyResults.Sum(month => month.CoolingDemandKWh)),
            Breakdown: new Iso52016EnergyBalanceBreakdown(
                SolarGainsKWh: Iso52016HourlyCalculatorMath.Round(groupedHourlyResults.Sum(hour => hour.SolarGainsW) / 1000.0),
                InternalGainsKWh: Iso52016HourlyCalculatorMath.Round(groupedHourlyResults.Sum(hour => hour.InternalGainsW) / 1000.0),
                HeatingInputKWh: Iso52016HourlyCalculatorMath.Round(monthlyResults.Sum(month => month.HeatingDemandKWh)),
                CoolingExtractedKWh: Iso52016HourlyCalculatorMath.Round(monthlyResults.Sum(month => month.CoolingDemandKWh))),
            ZoneHourlyResults: zoneHourlyResults.ToArray(),
            RoomHourlyResults: roomHourlyResults.ToArray());
    }
}
