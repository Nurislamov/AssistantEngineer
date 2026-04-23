using AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Sizing;

public sealed class EquipmentRecommendationReportService
{
    private readonly EquipmentRecommendationService _recommendations;
    private readonly TimeProvider _timeProvider;

    public EquipmentRecommendationReportService(
        EquipmentRecommendationService recommendations,
        TimeProvider timeProvider)
    {
        _recommendations = recommendations;
        _timeProvider = timeProvider;
    }

    public async Task<Result<BuildingEquipmentRecommendationReportResponse>> BuildAsync(
        int buildingId,
        int? year,
        EquipmentRecommendationReportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recommendations.CalculateAsync(
            buildingId,
            year,
            request.Request,
            cancellationToken);

        if (result.IsFailure)
            return Result<BuildingEquipmentRecommendationReportResponse>.Failure(result.Error, result.ErrorType);

        var source = result.Value;

        var rows = source.Scopes
            .SelectMany(scope => scope.Recommendations.Select(rec => new EquipmentRecommendationReportRowResponse
            {
                ScopeId = scope.ScopeId,
                ScopeName = scope.ScopeName,
                ParentScopeName = scope.ParentScopeName,
                RequiredCapacityKw = scope.RequiredCapacityKw,
                CompositeRank = rec.CompositeRank,
                CatalogRank = rec.CatalogRank,
                CatalogItemId = rec.CatalogItemId,
                Manufacturer = rec.Manufacturer,
                SystemType = rec.SystemType,
                UnitType = rec.UnitType,
                ModelName = rec.ModelName,
                UnitNominalCapacityKw = rec.UnitNominalCapacityKw,
                UnitCount = rec.UnitCount,
                TotalNominalCapacityKw = rec.TotalNominalCapacityKw,
                CoverageRatio = rec.CoverageRatio,
                OversizeRatio = rec.OversizeRatio,
                CapacityReserveKw = rec.CapacityReserveKw,
                CatalogScore = rec.CatalogScore,
                CapexProxyScore = rec.CapexProxyScore,
                EfficiencyProxyScore = rec.EfficiencyProxyScore,
                InstallComplexityScore = rec.InstallComplexityScore,
                CompositeScore = rec.CompositeScore,
                Notes = string.Join("; ", rec.Notes)
            }))
            .OrderBy(x => x.ScopeName)
            .ThenBy(x => x.CompositeRank)
            .ToList();

        var topRows = source.Scopes
            .Where(scope => scope.Recommendations.Count > 0)
            .Select(scope => scope.Recommendations
                .OrderBy(x => x.CompositeRank)
                .ThenByDescending(x => x.CompositeScore)
                .First())
            .ToArray();

        var response = new BuildingEquipmentRecommendationReportResponse
        {
            BuildingId = source.BuildingId,
            BuildingName = source.BuildingName,
            Year = source.Year,
            ScenarioName = request.ScenarioName,
            Granularity = source.Granularity,
            SystemType = source.SystemType,
            UnitType = source.UnitType,
            ScopeCount = source.Scopes.Count,
            RowCount = rows.Count,
            AverageRequiredCapacityKw = Round(source.Scopes.Count == 0 ? 0 : source.Scopes.Average(x => x.RequiredCapacityKw)),
            AverageTopCompositeScore = Round(topRows.Length == 0 ? 0 : topRows.Average(x => x.CompositeScore)),
            GeneratedAtUtc = _timeProvider.GetUtcNow(),
            Rows = rows
        };

        return Result<BuildingEquipmentRecommendationReportResponse>.Success(response);
    }

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}