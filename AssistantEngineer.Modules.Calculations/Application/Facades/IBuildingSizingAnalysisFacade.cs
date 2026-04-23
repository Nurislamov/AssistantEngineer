using AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public interface IBuildingSizingAnalysisFacade
{
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

    Task<Result<BuildingEquipmentRecommendationReportResponse>> BuildEquipmentRecommendationReportAsync(
        int buildingId,
        int? year,
        EquipmentRecommendationReportRequest request,
        CancellationToken cancellationToken);

    Task<Result<BuildingEquipmentRecommendationComparisonReportResponse>> BuildEquipmentRecommendationComparisonReportAsync(
        int buildingId,
        int? year,
        EquipmentRecommendationComparisonReportRequest request,
        CancellationToken cancellationToken);
}