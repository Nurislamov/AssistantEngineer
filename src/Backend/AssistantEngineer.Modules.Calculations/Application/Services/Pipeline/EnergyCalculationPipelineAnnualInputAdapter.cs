using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Pipeline;

internal sealed class EnergyCalculationPipelineAnnualInputAdapter
{
    private const string TrueHourlySimulationSource = HourlySimulationToAnnualEnergyInputMapper.TrueHourlySimulationSource;
    private const string MonthlyBalanceAdapterSource = "MonthlyBalanceAdapter";
    private const string ExternalReferenceValidationAnnualAggregationAdapter = "ExternalReferenceValidationAnnualAggregationAdapter";

    private readonly TimeProvider _timeProvider;

    public EnergyCalculationPipelineAnnualInputAdapter(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public AnnualEnergyAdapterInput BuildAnnualEnergyInput(
        Building building,
        BuildingEnergyBalanceResult source)
    {
        if (source.HourlyBalanceRecords.Count > 0)
        {
            var mapping = new HourlySimulationToAnnualEnergyInputMapper().Map(
                building.Id,
                building.Name,
                CalculateBuildingArea(building),
                _timeProvider.GetUtcNow().Year,
                source.HourlyBalanceRecords,
                $"Building {building.Id} application energy balance");

            return new AnnualEnergyAdapterInput(
                mapping.Input,
                TrueHourlySimulationSource,
                mapping.IsTrueHourly8760,
                mapping.HourlyRecordCount,
                mapping.Diagnostics);
        }

        var balances = source.MonthlyBalances.OrderBy(balance => balance.Month).ToArray();
        var hours = new List<AnnualEnergyBalanceHourInput>(balances.Length);
        var diagnosticsForAdapter = new List<CalculationDiagnostic>
        {
            new(
                CalculationDiagnosticSeverity.Info,
                "AnnualEnergy.InternalGainScheduleUnavailableInMonthlyAdapter",
                "Monthly balance adapter does not expose hourly internal gains schedules; annual internal gains are not expanded from room schedules in this path.",
                $"Building {building.Id} application energy balance")
        };

        foreach (var balance in balances)
        {
            var duration = HoursInMonth(balance.Month);
            hours.Add(new AnnualEnergyBalanceHourInput(
                HourIndex: MonthStartHour(balance.Month),
                Month: balance.Month,
                HeatingLoadW: duration > 0 ? balance.HeatingDemandKWh * 1000.0 / duration : 0,
                CoolingLoadW: duration > 0 ? balance.CoolingDemandKWh * 1000.0 / duration : 0,
                HourDurationH: duration));
        }

        return new AnnualEnergyAdapterInput(
            new AnnualEnergyBalanceInput(
                BuildingId: building.Id,
                BuildingName: building.Name,
                BuildingAreaM2: CalculateBuildingArea(building),
                Year: _timeProvider.GetUtcNow().Year,
                Hours: hours,
                UsesSyntheticWeather: true,
                WeatherSource: MonthlyBalanceAdapterSource,
                DiagnosticsContext: $"Building {building.Id} application energy balance",
                EnergyDataSource: MonthlyBalanceAdapterSource,
                IsTrueHourly8760: false,
                ActualMethod: ExternalReferenceValidationAnnualAggregationAdapter),
            MonthlyBalanceAdapterSource,
            IsTrueHourly8760: false,
            HourlyRecordCount: hours.Count,
            diagnosticsForAdapter);
    }

    private static double CalculateBuildingArea(Building building) =>
        building.Floors.SelectMany(floor => floor.Rooms).Sum(room => room.Area.SquareMeters);

    private static int HoursInMonth(int month)
    {
        var days = month switch
        {
            1 or 3 or 5 or 7 or 8 or 10 or 12 => 31,
            4 or 6 or 9 or 11 => 30,
            2 => 28,
            _ => 30
        };

        return days * 24;
    }

    private static int MonthStartHour(int month)
    {
        var hour = 0;
        for (var current = 1; current < month; current++)
            hour += HoursInMonth(current);

        return hour;
    }
}
