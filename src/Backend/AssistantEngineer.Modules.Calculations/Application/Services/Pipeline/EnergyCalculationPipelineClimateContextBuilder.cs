using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Options;
using Microsoft.Extensions.Logging;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Pipeline;

internal sealed class EnergyCalculationPipelineClimateContextBuilder
{
    private readonly IAnnualClimateDataProvider? _annualClimateDataProvider;
    private readonly Iso52016EnergyNeedOptions _energyNeedOptions;
    private readonly ILogger<EnergyCalculationPipelineService> _logger;

    public EnergyCalculationPipelineClimateContextBuilder(
        IAnnualClimateDataProvider? annualClimateDataProvider,
        Iso52016EnergyNeedOptions energyNeedOptions,
        ILogger<EnergyCalculationPipelineService> logger)
    {
        _annualClimateDataProvider = annualClimateDataProvider;
        _energyNeedOptions = energyNeedOptions;
        _logger = logger;
    }

    public async Task<PipelineClimateContext> BuildClimateContextAsync(
        Building building,
        CancellationToken cancellationToken)
    {
        if (_annualClimateDataProvider is null ||
            building.ClimateZone is null)
        {
            return new PipelineClimateContext(null, IsCompleteAnnualClimateData: false);
        }

        try
        {
            var annualData = await _annualClimateDataProvider.GetForClimateZoneAsync(
                building.ClimateZone.Id,
                _energyNeedOptions.DefaultWeatherYear,
                cancellationToken);

            return new PipelineClimateContext(
                annualData,
                IsCompleteAnnualClimateData: HasCompleteAnnualClimateData(annualData));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Annual climate data was unavailable for building {BuildingId}; Energy Calculation Parity pipeline will use documented design-point fallbacks.",
                building.Id);
            return new PipelineClimateContext(null, IsCompleteAnnualClimateData: false);
        }
    }

    private static bool HasCompleteAnnualClimateData(
        AnnualClimateData? annualData) =>
        annualData?.HourlyData
            .Select(hour => hour.HourOfYear)
            .Distinct()
            .Count() == 8760;
}
