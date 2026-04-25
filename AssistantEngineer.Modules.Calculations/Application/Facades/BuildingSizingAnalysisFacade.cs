using AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;
using AssistantEngineer.Modules.Calculations.Application.Services.Sizing;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public sealed class BuildingSizingAnalysisFacade : IBuildingSizingAnalysisFacade
{
    private readonly BuildingPeakSizingService _peakService;
    private readonly BuildingReferenceDesignDayService _referenceDayService;
    private readonly BuildingSyntheticDesignDayService _syntheticDayService;
    private readonly BuildingAutosizingService _autosizingService;
    private readonly BuildingCatalogAutosizingService _catalogAutosizingService;
    private readonly EquipmentRecommendationService _equipmentRecommendationService;
    private readonly EquipmentRecommendationComparisonService _comparisonService;
    private readonly EquipmentRecommendationReportService _reportService;
    private readonly EquipmentRecommendationComparisonReportService _comparisonReportService;

    public BuildingSizingAnalysisFacade(
        BuildingPeakSizingService peakService,
        BuildingReferenceDesignDayService referenceDayService,
        BuildingSyntheticDesignDayService syntheticDayService,
        BuildingAutosizingService autosizingService,
        BuildingCatalogAutosizingService catalogAutosizingService,
        EquipmentRecommendationService equipmentRecommendationService,
        EquipmentRecommendationComparisonService comparisonService,
        EquipmentRecommendationReportService reportService,
        EquipmentRecommendationComparisonReportService comparisonReportService)
    {
        _peakService = peakService;
        _referenceDayService = referenceDayService;
        _syntheticDayService = syntheticDayService;
        _autosizingService = autosizingService;
        _catalogAutosizingService = catalogAutosizingService;
        _equipmentRecommendationService = equipmentRecommendationService;
        _comparisonService = comparisonService;
        _reportService = reportService;
        _comparisonReportService = comparisonReportService;
    }

    public Task<Result<BuildingPeakSizingResponse>> CalculatePeakLoadsAsync(
        int buildingId,
        int? year,
        PeakSizingRequest request,
        CancellationToken cancellationToken) =>
        _peakService.CalculateAsync(buildingId, year, request, cancellationToken);

    public Task<Result<BuildingReferenceDesignDayResponse>> CalculateReferenceDesignDayAsync(
        int buildingId,
        int? year,
        ReferenceDesignDayRequest request,
        CancellationToken cancellationToken) =>
        _referenceDayService.CalculateAsync(buildingId, year, request, cancellationToken);

    public Task<Result<BuildingSyntheticDesignDayResponse>> CalculateSyntheticDesignDayAsync(
        int buildingId,
        SyntheticDesignDayRequest request,
        CancellationToken cancellationToken) =>
        _syntheticDayService.CalculateAsync(buildingId, request, cancellationToken);

    public Task<Result<BuildingAutosizingResponse>> CalculateAutosizingAsync(
        int buildingId,
        int? year,
        AutosizingRequest request,
        CancellationToken cancellationToken) =>
        _autosizingService.CalculateAsync(buildingId, year, request, cancellationToken);

    public Task<Result<BuildingCatalogAutosizingResponse>> CalculateCatalogAutosizingAsync(
        int buildingId,
        int? year,
        CatalogAutosizingRequest request,
        CancellationToken cancellationToken) =>
        _catalogAutosizingService.CalculateAsync(buildingId, year, request, cancellationToken);

    public Task<Result<BuildingEquipmentRecommendationResponse>> CalculateEquipmentRecommendationsAsync(
        int buildingId,
        int? year,
        EquipmentRecommendationRequest request,
        CancellationToken cancellationToken) =>
        _equipmentRecommendationService.CalculateAsync(buildingId, year, request, cancellationToken);

    public Task<Result<BuildingEquipmentRecommendationComparisonResponse>> CompareEquipmentRecommendationsAsync(
        int buildingId,
        int? year,
        EquipmentRecommendationComparisonRequest request,
        CancellationToken cancellationToken) =>
        _comparisonService.CompareAsync(buildingId, year, request, cancellationToken);

    public Task<Result<BuildingEquipmentRecommendationReportResponse>> BuildEquipmentRecommendationReportAsync(
        int buildingId,
        int? year,
        EquipmentRecommendationReportRequest request,
        CancellationToken cancellationToken) =>
        _reportService.BuildAsync(buildingId, year, request, cancellationToken);

    public Task<Result<BuildingEquipmentRecommendationComparisonReportResponse>> BuildEquipmentRecommendationComparisonReportAsync(
        int buildingId,
        int? year,
        EquipmentRecommendationComparisonReportRequest request,
        CancellationToken cancellationToken) =>
        _comparisonReportService.BuildAsync(buildingId, year, request, cancellationToken);
}