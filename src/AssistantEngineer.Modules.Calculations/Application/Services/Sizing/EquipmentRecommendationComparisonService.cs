using AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Sizing;

public sealed class EquipmentRecommendationComparisonService
{
    private readonly EquipmentRecommendationService _recommendations;

    public EquipmentRecommendationComparisonService(
        EquipmentRecommendationService recommendations)
    {
        _recommendations = recommendations;
    }

    public async Task<Result<BuildingEquipmentRecommendationComparisonResponse>> CompareAsync(
        int buildingId,
        int? year,
        EquipmentRecommendationComparisonRequest request,
        CancellationToken cancellationToken)
    {
        var scenarioResults = new List<(string ScenarioName, BuildingEquipmentRecommendationResponse Response)>();

        foreach (var scenario in request.Scenarios)
        {
            var result = await _recommendations.CalculateAsync(
                buildingId,
                year,
                scenario.Request,
                cancellationToken);

            if (result.IsFailure)
            {
                return Result<BuildingEquipmentRecommendationComparisonResponse>.Failure(
                    $"Scenario '{scenario.ScenarioName}': {result.Error}",
                    result.ErrorType);
            }

            scenarioResults.Add((scenario.ScenarioName, result.Value));
        }

        if (scenarioResults.Count == 0)
        {
            return Result<BuildingEquipmentRecommendationComparisonResponse>.Validation(
                "No scenario results were produced.");
        }

        var first = scenarioResults[0].Response;

        var response = new BuildingEquipmentRecommendationComparisonResponse
        {
            BuildingId = first.BuildingId,
            BuildingName = first.BuildingName,
            Year = first.Year,
            Granularity = first.Granularity,
            ScenarioSummaries = scenarioResults
                .Select(x => BuildScenarioSummary(x.ScenarioName, x.Response))
                .ToList()
        };

        var allScopeKeys = scenarioResults
            .SelectMany(x => x.Response.Scopes.Select(scope => MakeScopeKey(scope.ScopeId, scope.ScopeName, scope.ParentScopeName)))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        foreach (var scopeKey in allScopeKeys)
        {
            var scopeScenarios = new List<EquipmentRecommendationComparisonScopeScenarioResponse>();

            int? scopeId = null;
            string scopeName = string.Empty;
            string? parentScopeName = null;

            foreach (var scenario in scenarioResults)
            {
                var scope = scenario.Response.Scopes
                    .FirstOrDefault(x => MakeScopeKey(x.ScopeId, x.ScopeName, x.ParentScopeName) == scopeKey);

                if (scope is null || scope.Recommendations.Count == 0)
                    continue;

                var best = scope.Recommendations
                    .OrderBy(x => x.CompositeRank)
                    .ThenByDescending(x => x.CompositeScore)
                    .First();

                scopeId ??= scope.ScopeId;
                scopeName = scope.ScopeName;
                parentScopeName ??= scope.ParentScopeName;

                scopeScenarios.Add(new EquipmentRecommendationComparisonScopeScenarioResponse
                {
                    ScenarioName = scenario.ScenarioName,
                    SystemType = scenario.Response.SystemType,
                    UnitType = scenario.Response.UnitType,
                    CatalogItemId = best.CatalogItemId,
                    Manufacturer = best.Manufacturer,
                    ModelName = best.ModelName,
                    RequiredCapacityKw = best.RequiredCapacityKw,
                    UnitNominalCapacityKw = best.UnitNominalCapacityKw,
                    UnitCount = best.UnitCount,
                    TotalNominalCapacityKw = best.TotalNominalCapacityKw,
                    CatalogScore = best.CatalogScore,
                    CapexProxyScore = best.CapexProxyScore,
                    EfficiencyProxyScore = best.EfficiencyProxyScore,
                    InstallComplexityScore = best.InstallComplexityScore,
                    CompositeScore = best.CompositeScore,
                    Notes = best.Notes
                });
            }

            if (scopeScenarios.Count == 0)
                continue;

            var winner = scopeScenarios
                .OrderByDescending(x => x.CompositeScore)
                .ThenBy(x => x.TotalNominalCapacityKw)
                .ThenBy(x => x.UnitCount)
                .First();

            response.Scopes.Add(new EquipmentRecommendationComparisonScopeResponse
            {
                ScopeId = scopeId,
                ScopeName = scopeName,
                ParentScopeName = parentScopeName,
                WinningScenarioName = winner.ScenarioName,
                WinningCompositeScore = Round(winner.CompositeScore),
                Scenarios = scopeScenarios
                    .OrderByDescending(x => x.CompositeScore)
                    .ThenBy(x => x.TotalNominalCapacityKw)
                    .ThenBy(x => x.UnitCount)
                    .ToList()
            });
        }

        if (response.Scopes.Count == 0)
        {
            return Result<BuildingEquipmentRecommendationComparisonResponse>.Validation(
                "No comparison scopes were produced.");
        }

        return Result<BuildingEquipmentRecommendationComparisonResponse>.Success(response);
    }

    private static EquipmentRecommendationScenarioSummaryResponse BuildScenarioSummary(
        string scenarioName,
        BuildingEquipmentRecommendationResponse response)
    {
        var bestRecommendations = response.Scopes
            .Where(scope => scope.Recommendations.Count > 0)
            .Select(scope => scope.Recommendations
                .OrderBy(x => x.CompositeRank)
                .ThenByDescending(x => x.CompositeScore)
                .First())
            .ToArray();

        return new EquipmentRecommendationScenarioSummaryResponse
        {
            ScenarioName = scenarioName,
            Granularity = response.Granularity,
            SystemType = response.SystemType,
            UnitType = response.UnitType,
            ScopeCount = response.Scopes.Count,
            AverageRequiredCapacityKw = Round(response.Scopes.Count == 0 ? 0 : response.Scopes.Average(x => x.RequiredCapacityKw)),
            AverageTopCompositeScore = Round(bestRecommendations.Length == 0 ? 0 : bestRecommendations.Average(x => x.CompositeScore))
        };
    }

    private static string MakeScopeKey(int? scopeId, string scopeName, string? parentScopeName) =>
        $"{scopeId?.ToString() ?? "null"}|{scopeName}|{parentScopeName ?? string.Empty}";

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}