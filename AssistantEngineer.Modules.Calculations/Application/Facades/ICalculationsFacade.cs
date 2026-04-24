using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Analytics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Comfort;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Contracts.CoolingSystems;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.HeatingSystems;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Performance;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public interface ICalculationsFacade
{
    Task<Result<BuildingCalculationResult>> CalculateBuildingCoolingLoadAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken);

    Task<Result<BuildingHeatingLoadResult>> CalculateBuildingHeatingLoadAsync(
        int buildingId,
        HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken);

    Task<Result<BuildingEnergyBalanceResult>> CalculateBuildingEnergyBalanceAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto coolingMethod,
        HeatingLoadCalculationMethodDto heatingMethod,
        CancellationToken cancellationToken);

    Task<Result<FloorCalculationResult>> CalculateFloorCoolingLoadAsync(
        int floorId,
        CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken);

    Task<Result<RoomCalculationResult>> CalculateRoomCoolingLoadAsync(
        int roomId,
        CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken);

    Task<Result<RoomHeatingLoadResult>> CalculateRoomHeatingLoadAsync(
        int roomId,
        HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken);

    Task<Result<NaturalVentilationPreviewResponse>> PreviewNaturalVentilationAsync(
        int roomId,
        NaturalVentilationPreviewRequest request,
        CancellationToken cancellationToken);

    Result<DomesticHotWaterDemandResult> CalculateDomesticHotWaterDemand(
        DomesticHotWaterDemandRequest request);

    En16798RoomUsageProfileResponse GetRoomUsageProfile(
        RoomTypeDto roomType,
        En16798ProfileCategory category);

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

    Task<Result<BuildingComfortMetricsResponse>> CalculateComfortMetricsAsync(
        int buildingId,
        int? year,
        BuildingComfortMetricsRequest request,
        CancellationToken cancellationToken);

    Task<Result<BuildingZoneComfortMetricsResponse>> CalculateZoneComfortMetricsAsync(
        int buildingId,
        int? year,
        BuildingComfortMetricsRequest request,
        CancellationToken cancellationToken);

    Task<Result<BuildingRoomComfortMetricsResponse>> CalculateRoomComfortMetricsAsync(
        int buildingId,
        int? year,
        BuildingComfortMetricsRequest request,
        CancellationToken cancellationToken);

    Task<Result<BuildingPeakSizingResponse>> CalculatePeakLoadsAsync(
        int buildingId,
        int? year,
        PeakSizingRequest request,
        CancellationToken cancellationToken);

    Task<Result<BuildingReferenceDesignDayResponse>> CalculateReferenceDesignDayAsync(
        int buildingId,
        int? year,
        ReferenceDesignDayRequest request,
        CancellationToken cancellationToken);

    Task<Result<BuildingSyntheticDesignDayResponse>> CalculateSyntheticDesignDayAsync(
        int buildingId,
        SyntheticDesignDayRequest request,
        CancellationToken cancellationToken);

    Task<Result<BuildingAutosizingResponse>> CalculateAutosizingAsync(
        int buildingId,
        int? year,
        AutosizingRequest request,
        CancellationToken cancellationToken);

    Task<Result<BuildingCatalogAutosizingResponse>> CalculateCatalogAutosizingAsync(
        int buildingId,
        int? year,
        CatalogAutosizingRequest request,
        CancellationToken cancellationToken);

    Task<Result<BuildingEquipmentRecommendationResponse>> CalculateEquipmentRecommendationsAsync(
        int buildingId,
        int? year,
        EquipmentRecommendationRequest request,
        CancellationToken cancellationToken);

    Task<Result<BuildingEquipmentRecommendationComparisonResponse>> CompareEquipmentRecommendationsAsync(
        int buildingId,
        int? year,
        EquipmentRecommendationComparisonRequest request,
        CancellationToken cancellationToken);
}
