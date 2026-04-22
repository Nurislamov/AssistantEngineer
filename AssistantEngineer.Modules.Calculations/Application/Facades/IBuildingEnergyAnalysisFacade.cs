using AssistantEngineer.Modules.Calculations.Application.Contracts.Analytics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.CoolingSystems;
using AssistantEngineer.Modules.Calculations.Application.Contracts.HeatingSystems;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Performance;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public interface IBuildingEnergyAnalysisFacade
{
    Task<Result<Iso52016EnergyBalanceBreakdown>> GetIso52016BreakdownAsync(
        int buildingId,
        int? year,
        CancellationToken cancellationToken);

    Task<Result<EnergySignatureResult>> GetEnergySignatureAsync(
        int buildingId,
        int? year,
        double? heatingBaseTemperatureC,
        CancellationToken cancellationToken);

    Task<Result<HeatingSystemEnergyResult>> CalculateHeatingSystemEnergyAsync(
        int buildingId,
        int? year,
        HeatingSystemEnergyRequest request,
        CancellationToken cancellationToken);

    Task<Result<CoolingSystemEnergyResult>> CalculateCoolingSystemEnergyAsync(
        int buildingId,
        int? year,
        CoolingSystemEnergyRequest request,
        CancellationToken cancellationToken);

    Task<Result<BuildingEnergyPerformanceSummary>> CalculateSummaryAsync(
        int buildingId,
        int? year,
        BuildingEnergyPerformanceRequest request,
        CancellationToken cancellationToken);
}
