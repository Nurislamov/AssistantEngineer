using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Services.Buildings;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Analytics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.CoolingSystems;
using AssistantEngineer.Modules.Calculations.Application.Contracts.HeatingSystems;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Performance;
using AssistantEngineer.Modules.Calculations.Application.Services.Analytics;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingSystems;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingSystems;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Performance;

public sealed class BuildingPerformanceService
{
    private readonly IBuildingRepository _buildings;
    private readonly ICalculationPreferencesRepository _preferences;
    private readonly Iso52016HourlySteadyStateCalculator _iso52016;
    private readonly EnergySignatureService _energySignature;
    private readonly HeatingSystemEnergyService _heatingSystemEnergy;
    private readonly CoolingSystemEnergyService _coolingSystemEnergy;
    private readonly BuildingEnergyPerformanceSummaryService _performanceSummary;
    private readonly BuildingCalculationReadinessService _readiness;

    public BuildingPerformanceService(
        IBuildingRepository buildings,
        ICalculationPreferencesRepository preferences,
        Iso52016HourlySteadyStateCalculator iso52016,
        EnergySignatureService energySignature,
        HeatingSystemEnergyService heatingSystemEnergy,
        CoolingSystemEnergyService coolingSystemEnergy,
        BuildingEnergyPerformanceSummaryService performanceSummary,
        BuildingCalculationReadinessService readiness)
    {
        _buildings = buildings;
        _preferences = preferences;
        _iso52016 = iso52016;
        _energySignature = energySignature;
        _heatingSystemEnergy = heatingSystemEnergy;
        _coolingSystemEnergy = coolingSystemEnergy;
        _performanceSummary = performanceSummary;
        _readiness = readiness;
    }

    public async Task<Result<Iso52016EnergyBalanceBreakdown>> GetIso52016BreakdownAsync(
        int buildingId,
        int year,
        CancellationToken cancellationToken)
    {
        var energyNeed = await CalculateEnergyNeedAsync(buildingId, year, cancellationToken);
        if (energyNeed.IsFailure)
            return Result<Iso52016EnergyBalanceBreakdown>.Failure(energyNeed);

        return Result<Iso52016EnergyBalanceBreakdown>.Success(energyNeed.Value.Breakdown);
    }

    public async Task<Result<EnergySignatureResult>> GetEnergySignatureAsync(
        int buildingId,
        int year,
        double heatingBaseTemperatureC,
        CancellationToken cancellationToken)
    {
        var energyNeed = await CalculateEnergyNeedAsync(buildingId, year, cancellationToken);
        if (energyNeed.IsFailure)
            return Result<EnergySignatureResult>.Failure(energyNeed);

        return _energySignature.Calculate(
            energyNeed.Value,
            heatingBaseTemperatureC == 0 ? 18 : heatingBaseTemperatureC);
    }

    public async Task<Result<HeatingSystemEnergyResult>> CalculateHeatingSystemEnergyAsync(
        int buildingId,
        int year,
        HeatingSystemEnergyRequest request,
        CancellationToken cancellationToken)
    {
        var energyNeed = await CalculateEnergyNeedAsync(buildingId, year, cancellationToken);
        if (energyNeed.IsFailure)
            return Result<HeatingSystemEnergyResult>.Failure(energyNeed);

        return _heatingSystemEnergy.Calculate(energyNeed.Value, request);
    }

    public async Task<Result<CoolingSystemEnergyResult>> CalculateCoolingSystemEnergyAsync(
        int buildingId,
        int year,
        CoolingSystemEnergyRequest request,
        CancellationToken cancellationToken)
    {
        var energyNeed = await CalculateEnergyNeedAsync(buildingId, year, cancellationToken);
        if (energyNeed.IsFailure)
            return Result<CoolingSystemEnergyResult>.Failure(energyNeed);

        return _coolingSystemEnergy.Calculate(energyNeed.Value, request);
    }

    public async Task<Result<BuildingEnergyPerformanceSummary>> CalculateSummaryAsync(
        int buildingId,
        int year,
        BuildingEnergyPerformanceRequest request,
        CancellationToken cancellationToken)
    {
        var context = await CalculateEnergyNeedContextAsync(buildingId, year, cancellationToken);
        if (context.IsFailure)
            return Result<BuildingEnergyPerformanceSummary>.Failure(context);

        return _performanceSummary.Calculate(
            context.Value.Building,
            context.Value.EnergyNeed,
            request);
    }

    private async Task<Result<Iso52016AnnualEnergyNeedResult>> CalculateEnergyNeedAsync(
        int buildingId,
        int year,
        CancellationToken cancellationToken)
    {
        var readiness = await EnsureCalculationReadyAsync(buildingId, year, cancellationToken);
        if (readiness.IsFailure)
            return Result<Iso52016AnnualEnergyNeedResult>.Failure(readiness);

        var building = await _buildings.GetForCalculationAsync(buildingId, cancellationToken);
        if (building is null)
            return Result<Iso52016AnnualEnergyNeedResult>.NotFound($"Building with id {buildingId} not found.");

        var preferences = await _preferences.GetByProjectIdAsync(building.ProjectId, cancellationToken);
        var result = await _iso52016.CalculateBuildingEnergyNeedsAsync(
            building,
            preferences,
            year == 0 ? null : year,
            cancellationToken);

        return result is null
            ? Result<Iso52016AnnualEnergyNeedResult>.Validation("Complete annual climate data is required for ISO 52016 analysis.")
            : Result<Iso52016AnnualEnergyNeedResult>.Success(result);
    }

    private async Task<Result<BuildingEnergyNeedContext>> CalculateEnergyNeedContextAsync(
        int buildingId,
        int year,
        CancellationToken cancellationToken)
    {
        var readiness = await EnsureCalculationReadyAsync(buildingId, year, cancellationToken);
        if (readiness.IsFailure)
            return Result<BuildingEnergyNeedContext>.Failure(readiness);

        var building = await _buildings.GetForCalculationAsync(buildingId, cancellationToken);
        if (building is null)
            return Result<BuildingEnergyNeedContext>.NotFound($"Building with id {buildingId} not found.");

        var preferences = await _preferences.GetByProjectIdAsync(building.ProjectId, cancellationToken);
        var result = await _iso52016.CalculateBuildingEnergyNeedsAsync(
            building,
            preferences,
            year == 0 ? null : year,
            cancellationToken);

        return result is null
            ? Result<BuildingEnergyNeedContext>.Validation("Complete annual climate data is required for ISO 52016 analysis.")
            : Result<BuildingEnergyNeedContext>.Success(new BuildingEnergyNeedContext(building, result));
    }

    private async Task<Result> EnsureCalculationReadyAsync(
        int buildingId,
        int year,
        CancellationToken cancellationToken)
    {
        var report = await _readiness.CheckAsync(
            buildingId,
            year == 0 ? 2020 : year,
            cancellationToken);

        if (report.IsFailure)
            return Result.Failure(report.Error, report.ErrorType);

        var errors = report.Value.Issues
            .Where(issue => issue.Severity == BuildingCalculationReadinessSeverity.Error)
            .ToArray();

        if (errors.Length == 0)
            return Result.Success();

        return Result.Validation(
            "Building is not ready for calculation: " +
            string.Join("; ", errors.Select(issue => $"{issue.Path}: {issue.Message}")));
    }

    private sealed record BuildingEnergyNeedContext(
        Building Building,
        Iso52016AnnualEnergyNeedResult EnergyNeed);
}