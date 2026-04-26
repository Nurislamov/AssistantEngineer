using AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Sizing;

public sealed class BuildingAutosizingService
{
    private readonly BuildingPeakSizingService _peakSizingService;

    public BuildingAutosizingService(BuildingPeakSizingService peakSizingService)
    {
        _peakSizingService = peakSizingService;
    }

    public async Task<Result<BuildingAutosizingResponse>> CalculateAsync(
        int buildingId,
        int? year,
        AutosizingRequest request,
        CancellationToken cancellationToken)
    {
        var peakRequest = new PeakSizingRequest
        {
            OccupiedHoursOnly = request.OccupiedHoursOnly,
            OccupancyThreshold = request.OccupancyThreshold,
            CoolingSeasonStartMonth = request.CoolingSeasonStartMonth,
            CoolingSeasonEndMonth = request.CoolingSeasonEndMonth,
            HeatingSeasonStartMonth = request.HeatingSeasonStartMonth,
            HeatingSeasonEndMonth = request.HeatingSeasonEndMonth,
            CoolingSafetyFactor = request.CoolingSafetyFactor,
            HeatingSafetyFactor = request.HeatingSafetyFactor
        };

        var peakResult = await _peakSizingService.CalculateAsync(
            buildingId,
            year,
            peakRequest,
            cancellationToken);

        if (peakResult.IsFailure)
            return Result<BuildingAutosizingResponse>.Failure(peakResult.Error, peakResult.ErrorType);

        var peak = peakResult.Value;

        var response = new BuildingAutosizingResponse
        {
            BuildingId = peak.BuildingId,
            BuildingName = peak.BuildingName,
            Year = peak.Year,
            Mode = request.Mode,
            Granularity = request.Granularity,
            CandidateNominalCapacitiesKw = request.CandidateNominalCapacitiesKw
                .Distinct()
                .OrderBy(x => x)
                .ToList(),
            OccupiedHoursOnly = request.OccupiedHoursOnly,
            OccupancyThreshold = request.OccupancyThreshold,
            CoolingSeasonStartMonth = request.CoolingSeasonStartMonth,
            CoolingSeasonEndMonth = request.CoolingSeasonEndMonth,
            HeatingSeasonStartMonth = request.HeatingSeasonStartMonth,
            HeatingSeasonEndMonth = request.HeatingSeasonEndMonth,
            CoolingSafetyFactor = request.CoolingSafetyFactor,
            HeatingSafetyFactor = request.HeatingSafetyFactor,
            MaxUnitsPerScope = request.MaxUnitsPerScope,
            TopRecommendationsPerScope = request.TopRecommendationsPerScope,
            MaxOversizeRatio = request.MaxOversizeRatio
        };

        var sourceScopes = SelectSourceScopes(peak, request.Mode, request.Granularity);

        foreach (var scope in sourceScopes)
        {
            var requiredKw = scope.SizedPeakLoadKw;
            if (requiredKw <= 0)
                continue;

            var recommendations = BuildRecommendations(
                requiredKw,
                response.CandidateNominalCapacitiesKw,
                request.MaxUnitsPerScope,
                request.TopRecommendationsPerScope,
                request.MaxOversizeRatio);

            if (recommendations.Count == 0)
                continue;

            response.Scopes.Add(new AutosizingScopeResponse
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
            return Result<BuildingAutosizingResponse>.Validation(
                "No autosizing recommendations matched the selected inputs.");
        }

        return Result<BuildingAutosizingResponse>.Success(response);
    }

    private static IReadOnlyList<PeakLoadSummaryResponse> SelectSourceScopes(
        BuildingPeakSizingResponse source,
        ReferenceDesignDayMode mode,
        AutosizingGranularity granularity)
    {
        return (mode, granularity) switch
        {
            (ReferenceDesignDayMode.Cooling, AutosizingGranularity.Building) =>
                source.BuildingCoolingPeak is null
                    ? []
                    : [source.BuildingCoolingPeak],

            (ReferenceDesignDayMode.Heating, AutosizingGranularity.Building) =>
                source.BuildingHeatingPeak is null
                    ? []
                    : [source.BuildingHeatingPeak],

            (ReferenceDesignDayMode.Cooling, AutosizingGranularity.Zone) =>
                source.ZoneCoolingPeaks,

            (ReferenceDesignDayMode.Heating, AutosizingGranularity.Zone) =>
                source.ZoneHeatingPeaks,

            (ReferenceDesignDayMode.Cooling, AutosizingGranularity.Room) =>
                source.RoomCoolingPeaks,

            (ReferenceDesignDayMode.Heating, AutosizingGranularity.Room) =>
                source.RoomHeatingPeaks,

            _ => []
        };
    }

    private static List<AutosizingOptionResponse> BuildRecommendations(
        double requiredCapacityKw,
        IReadOnlyCollection<double> candidateNominalCapacitiesKw,
        int maxUnitsPerScope,
        int topRecommendationsPerScope,
        double maxOversizeRatio)
    {
        var results = new List<AutosizingOptionResponse>();

        foreach (var unitKw in candidateNominalCapacitiesKw
                     .Distinct()
                     .Where(x => x > 0)
                     .OrderBy(x => x))
        {
            var unitCount = (int)Math.Ceiling(requiredCapacityKw / unitKw);
            if (unitCount <= 0)
                unitCount = 1;

            if (unitCount > maxUnitsPerScope)
                continue;

            var totalKw = unitCount * unitKw;
            var coverageRatio = totalKw / requiredCapacityKw;
            var oversizeRatio = coverageRatio - 1.0;

            if (oversizeRatio > maxOversizeRatio)
                continue;

            results.Add(new AutosizingOptionResponse
            {
                RequiredCapacityKw = Round(requiredCapacityKw),
                UnitNominalCapacityKw = Round(unitKw),
                UnitCount = unitCount,
                TotalNominalCapacityKw = Round(totalKw),
                CoverageRatio = Round(coverageRatio),
                OversizeRatio = Round(oversizeRatio)
            });
        }

        return results
            .OrderBy(x => x.OversizeRatio)
            .ThenBy(x => x.UnitCount)
            .ThenBy(x => x.UnitNominalCapacityKw)
            .Take(topRecommendationsPerScope)
            .ToList();
    }

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}