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
using AssistantEngineer.Modules.Calculations.Application.Mappers;
using AssistantEngineer.Modules.Calculations.Application.Services.Buildings;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.Floors;
using AssistantEngineer.Modules.Calculations.Application.Services.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Services.Rooms;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public sealed class CalculationsFacade : ICalculationsFacade
{
    private readonly BuildingCoolingLoadService _buildingCooling;
    private readonly BuildingHeatingLoadService _buildingHeating;
    private readonly BuildingEnergyBalanceService _buildingEnergyBalance;
    private readonly FloorCalculationService _floorCalculation;
    private readonly RoomCalculationService _roomCalculation;
    private readonly NaturalVentilationPreviewService _naturalVentilationPreview;
    private readonly DomesticHotWaterDemandService _domesticHotWater;
    private readonly En16798ProfileService _profiles;
    private readonly IBuildingEnergyAnalysisFacade _energyAnalysis;
    private readonly IBuildingComfortAnalysisFacade _comfortAnalysis;
    private readonly IBuildingSizingAnalysisFacade _sizingAnalysis;

    public CalculationsFacade(
        BuildingCoolingLoadService buildingCooling,
        BuildingHeatingLoadService buildingHeating,
        BuildingEnergyBalanceService buildingEnergyBalance,
        FloorCalculationService floorCalculation,
        RoomCalculationService roomCalculation,
        NaturalVentilationPreviewService naturalVentilationPreview,
        DomesticHotWaterDemandService domesticHotWater,
        En16798ProfileService profiles,
        IBuildingEnergyAnalysisFacade energyAnalysis,
        IBuildingComfortAnalysisFacade comfortAnalysis,
        IBuildingSizingAnalysisFacade sizingAnalysis)
    {
        _buildingCooling = buildingCooling;
        _buildingHeating = buildingHeating;
        _buildingEnergyBalance = buildingEnergyBalance;
        _floorCalculation = floorCalculation;
        _roomCalculation = roomCalculation;
        _naturalVentilationPreview = naturalVentilationPreview;
        _domesticHotWater = domesticHotWater;
        _profiles = profiles;
        _energyAnalysis = energyAnalysis;
        _comfortAnalysis = comfortAnalysis;
        _sizingAnalysis = sizingAnalysis;
    }

    public Task<Result<BuildingCalculationResult>> CalculateBuildingCoolingLoadAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _buildingCooling.CalculateAsync(buildingId, method.ToDomain(), cancellationToken);

    public Task<Result<BuildingHeatingLoadResult>> CalculateBuildingHeatingLoadAsync(
        int buildingId,
        HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _buildingHeating.CalculateAsync(buildingId, method.ToDomain(), cancellationToken);

    public Task<Result<BuildingEnergyBalanceResult>> CalculateBuildingEnergyBalanceAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto coolingMethod,
        HeatingLoadCalculationMethodDto heatingMethod,
        CancellationToken cancellationToken) =>
        _buildingEnergyBalance.CalculateAsync(
            buildingId,
            coolingMethod.ToDomain(),
            heatingMethod.ToDomain(),
            cancellationToken);

    public Task<Result<FloorCalculationResult>> CalculateFloorCoolingLoadAsync(
        int floorId,
        CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _floorCalculation.CalculateAsync(floorId, method.ToDomain(), cancellationToken);

    public Task<Result<RoomCalculationResult>> CalculateRoomCoolingLoadAsync(
        int roomId,
        CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _roomCalculation.CalculateAsync(roomId, method.ToDomain(), cancellationToken);

    public Task<Result<RoomHeatingLoadResult>> CalculateRoomHeatingLoadAsync(
        int roomId,
        HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _roomCalculation.CalculateHeatingLoadAsync(roomId, method.ToDomain(), cancellationToken);

    public Task<Result<NaturalVentilationPreviewResponse>> PreviewNaturalVentilationAsync(
        int roomId,
        NaturalVentilationPreviewRequest request,
        CancellationToken cancellationToken) =>
        _naturalVentilationPreview.PreviewAsync(roomId, request, cancellationToken);

    public Result<DomesticHotWaterDemandResult> CalculateDomesticHotWaterDemand(
        DomesticHotWaterDemandRequest request) =>
        _domesticHotWater.Calculate(request);

    public En16798RoomUsageProfileResponse GetRoomUsageProfile(
        RoomTypeDto roomType,
        En16798ProfileCategory category) =>
        _profiles.GetRoomUsageProfile(roomType, category);

    public Task<Result<Iso52016EnergyBalanceBreakdown>> GetIso52016BreakdownAsync(
        int buildingId,
        int? year,
        CancellationToken cancellationToken) =>
        _energyAnalysis.GetIso52016BreakdownAsync(buildingId, year, cancellationToken);

    public Task<Result<EnergySignatureResult>> GetEnergySignatureAsync(
        int buildingId,
        int? year,
        double? heatingBaseTemperatureC,
        CancellationToken cancellationToken) =>
        _energyAnalysis.GetEnergySignatureAsync(buildingId, year, heatingBaseTemperatureC, cancellationToken);

    public Task<Result<HeatingSystemEnergyResult>> CalculateHeatingSystemEnergyAsync(
        int buildingId,
        int? year,
        HeatingSystemEnergyRequest request,
        CancellationToken cancellationToken) =>
        _energyAnalysis.CalculateHeatingSystemEnergyAsync(buildingId, year, request, cancellationToken);

    public Task<Result<CoolingSystemEnergyResult>> CalculateCoolingSystemEnergyAsync(
        int buildingId,
        int? year,
        CoolingSystemEnergyRequest request,
        CancellationToken cancellationToken) =>
        _energyAnalysis.CalculateCoolingSystemEnergyAsync(buildingId, year, request, cancellationToken);

    public Task<Result<BuildingEnergyPerformanceSummary>> CalculateSummaryAsync(
        int buildingId,
        int? year,
        BuildingEnergyPerformanceRequest request,
        CancellationToken cancellationToken) =>
        _energyAnalysis.CalculateSummaryAsync(buildingId, year, request, cancellationToken);

    public Task<Result<BuildingComfortMetricsResponse>> CalculateComfortMetricsAsync(
        int buildingId,
        int? year,
        BuildingComfortMetricsRequest request,
        CancellationToken cancellationToken) =>
        _comfortAnalysis.CalculateMetricsAsync(buildingId, year, request, cancellationToken);

    public Task<Result<BuildingZoneComfortMetricsResponse>> CalculateZoneComfortMetricsAsync(
        int buildingId,
        int? year,
        BuildingComfortMetricsRequest request,
        CancellationToken cancellationToken) =>
        _comfortAnalysis.CalculateZoneMetricsAsync(buildingId, year, request, cancellationToken);

    public Task<Result<BuildingRoomComfortMetricsResponse>> CalculateRoomComfortMetricsAsync(
        int buildingId,
        int? year,
        BuildingComfortMetricsRequest request,
        CancellationToken cancellationToken) =>
        _comfortAnalysis.CalculateRoomMetricsAsync(buildingId, year, request, cancellationToken);

    public Task<Result<BuildingPeakSizingResponse>> CalculatePeakLoadsAsync(
        int buildingId,
        int? year,
        PeakSizingRequest request,
        CancellationToken cancellationToken) =>
        _sizingAnalysis.CalculatePeakLoadsAsync(buildingId, year, request, cancellationToken);

    public Task<Result<BuildingReferenceDesignDayResponse>> CalculateReferenceDesignDayAsync(
        int buildingId,
        int? year,
        ReferenceDesignDayRequest request,
        CancellationToken cancellationToken) =>
        _sizingAnalysis.CalculateReferenceDesignDayAsync(buildingId, year, request, cancellationToken);

    public Task<Result<BuildingSyntheticDesignDayResponse>> CalculateSyntheticDesignDayAsync(
        int buildingId,
        SyntheticDesignDayRequest request,
        CancellationToken cancellationToken) =>
        _sizingAnalysis.CalculateSyntheticDesignDayAsync(buildingId, request, cancellationToken);

    public Task<Result<BuildingAutosizingResponse>> CalculateAutosizingAsync(
        int buildingId,
        int? year,
        AutosizingRequest request,
        CancellationToken cancellationToken) =>
        _sizingAnalysis.CalculateAutosizingAsync(buildingId, year, request, cancellationToken);

    public Task<Result<BuildingCatalogAutosizingResponse>> CalculateCatalogAutosizingAsync(
        int buildingId,
        int? year,
        CatalogAutosizingRequest request,
        CancellationToken cancellationToken) =>
        _sizingAnalysis.CalculateCatalogAutosizingAsync(buildingId, year, request, cancellationToken);

    public Task<Result<BuildingEquipmentRecommendationResponse>> CalculateEquipmentRecommendationsAsync(
        int buildingId,
        int? year,
        EquipmentRecommendationRequest request,
        CancellationToken cancellationToken) =>
        _sizingAnalysis.CalculateEquipmentRecommendationsAsync(buildingId, year, request, cancellationToken);

    public Task<Result<BuildingEquipmentRecommendationComparisonResponse>> CompareEquipmentRecommendationsAsync(
        int buildingId,
        int? year,
        EquipmentRecommendationComparisonRequest request,
        CancellationToken cancellationToken) =>
        _sizingAnalysis.CompareEquipmentRecommendationsAsync(buildingId, year, request, cancellationToken);
}
