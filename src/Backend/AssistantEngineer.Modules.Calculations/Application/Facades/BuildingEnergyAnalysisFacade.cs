using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
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
    private readonly IIso52016BuildingEnergySimulationApplicationService _iso52016Simulation;

    public BuildingEnergyAnalysisFacade(
        BuildingPerformanceService performance,
        IIso52016BuildingEnergySimulationApplicationService iso52016Simulation)
    {
        _performance = performance;
        _iso52016Simulation = iso52016Simulation;
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

    public Task<Result<Iso52016BuildingEnergySimulationApplicationResult>> SimulateIso52016Async(
        int buildingId,
        Iso52016BuildingEnergySimulationCommand request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Task.FromResult(
                Result<Iso52016BuildingEnergySimulationApplicationResult>.Validation(
                    "ISO 52016 simulation request is required."));
        }

        return _iso52016Simulation.SimulateAsync(
            new Iso52016BuildingEnergySimulationApplicationRequest(
                BuildingId: buildingId,
                LatitudeDegrees: request.LatitudeDegrees,
                LongitudeDegrees: request.LongitudeDegrees,
                TimeZoneOffset: request.TimeZoneOffset,
                WeatherYear: request.WeatherYear,
                Surfaces: request.Surfaces,
                GroundReflectance: request.GroundReflectance,
                GroundBoundaryTemperature: request.GroundBoundaryTemperature,
                Defaults: request.Defaults,
                HeatingSetpointOverrideC: request.HeatingSetpointOverrideC,
                CoolingSetpointOverrideC: request.CoolingSetpointOverrideC,
                HeatBalanceOptions: request.HeatBalanceOptions,
                SimulationEngine: request.SimulationEngine),
            cancellationToken);
    }}