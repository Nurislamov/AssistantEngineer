using AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;
using AssistantEngineer.Modules.Calculations.Application.Models.Sizing;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Sizing;

public sealed class CatalogAutosizingRankingService
{
    public List<CatalogAutosizingOptionResponse> BuildRecommendations(
        double requiredCapacityKw,
        IReadOnlyCollection<CoolingEquipmentCatalogSizingCandidate> candidates,
        CatalogAutosizingRequest request)
    {
        var results = new List<CatalogAutosizingOptionResponse>();

        foreach (var candidate in candidates
                     .OrderBy(x => x.NominalCoolingCapacityKw)
                     .ThenBy(x => x.Manufacturer)
                     .ThenBy(x => x.ModelName))
        {
            if (IsExcluded(candidate, request))
                continue;

            var unitCount = (int)Math.Ceiling(requiredCapacityKw / candidate.NominalCoolingCapacityKw);
            if (unitCount <= 0)
                unitCount = 1;

            if (unitCount > request.MaxUnitsPerScope)
                continue;

            var totalNominalKw = unitCount * candidate.NominalCoolingCapacityKw;
            var coverageRatio = totalNominalKw / requiredCapacityKw;
            var oversizeRatio = coverageRatio - 1.0;

            if (oversizeRatio > request.MaxOversizeRatio)
                continue;

            var notes = BuildNotes(candidate, unitCount, oversizeRatio, request);
            var score = CalculateScore(candidate, unitCount, oversizeRatio, request);

            if (score < request.MinimumScore)
                continue;

            results.Add(new CatalogAutosizingOptionResponse
            {
                CatalogItemId = candidate.CatalogItemId,
                Manufacturer = candidate.Manufacturer,
                SystemType = candidate.SystemType,
                UnitType = candidate.UnitType,
                ModelName = candidate.ModelName,
                RequiredCapacityKw = Round(requiredCapacityKw),
                UnitNominalCapacityKw = Round(candidate.NominalCoolingCapacityKw),
                UnitCount = unitCount,
                TotalNominalCapacityKw = Round(totalNominalKw),
                CoverageRatio = Round(coverageRatio),
                OversizeRatio = Round(oversizeRatio),
                CapacityReserveKw = Round(totalNominalKw - requiredCapacityKw),
                Score = Round(score),
                RankingNotes = notes
            });
        }

        return results
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.OversizeRatio)
            .ThenBy(x => x.UnitCount)
            .ThenBy(x => x.UnitNominalCapacityKw)
            .ThenBy(x => x.Manufacturer)
            .ThenBy(x => x.ModelName)
            .Take(request.TopRecommendationsPerScope)
            .Select((item, index) =>
            {
                item.Rank = index + 1;
                return item;
            })
            .ToList();
    }

    private static bool IsExcluded(
        CoolingEquipmentCatalogSizingCandidate candidate,
        CatalogAutosizingRequest request)
    {
        if (request.ExcludedManufacturers.Any(name =>
                string.Equals(
                    Normalize(name),
                    Normalize(candidate.Manufacturer),
                    StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (request.ExcludedModelKeywords.Any(keyword =>
                ContainsNormalized(candidate.ModelName, keyword)))
        {
            return true;
        }

        return false;
    }

    private static double CalculateScore(
        CoolingEquipmentCatalogSizingCandidate candidate,
        int unitCount,
        double oversizeRatio,
        CatalogAutosizingRequest request)
    {
        var score = 100.0;

        score -= oversizeRatio * 100.0 * request.OversizePenaltyWeight;
        score -= Math.Max(0, unitCount - 1) * 10.0 * request.UnitCountPenaltyWeight;

        if (request.PreferredManufacturers.Any(name =>
                string.Equals(
                    Normalize(name),
                    Normalize(candidate.Manufacturer),
                    StringComparison.OrdinalIgnoreCase)))
        {
            score += request.PreferredManufacturerBonus;
        }

        if (request.PreferredModelKeywords.Any(keyword =>
                ContainsNormalized(candidate.ModelName, keyword)))
        {
            score += request.PreferredModelKeywordBonus;
        }

        return score;
    }

    private static List<string> BuildNotes(
        CoolingEquipmentCatalogSizingCandidate candidate,
        int unitCount,
        double oversizeRatio,
        CatalogAutosizingRequest request)
    {
        var notes = new List<string>();

        if (oversizeRatio <= 0.10)
            notes.Add("Near-match capacity");
        else if (oversizeRatio <= 0.25)
            notes.Add("Moderate reserve");
        else
            notes.Add("High reserve");

        if (unitCount == 1)
            notes.Add("Single-unit solution");
        else
            notes.Add($"{unitCount} units required");

        if (request.PreferredManufacturers.Any(name =>
                string.Equals(
                    Normalize(name),
                    Normalize(candidate.Manufacturer),
                    StringComparison.OrdinalIgnoreCase)))
        {
            notes.Add("Preferred manufacturer");
        }

        if (request.PreferredModelKeywords.Any(keyword =>
                ContainsNormalized(candidate.ModelName, keyword)))
        {
            notes.Add("Preferred model keyword");
        }

        return notes;
    }

    private static bool ContainsNormalized(string source, string fragment) =>
        Normalize(source).Contains(Normalize(fragment), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}