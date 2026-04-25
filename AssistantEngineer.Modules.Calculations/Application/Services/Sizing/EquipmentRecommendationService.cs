using AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Sizing;

public sealed class EquipmentRecommendationService
{
    private readonly BuildingCatalogAutosizingService _catalogAutosizingService;

    public EquipmentRecommendationService(BuildingCatalogAutosizingService catalogAutosizingService)
    {
        _catalogAutosizingService = catalogAutosizingService;
    }

    public async Task<Result<BuildingEquipmentRecommendationResponse>> CalculateAsync(
        int buildingId,
        int? year,
        EquipmentRecommendationRequest request,
        CancellationToken cancellationToken)
    {
        var catalogRequest = new CatalogAutosizingRequest
        {
            Granularity = request.Granularity,
            SystemType = request.SystemType,
            UnitType = request.UnitType,
            OccupiedHoursOnly = request.OccupiedHoursOnly,
            OccupancyThreshold = request.OccupancyThreshold,
            CoolingSeasonStartMonth = request.CoolingSeasonStartMonth,
            CoolingSeasonEndMonth = request.CoolingSeasonEndMonth,
            CoolingSafetyFactor = request.CoolingSafetyFactor,
            MaxUnitsPerScope = request.MaxUnitsPerScope,
            TopRecommendationsPerScope = Math.Max(request.TopRecommendationsPerScope * 4, request.TopRecommendationsPerScope),
            MaxOversizeRatio = request.MaxOversizeRatio,
            PreferredManufacturers = request.PreferredManufacturers,
            PreferredModelKeywords = request.PreferredModelKeywords,
            ExcludedManufacturers = request.ExcludedManufacturers,
            ExcludedModelKeywords = request.ExcludedModelKeywords,
            OversizePenaltyWeight = request.OversizePenaltyWeight,
            UnitCountPenaltyWeight = request.UnitCountPenaltyWeight,
            PreferredManufacturerBonus = request.PreferredManufacturerBonus,
            PreferredModelKeywordBonus = request.PreferredModelKeywordBonus,
            MinimumScore = request.MinimumCatalogScore
        };

        var catalogResult = await _catalogAutosizingService.CalculateAsync(
            buildingId,
            year,
            catalogRequest,
            cancellationToken);

        if (catalogResult.IsFailure)
            return Result<BuildingEquipmentRecommendationResponse>.Failure(catalogResult.Error, catalogResult.ErrorType);

        var catalog = catalogResult.Value;

        var response = new BuildingEquipmentRecommendationResponse
        {
            BuildingId = catalog.BuildingId,
            BuildingName = catalog.BuildingName,
            Year = catalog.Year,
            Granularity = catalog.Granularity,
            SystemType = catalog.SystemType,
            UnitType = catalog.UnitType,
            OccupiedHoursOnly = catalog.OccupiedHoursOnly,
            OccupancyThreshold = catalog.OccupancyThreshold,
            CoolingSeasonStartMonth = catalog.CoolingSeasonStartMonth,
            CoolingSeasonEndMonth = catalog.CoolingSeasonEndMonth,
            CoolingSafetyFactor = catalog.CoolingSafetyFactor,
            MaxUnitsPerScope = catalog.MaxUnitsPerScope,
            TopRecommendationsPerScope = request.TopRecommendationsPerScope,
            MaxOversizeRatio = catalog.MaxOversizeRatio,
            SizingWeight = request.SizingWeight,
            CapexWeight = request.CapexWeight,
            EfficiencyWeight = request.EfficiencyWeight,
            ComplexityWeight = request.ComplexityWeight
        };

        foreach (var scope in catalog.Scopes)
        {
            if (scope.Recommendations.Count == 0)
                continue;

            var proxies = scope.Recommendations
                .Select(option => new
                {
                    Option = option,
                    CapexProxy = option.TotalNominalCapacityKw * request.CapexPerKwFactor +
                                 option.UnitCount * request.CapexPerUnitFactor,
                    EfficiencyProxy = CalculateEfficiencyProxy(option, request),
                    ComplexityProxy = CalculateComplexityScore(option)
                })
                .ToArray();

            var minCapex = proxies.Min(x => x.CapexProxy);
            var maxCapex = proxies.Max(x => x.CapexProxy);

            var ranked = proxies
                .Select(x =>
                {
                    var capexScore = NormalizeInverse(x.CapexProxy, minCapex, maxCapex);
                    var efficiencyScore = Clamp(x.EfficiencyProxy, 0, 100);
                    var complexityScore = Clamp(x.ComplexityProxy, 0, 100);
                    var sizingScore = Clamp(x.Option.Score, 0, 100);

                    var composite = WeightedAverage(
                        (sizingScore, request.SizingWeight),
                        (capexScore, request.CapexWeight),
                        (efficiencyScore, request.EfficiencyWeight),
                        (complexityScore, request.ComplexityWeight));

                    var notes = new List<string>(x.Option.RankingNotes);

                    if (capexScore >= 80)
                        notes.Add("Low CAPEX proxy");

                    if (efficiencyScore >= 80)
                        notes.Add("High efficiency proxy");

                    if (complexityScore >= 80)
                        notes.Add("Low install complexity");

                    return new EquipmentRecommendationOptionResponse
                    {
                        CatalogItemId = x.Option.CatalogItemId,
                        Manufacturer = x.Option.Manufacturer,
                        SystemType = x.Option.SystemType,
                        UnitType = x.Option.UnitType,
                        ModelName = x.Option.ModelName,
                        RequiredCapacityKw = x.Option.RequiredCapacityKw,
                        UnitNominalCapacityKw = x.Option.UnitNominalCapacityKw,
                        UnitCount = x.Option.UnitCount,
                        TotalNominalCapacityKw = x.Option.TotalNominalCapacityKw,
                        CoverageRatio = x.Option.CoverageRatio,
                        OversizeRatio = x.Option.OversizeRatio,
                        CapacityReserveKw = x.Option.CapacityReserveKw,
                        CatalogScore = x.Option.Score,
                        CatalogRank = x.Option.Rank,
                        CapexProxyScore = Round(capexScore),
                        EfficiencyProxyScore = Round(efficiencyScore),
                        InstallComplexityScore = Round(complexityScore),
                        CompositeScore = Round(composite),
                        Notes = notes
                    };
                })
                .OrderByDescending(x => x.CompositeScore)
                .ThenBy(x => x.OversizeRatio)
                .ThenBy(x => x.UnitCount)
                .ThenByDescending(x => x.CatalogScore)
                .Take(request.TopRecommendationsPerScope)
                .Select((item, index) =>
                {
                    item.CompositeRank = index + 1;
                    return item;
                })
                .ToList();

            if (ranked.Count == 0)
                continue;

            response.Scopes.Add(new EquipmentRecommendationScopeResponse
            {
                ScopeId = scope.ScopeId,
                ScopeName = scope.ScopeName,
                ParentScopeName = scope.ParentScopeName,
                RequiredCapacityKw = scope.RequiredCapacityKw,
                Recommendations = ranked
            });
        }

        if (response.Scopes.Count == 0)
        {
            return Result<BuildingEquipmentRecommendationResponse>.Validation(
                "No equipment recommendations matched the selected scoring inputs.");
        }

        return Result<BuildingEquipmentRecommendationResponse>.Success(response);
    }

