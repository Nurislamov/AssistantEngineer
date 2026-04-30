using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;

public sealed class Iso52016BuildingEnergyCalculator : IBuildingEnergyCalculator
{
    private readonly IRoomCoolingLoadCalculator _coolingCalculator;
    private readonly IRoomHeatingLoadCalculator _heatingCalculator;
    private readonly IClimateDataRepository _climateDataRepository;
    private readonly Iso52016HourlySteadyStateCalculator? _annualEnergyNeedCalculator;
    private readonly ILogger<Iso52016BuildingEnergyCalculator> _logger;

    public Iso52016BuildingEnergyCalculator(
        IRoomCoolingLoadCalculator coolingCalculator,
        IRoomHeatingLoadCalculator heatingCalculator,
        IClimateDataRepository climateDataRepository,
        Iso52016HourlySteadyStateCalculator? annualEnergyNeedCalculator = null,
        ILogger<Iso52016BuildingEnergyCalculator>? logger = null)
    {
        _coolingCalculator = coolingCalculator;
        _heatingCalculator = heatingCalculator;
        _climateDataRepository = climateDataRepository;
        _annualEnergyNeedCalculator = annualEnergyNeedCalculator;
        _logger = logger ?? NullLogger<Iso52016BuildingEnergyCalculator>.Instance;
    }

