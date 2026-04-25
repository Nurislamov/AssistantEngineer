using AssistantEngineer.Modules.Calculations.Application.Contracts.Analytics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.CoolingSystems;
using AssistantEngineer.Modules.Calculations.Application.Contracts.HeatingSystems;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Performance;
using AssistantEngineer.Modules.Calculations.Application.Services.Performance;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public sealed class BuildingEnergyAnalysisFacade : IBuildingEnergyAnalysisFacade
{
    private readonly BuildingPerformanceService _performance;

    public BuildingEnergyAnalysisFacade(BuildingPerformanceService performance)
    {
        _performance = performance;
    }

    public Task<Result<Iso52016EnergyBalanceBreakdown>> GetIso52016BreakdownAsync(
        int buildingId,
        int? year,
        CancellationToken cancellationToken) =>
        _performance.GetIso52016BreakdownAsync(buildingId, year, cancellationToken);

    public Task<Result<Iso52016HourlyResultsResponse>> GetIso52016HourlyResultsAsync(
        int buildingId,
        int? year,
        int? month,
        CancellationToken cancellationToken) =>
        _performance.GetIso52016HourlyResultsAsync(buildingId, year, month, cancellationToken);

    public Task<Result<Iso52016MonthlyResultsResponse>> GetIso52016MonthlyResultsAsync(
        int buildingId,
        int? year,
        CancellationToken cancellationToken) =>
        _performance.GetIso52016MonthlyResultsAsync(buildingId, year, cancellationToken);

    public Task<Result<Iso52016MonthlyResultsResponse>> GetIso52016MonthlyMethodResultsAsync(
        int buildingId,
        int? year,
        CancellationToken cancellationToken) =>
        _performance.GetIso52016MonthlyMethodResultsAsync(buildingId, year, cancellationToken);

    public Task<Result<EnergySignatureResult>> GetEnergySignatureAsync(
        int buildingId,
        int? year,
        double? heatingBaseTemperatureC,
        CancellationToken cancellationToken) =>
        _performance.GetEnergySignatureAsync(buildingId, year, heatingBaseTemperatureC, cancellationToken);

    public Task<Result<HeatingSystemEnergyResult>> CalculateHeatingSystemEnergyAsync(
        int buildingId,
        int? year,
        HeatingSystemEnergyRequest request,
        CancellationToken cancellationToken) =>
        _performance.CalculateHeatingSystemEnergyAsync(buildingId, year, request, cancellationToken);

    public Task<Result<CoolingSystemEnergyResult>> CalculateCoolingSystemEnergyAsync(
        int buildingId,
        int? year,
        CoolingSystemEnergyRequest request,
        CancellationToken cancellationToken) =>
        _performance.CalculateCoolingSystemEnergyAsync(buildingId, year, request, cancellationToken);

    public Task<Result<BuildingEnergyPerformanceSummary>> CalculateSummaryAsync(
        int buildingId,
        int? year,
        BuildingEnergyPerformanceRequest request,
        CancellationToken cancellationToken) =>
        _performance.CalculateSummaryAsync(buildingId, year, request, cancellationToken);
}