    private static double CalculateEfficiencyProxy(
        CatalogAutosizingOptionResponse option,
        EquipmentRecommendationRequest request)
    {
        var score = 100.0;

        score -= option.OversizeRatio * 80.0;
        score -= Math.Max(0, option.UnitCount - 1) * 5.0;

        if (request.PreferredEfficiencyKeywords.Any(keyword =>
                ContainsNormalized(option.ModelName, keyword)))
        {
            score += request.PreferredEfficiencyKeywordBonus;
        }

        if (ContainsNormalized(option.ModelName, "Inverter"))
            score += 4.0;

        if (ContainsNormalized(option.ModelName, "Eco"))
            score += 2.0;

        return score;
    }

    private static double CalculateComplexityScore(CatalogAutosizingOptionResponse option)
    {
        var baseScore = option.SystemType.Trim().ToUpperInvariant() switch
        {
            "DX" => 90.0,
            "VRF" => 70.0,
            "CHILLEDWATER" => 60.0,
            _ => 75.0
        };

        if (option.UnitType.Contains("Duct", StringComparison.OrdinalIgnoreCase))
            baseScore -= 10.0;

        if (option.UnitType.Contains("Cassette", StringComparison.OrdinalIgnoreCase))
            baseScore -= 5.0;

        baseScore -= Math.Max(0, option.UnitCount - 1) * 12.0;

        return baseScore;
    }

    private static bool ContainsNormalized(string source, string fragment) =>
        Normalize(source).Contains(Normalize(fragment), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static double NormalizeInverse(double value, double min, double max)
    {
        if (Math.Abs(max - min) < 0.000001)
            return 100.0;

        var normalized = (value - min) / (max - min);
        return 100.0 - normalized * 100.0;
    }

    private static double WeightedAverage(params (double Score, double Weight)[] parts)
    {
        var sumWeights = parts.Sum(x => x.Weight);
        if (sumWeights <= 0)
            return 0;

        return parts.Sum(x => x.Score * x.Weight) / sumWeights;
    }

    private static double Clamp(double value, double min, double max) =>
        Math.Max(min, Math.Min(max, value));

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}