    public async Task<BuildingEnergyBalanceResult> CalculateAsync(
        Building building,
        CoolingLoadCalculationMethod coolingMethod,
        HeatingLoadCalculationMethod heatingMethod,
        CalculationPreferences? preferences = null,
        CancellationToken cancellationToken = default)
    {
        var climateZone = building.ClimateZone ?? throw new InvalidOperationException("Building has no climate zone.");
        _logger.LogInformation(
            "Energy balance calculation started for building {BuildingId} with climate zone {ClimateZoneId}.",
            building.Id,
            climateZone.Id);

        var result = new BuildingEnergyBalanceResult
        {
            BuildingId = building.Id,
            BuildingName = building.Name,
            CoolingCalculationMethod = coolingMethod.ToString(),
            HeatingCalculationMethod = heatingMethod.ToString(),
            RequestedCoolingMethod = coolingMethod.ToString(),
            RequestedHeatingMethod = heatingMethod.ToString()
        };

        double totalCooling = 0;
        double totalHeating = 0;

        if (coolingMethod == CoolingLoadCalculationMethod.Iso52016 &&
            _annualEnergyNeedCalculator is not null)
        {
            var annualEnergyNeeds = await _annualEnergyNeedCalculator.CalculateBuildingEnergyNeedsAsync(
                building,
                preferences,
                cancellationToken: cancellationToken);

            if (annualEnergyNeeds is not null)
            {
                result.CoolingCalculationMethod = "ISO52016InspiredHourlyAnalysis";
                result.HeatingCalculationMethod = "ISO52016InspiredHourlyAnalysis";
                result.ActualMethod = "ISO52016InspiredHourlyAnalysis";
                result.CalculationMethodLabel = "ISO52016InspiredHourlyAnalysis";
                result.EnergyDataSource = HourlySimulationToAnnualEnergyInputMapper.TrueHourlySimulationSource;
                result.IsTrueHourly8760 = annualEnergyNeeds.HourlyResults.Count == 8760;
                result.HourlyRecordCount = annualEnergyNeeds.HourlyResults.Count;
                result.Diagnostics.Add(new CalculationDiagnostic(
                    result.IsTrueHourly8760
                        ? CalculationDiagnosticSeverity.Info
                        : CalculationDiagnosticSeverity.Warning,
                    result.IsTrueHourly8760
                        ? "EnergyBalance.TrueHourlySimulationSource"
                        : "EnergyBalance.PartialHourlySimulationSource",
                    result.IsTrueHourly8760
                        ? "Energy balance source is true hourly simulation records."
                        : "Energy balance source is hourly simulation records, but the input is not a full 8760-hour year.",
                    $"Building {building.Id} ISO52016InspiredHourlyAnalysis"));

                foreach (var monthlyNeed in annualEnergyNeeds.MonthlyResults)
                {
                    result.MonthlyBalances.Add(new MonthlyEnergyBalance
                    {
                        Month = monthlyNeed.Month,
                        CoolingDemandKWh = monthlyNeed.CoolingDemandKWh,
                        HeatingDemandKWh = monthlyNeed.HeatingDemandKWh
                    });
                }

                var hourlyRecords = annualEnergyNeeds.HourlyResults
                    .OrderBy(hour => hour.HourOfYear)
                    .Select(hour => new AnnualEnergyBalanceHourInput(
                        HourIndex: hour.HourOfYear,
                        Month: hour.Month,
                        HeatingLoadW: hour.HeatingLoadW,
                        CoolingLoadW: hour.CoolingLoadW,
                        TransmissionW: hour.TransmissionW,
                        VentilationW: hour.VentilationW,
                        InfiltrationW: hour.InfiltrationW,
                        SolarGainsW: hour.SolarGainsW,
                        InternalGainsW: hour.InternalGainsW,
                        GroundW: hour.GroundW,
                        HourDurationH: 1.0))
                    .ToList();
                var hourlyMapping = new HourlySimulationToAnnualEnergyInputMapper().Map(
                    building.Id,
                    building.Name,
                    CalculateBuildingArea(building),
                    annualEnergyNeeds.Year,
                    hourlyRecords,
                    $"Building {building.Id} ISO52016InspiredHourlyAnalysis");
                result.HourlyBalanceRecords = hourlyMapping.Input.Hours.ToList();
                result.Diagnostics.AddRange(hourlyMapping.Diagnostics);

                result.AnnualCoolingDemandKWh = annualEnergyNeeds.AnnualCoolingDemandKWh;
                result.AnnualHeatingDemandKWh = annualEnergyNeeds.AnnualHeatingDemandKWh;
                result.AnnualTotalDemandKWh = Math.Round(
                    result.AnnualCoolingDemandKWh + result.AnnualHeatingDemandKWh,
                    2,
                    MidpointRounding.AwayFromZero);

                _logger.LogInformation(
                    "Calculated ISO52016-inspired annual energy needs for building {BuildingId}: heating {AnnualHeatingDemandKWh} kWh, cooling {AnnualCoolingDemandKWh} kWh.",
                    building.Id,
                    result.AnnualHeatingDemandKWh,
                    result.AnnualCoolingDemandKWh);
                return result;
            }
        }

        var climateDataMonths = await _climateDataRepository.GetAvailableMonthsForClimateZoneAsync(
            climateZone.Id,
            cancellationToken);

        if (climateDataMonths.Count == 0)
        {
            result.ActualMethod = "LegacyMonthlyEstimate";
            result.CalculationMethodLabel = "Legacy monthly estimate compatibility path";
            result.EnergyDataSource = "LegacyMonthlyEstimate";
            result.IsTrueHourly8760 = false;
            result.HourlyRecordCount = 0;
            result.Diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "EnergyBalance.LegacyMonthlyEstimateUnavailable",
                "No climate months were available for the legacy monthly estimate path.",
                $"Building {building.Id} legacy energy balance"));
            return result;
        }

        var roomEnergyEstimates = await CalculateRoomEnergyEstimatesAsync(
            building,
            coolingMethod,
            heatingMethod,
            preferences,
            cancellationToken);
        var monthlyCooling = roomEnergyEstimates.Sum(estimate => estimate.DesignDayCoolingDemandKWh);
        var monthlyHeating = roomEnergyEstimates.Sum(estimate => estimate.DesignMonthHeatingDemandKWh);

        foreach (var month in climateDataMonths)
        {
            totalCooling += monthlyCooling;
            totalHeating += monthlyHeating;

            result.MonthlyBalances.Add(new MonthlyEnergyBalance
            {
                Month = month,
                CoolingDemandKWh = Math.Round(monthlyCooling, 2, MidpointRounding.AwayFromZero),
                HeatingDemandKWh = Math.Round(monthlyHeating, 2, MidpointRounding.AwayFromZero)
            });

            _logger.LogDebug(
                "Calculated month {Month} energy balance for building {BuildingId}: cooling {CoolingDemandKWh} kWh, heating {HeatingDemandKWh} kWh.",
                month,
                building.Id,
                Math.Round(monthlyCooling, 2, MidpointRounding.AwayFromZero),
                Math.Round(monthlyHeating, 2, MidpointRounding.AwayFromZero));
        }

        result.AnnualCoolingDemandKWh = Math.Round(totalCooling, 2, MidpointRounding.AwayFromZero);
        result.AnnualHeatingDemandKWh = Math.Round(totalHeating, 2, MidpointRounding.AwayFromZero);
        result.AnnualTotalDemandKWh = Math.Round(
            result.AnnualCoolingDemandKWh + result.AnnualHeatingDemandKWh,
            2,
            MidpointRounding.AwayFromZero);
        result.ActualMethod = "LegacyMonthlyEstimate";
        result.CalculationMethodLabel = "Legacy monthly estimate compatibility path";
        result.EnergyDataSource = "LegacyMonthlyEstimate";
        result.IsTrueHourly8760 = false;
        result.HourlyRecordCount = 0;
        result.Diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Warning,
            "EnergyBalance.LegacyMonthlyEstimatePath",
            "Energy balance used the legacy monthly estimate compatibility path; this is not a true hourly 8760 simulation.",
            $"Building {building.Id} legacy energy balance"));

        _logger.LogInformation(
            "Energy balance calculation finished for building {BuildingId}: annual total {AnnualTotalDemandKWh} kWh.",
            building.Id,
            result.AnnualTotalDemandKWh);

        return result;
    }

    private async Task<IReadOnlyList<RoomEnergyEstimate>> CalculateRoomEnergyEstimatesAsync(
        Building building,
        CoolingLoadCalculationMethod coolingMethod,
        HeatingLoadCalculationMethod heatingMethod,
        CalculationPreferences? preferences,
        CancellationToken cancellationToken)
    {
        var estimates = new List<RoomEnergyEstimate>();
        foreach (var floor in building.Floors)
        foreach (var room in floor.Rooms)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var coolingResult = await _coolingCalculator.CalculateAsync(
                room,
                coolingMethod,
                preferences,
                cancellationToken);
            var heatingResult = await _heatingCalculator.CalculateAsync(
                room,
                heatingMethod,
                preferences,
                cancellationToken);

            estimates.Add(new RoomEnergyEstimate(
                DesignDayCoolingDemandKWh: coolingResult.HourlyHeatLoadW.Sum() / 1000.0,
                DesignMonthHeatingDemandKWh: heatingResult.TotalDesignHeatingLoadW * 24 * 30 / 1000.0));
        }

        return estimates;
    }

    private sealed record RoomEnergyEstimate(
        double DesignDayCoolingDemandKWh,
        double DesignMonthHeatingDemandKWh);

    private static double CalculateBuildingArea(Building building) =>
        building.Floors.SelectMany(floor => floor.Rooms).Sum(room => room.Area.SquareMeters);
}
