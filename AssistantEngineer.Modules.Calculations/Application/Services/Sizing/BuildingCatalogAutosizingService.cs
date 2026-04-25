using AssistantEngineer.Modules.Calculations.Application.Abstractions.Sizing;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Sizing;

public sealed class BuildingCatalogAutosizingService
{
    private readonly BuildingPeakSizingService _peakSizingService;
    private readonly ICoolingEquipmentCatalogSizingProvider _catalogProvider;
    private readonly CatalogAutosizingRankingService _rankingService;

    public BuildingCatalogAutosizingService(
        BuildingPeakSizingService peakSizingService,
        ICoolingEquipmentCatalogSizingProvider catalogProvider,
        CatalogAutosizingRankingService rankingService)
    {
        _peakSizingService = peakSizingService;
        _catalogProvider = catalogProvider;
        _rankingService = rankingService;
    }

    public async Task<Result<BuildingCatalogAutosizingResponse>> CalculateAsync(
        int buildingId,
        int? year,
        CatalogAutosizingRequest request,
        CancellationToken cancellationToken)
    {
        var peakRequest = new PeakSizingRequest
        {
            OccupiedHoursOnly = request.OccupiedHoursOnly,
            OccupancyThreshold = request.OccupancyThreshold,
            CoolingSeasonStartMonth = request.CoolingSeasonStartMonth,
            CoolingSeasonEndMonth = request.CoolingSeasonEndMonth,
            HeatingSeasonStartMonth = 11,
            HeatingSeasonEndMonth = 3,
            CoolingSafetyFactor = request.CoolingSafetyFactor,
            HeatingSafetyFactor = 1.0
        };

        var peakResult = await _peakSizingService.CalculateAsync(
            buildingId,
            year,
            peakRequest,
            cancellationToken);

        if (peakResult.IsFailure)
            return Result<BuildingCatalogAutosizingResponse>.Failure(peakResult.Error, peakResult.ErrorType);

        var peak = peakResult.Value;

        var candidates = await _catalogProvider.ListActiveCoolingCandidatesAsync(
            request.SystemType,
            request.UnitType,
            cancellationToken);

        if (candidates.Count == 0)
        {
            return Result<BuildingCatalogAutosizingResponse>.Validation(
                $"No active catalog items found for system type '{request.SystemType}' and unit type '{request.UnitType}'.");
        }

        var response = new BuildingCatalogAutosizingResponse
        {
            BuildingId = peak.BuildingId,
            BuildingName = peak.BuildingName,
            Year = peak.Year,
            Granularity = request.Granularity,
            SystemType = request.SystemType,
            UnitType = request.UnitType,
            OccupiedHoursOnly = request.OccupiedHoursOnly,
            OccupancyThreshold = request.OccupancyThreshold,
            CoolingSeasonStartMonth = request.CoolingSeasonStartMonth,
            CoolingSeasonEndMonth = request.CoolingSeasonEndMonth,
            CoolingSafetyFactor = request.CoolingSafetyFactor,
            MaxUnitsPerScope = request.MaxUnitsPerScope,
            TopRecommendationsPerScope = request.TopRecommendationsPerScope,
            MaxOversizeRatio = request.MaxOversizeRatio
        };

        var sourceScopes = SelectSourceScopes(peak, request.Granularity);

        foreach (var scope in sourceScopes)
        {
            var requiredKw = scope.SizedPeakLoadKw;
            if (requiredKw <= 0)
                continue;

            var recommendations = _rankingService.BuildRecommendations(
                requiredKw,
                candidates,
                request);

            if (recommendations.Count == 0)
                continue;

            response.Scopes.Add(new CatalogAutosizingScopeResponse
            {
                ScopeId = scope.ScopeId,
                ScopeName = scope.ScopeName,
                ParentScopeName = scope.ParentScopeName,
                RequiredCapacityKw = Round(requiredKw),
                Recommendations = recommendations
            });
        }

        if (response.Scopes.Count == 0)
        {
            return Result<BuildingCatalogAutosizingResponse>.Validation(
                "No catalog-backed autosizing recommendations matched the selected inputs.");
        }

        return Result<BuildingCatalogAutosizingResponse>.Success(response);
    }

    private static IReadOnlyList<PeakLoadSummaryResponse> SelectSourceScopes(
        BuildingPeakSizingResponse source,
        AutosizingGranularity granularity)
    {
        return granularity switch
        {
            AutosizingGranularity.Building =>
                source.BuildingCoolingPeak is null ? [] : [source.BuildingCoolingPeak],

            AutosizingGranularity.Zone =>
                source.ZoneCoolingPeaks,

            AutosizingGranularity.Room =>
                source.RoomCoolingPeaks,

            _ => []
        };
    }

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}