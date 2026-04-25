using AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Sizing;

public sealed class EquipmentRecommendationComparisonReportService
{
    private readonly EquipmentRecommendationComparisonService _comparison;
    private readonly TimeProvider _timeProvider;

    public EquipmentRecommendationComparisonReportService(
        EquipmentRecommendationComparisonService comparison,
        TimeProvider timeProvider)
    {
        _comparison = comparison;
        _timeProvider = timeProvider;
    }

    public async Task<Result<BuildingEquipmentRecommendationComparisonReportResponse>> BuildAsync(
        int buildingId,
        int? year,
        EquipmentRecommendationComparisonReportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _comparison.CompareAsync(
            buildingId,
            year,
            request.Request,
            cancellationToken);

        if (result.IsFailure)
            return Result<BuildingEquipmentRecommendationComparisonReportResponse>.Failure(result.Error, result.ErrorType);

        var source = result.Value;

        var rows = source.Scopes
            .SelectMany(scope => scope.Scenarios.Select(item => new EquipmentRecommendationComparisonReportRowResponse
            {
                ScopeId = scope.ScopeId,
                ScopeName = scope.ScopeName,
                ParentScopeName = scope.ParentScopeName,
                ScenarioName = item.ScenarioName,
                IsWinner = string.Equals(item.ScenarioName, scope.WinningScenarioName, StringComparison.Ordinal),
                WinningScenarioName = scope.WinningScenarioName,
                SystemType = item.SystemType,
                UnitType = item.UnitType,
                CatalogItemId = item.CatalogItemId,
                Manufacturer = item.Manufacturer,
                ModelName = item.ModelName,
                RequiredCapacityKw = item.RequiredCapacityKw,
                UnitNominalCapacityKw = item.UnitNominalCapacityKw,
                UnitCount = item.UnitCount,
                TotalNominalCapacityKw = item.TotalNominalCapacityKw,
                CatalogScore = item.CatalogScore,
                CapexProxyScore = item.CapexProxyScore,
                EfficiencyProxyScore = item.EfficiencyProxyScore,
                InstallComplexityScore = item.InstallComplexityScore,
                CompositeScore = item.CompositeScore,
                Notes = string.Join("; ", item.Notes)
            }))
            .OrderBy(x => x.ScopeName)
            .ThenByDescending(x => x.IsWinner)
            .ThenByDescending(x => x.CompositeScore)
            .ToList();

        var response = new BuildingEquipmentRecommendationComparisonReportResponse
        {
            BuildingId = source.BuildingId,
            BuildingName = source.BuildingName,
            Year = source.Year,
            Granularity = source.Granularity,
            ScenarioCount = source.ScenarioSummaries.Count,
            ScopeCount = source.Scopes.Count,
            RowCount = rows.Count,
            GeneratedAtUtc = _timeProvider.GetUtcNow(),
            ScenarioSummaries = source.ScenarioSummaries,
            Rows = rows
        };

        return Result<BuildingEquipmentRecommendationComparisonReportResponse>.Success(response);
    }
}